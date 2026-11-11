using MemeHunter.Dev;
using MemeHunter.Net;
using MemeHunter.UI;               // the designer's ARGameUIController / ARGameUIState
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemeHunter.Game
{
    /// <summary>
    /// Game logic of one hunt: find nearest card -> claim -> walk (distance drives the AR UI) -> catch (photo + upload).
    /// Lives only in the AR scene. It is created from code when a scene with the designer's ARGameUIController
    /// loads, so no scene file is edited.
    /// </summary>
    public sealed class HuntController : MonoBehaviour
    {
        /// <summary>The designer's "Start hunt" button can call HuntController.Instance.StartHunt().</summary>
        public static HuntController Instance { get; private set; }

        const double CatchRadiusM = 30.0;        // closer than this -> Catch is enabled
        const double LeaveRadiusM = 50.0;        // Catch is left only beyond this (GPS jumps indoors, so no flicker at 30 m)
        const double MaxSearchRadiusM = 3000.0;  // cards farther than this are ignored (server returns all for now)

        ARGameUIController ui;                   // the designer's controller found in the scene
        ClaimDto hunt;                           // the claimed card (exact coordinates) or null
        bool busy;                               // a network call / photo is in progress
        float nextCheck;                         // distance is evaluated twice a second, not every frame
        double lastDistanceM = -1;
        double lastAccuracyM;                    // GPS error radius of the last position (DEV label)

        // ---------- Automatic creation when the AR scene loads ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Hook()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Only scenes that contain the designer's AR controller get a HuntController.
            var arUi = FindFirstObjectByType<ARGameUIController>();
            if (arUi == null) return;

            var go = new GameObject("HuntController");
            go.AddComponent<HuntController>().ui = arUi;   // assigned before Start() runs
        }

        // ---------- Lifecycle ----------

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        async void Start()
        {
            TemplateUiCleanup.Run();                        // no tap-to-place cubes / template prompts in the hunt
            ui.CatchRequested.AddListener(OnCatchPressed);

            hunt = Session.ActiveHunt;                      // resume a hunt after restarting the app
            if (hunt == null)
            {
                ui.SetState(ARGameUIState.Freeze);
                return;
            }

            Toast.Show("Resuming your hunt...");
            if (!await Geo.StartAsync())
                Toast.Show("Location is needed to continue the hunt.");
        }

        void Update()
        {
            if (hunt == null || busy || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;

            if (!Geo.TryGetFix(out var fix)) return;
            lastDistanceM = Geo.DistanceMeters(fix.Lat, fix.Lng, hunt.Lat, hunt.Lng);
            lastAccuracyM = fix.AccuracyM;

            // Close enough -> "Catch"; otherwise -> "Get closer".
            // Entering Catch needs <= 30 m, leaving it needs > 50 m (hysteresis), so a jumping GPS does not
            // switch the state back and forth. SetState fires UI updates, so call it only when the state really changes.
            var inCatch = ui.CurrentState == ARGameUIState.Catch;
            var wanted = lastDistanceM <= (inCatch ? LeaveRadiusM : CatchRadiusM) ? ARGameUIState.Catch : ARGameUIState.GetCloser;
            if (ui.CurrentState != wanted)
            {
                ui.SetState(wanted);
                if (wanted == ARGameUIState.Catch)
                    MemeSpawner.Show(hunt.Rarity);          // the meme appears in the camera view
                else
                    MemeSpawner.Hide();
            }
        }

        // ---------- Start a hunt ----------

        /// <summary>Finds the nearest card and claims it. Safe to call from any button.</summary>
        public async void StartHunt()
        {
            if (busy || hunt != null) return;
            if (!Session.HasPlayer) { Toast.Show("Create a player first (DEV > Net / camera test > 2)."); return; }

            busy = true;
            try
            {
                if (!await Geo.StartAsync())
                {
                    Toast.Show("Location is needed to find memes. Allow it in the phone settings.");
                    return;
                }
                if (!Geo.TryGetFix(out var fix))
                {
                    Toast.Show("Waiting for GPS... try again in a few seconds.");
                    return;
                }

                // 1) Ask the server which cards exist and pick the nearest one
                //    (the server rounds positions to ~100 m, good enough for choosing).
                var cards = await Api.GetNearby(fix.Lat, fix.Lng);
                NearbyCardDto best = null;
                var bestDistance = double.MaxValue;
                foreach (var card in cards)
                {
                    var d = Geo.DistanceMeters(fix.Lat, fix.Lng, card.ApproxLat, card.ApproxLng);
                    if (d < bestDistance) { best = card; bestDistance = d; }
                }
                if (best == null || bestDistance > MaxSearchRadiusM)
                {
                    Toast.Show("No memes around here yet.");
                    return;
                }

                // 2) Claim it: the answer contains the EXACT place. Save it at once (survives an app restart).
                var claim = await Api.ClaimCard(best.Id, Session.PlayerId);
                Session.SaveHunt(claim);
                hunt = claim;
                nextCheck = 0f;                              // evaluate the distance on the next frame
                Toast.Show($"A {claim.Rarity} meme is hiding nearby. Go find it!");
            }
            catch (ApiException e)
            {
                Toast.Show(e.Message);                       // e.g. someone else claimed it a second ago
            }
            finally
            {
                busy = false;
            }
        }

        /// <summary>Developer helper: forget the local hunt (the card stays claimed on the server).</summary>
        public void CancelHunt()
        {
            hunt = null;
            lastDistanceM = -1;
            Session.ClearHunt();
            MemeSpawner.Hide();
            ui.SetState(ARGameUIState.Freeze);
        }

        // ---------- Catch: photo -> upload ----------

        async void OnCatchPressed()
        {
            if (busy || hunt == null) return;

            // Position at the moment of pressing (before the photo is taken).
            if (!Geo.TryGetFix(out var fix))
            {
                Toast.Show("No GPS position yet. Try again in a moment.");
                return;
            }
            if (Geo.DistanceMeters(fix.Lat, fix.Lng, hunt.Lat, hunt.Lng) > CatchRadiusM * 2)
            {
                Toast.Show("You are too far from the meme.");
                return;
            }

            busy = true;
            try
            {
                // 1) Photo (the AR view on the phone, file picker in the Editor)
                var jpeg = await PhotoCapture.TakeAsync();
                if (jpeg == null)
                {
                    Toast.Show("Photo cancelled.");
                    return;                                   // the hunt stays active, the player can retry
                }

                // 2) Upload (can take many seconds: the backend forwards it to doskaz.kz)
                Toast.Show("Sending photo...", 60f);
                var result = await Api.SubmitPhoto(Session.PlayerId, hunt.CardId, fix.Lat, fix.Lng, jpeg);

                if (result.Status == "sent")
                {
                    Toast.Show($"Meme caught! +{result.Reward} coins");
                    Session.ClearHunt();
                    hunt = null;
                    lastDistanceM = -1;
                    MemeSpawner.Hide();
                    ui.SetState(ARGameUIState.Freeze);
                    await RefreshPlayer();                    // new coin balance for Profile / HUD
                }
                else
                {
                    // The backend could not hand it to doskaz (e.g. expired token). Nothing is lost on our side.
                    Toast.Show("Could not send the photo right now. Please try again later.");
                }
            }
            catch (ApiException e)
            {
                Toast.Show(e.Message);                         // offline / timeout / server error: the hunt stays active
            }
            finally
            {
                busy = false;
            }
        }

        /// <summary>Re-reads the player so the new coin balance is available everywhere (Session.PlayerChanged).</summary>
        static async Awaitable RefreshPlayer()
        {
            try
            {
                var player = await Api.GetPlayer(Session.PlayerId);
                Session.SetPlayer(player);
            }
            catch (ApiException)
            {
                // Not critical: Profile fetches the balance again when it opens.
            }
        }

        // ---------- Developer panel (hidden when ApiConfig.ShowDevTools = false) ----------

        void OnGUI()
        {
            if (!ApiConfig.ShowDevTools || DevGui.Hidden) return;
            DevGui.Scale();

            GUILayout.BeginArea(new Rect(8, 40, 190, 280));
            GUILayout.Label(hunt == null ? "No active hunt" : $"Distance: {lastDistanceM:F0} m (GPS ±{lastAccuracyM:F0} m)");

            GUI.enabled = !busy;
            if (hunt == null)
            {
                if (GUILayout.Button("[DEV] Hunt nearest", GUILayout.Height(34)))
                    StartHunt();
            }
            else
            {
                if (GUILayout.Button("[DEV] Cancel hunt", GUILayout.Height(34)))
                    CancelHunt();
#if UNITY_EDITOR
                // The Editor has no GPS: move the fake position instead of walking.
                if (GUILayout.Button("[DEV] Teleport to card", GUILayout.Height(34)))
                { Geo.FakeLat = hunt.Lat; Geo.FakeLng = hunt.Lng; }
                if (GUILayout.Button("[DEV] Move 100 m away", GUILayout.Height(34)))
                { Geo.FakeLat = hunt.Lat + 0.0009; Geo.FakeLng = hunt.Lng; }   // 0.0009 deg latitude ~ 100 m
#endif
            }
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
}
