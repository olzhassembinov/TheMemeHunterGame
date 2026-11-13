using MemeHunter.Dev;
using MemeHunter.Net;
using MemeHunter.UI;               // ARGameUIController marks the AR scene; AppNav
using MemeHunter.UI.Hunt;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemeHunter.Game
{
    /// <summary>
    /// Game logic of one hunt: find nearest card -> claim -> walk (distance drives the HUD) -> catch (photo + upload).
    /// Lives only in the AR scene. It is created from code when a scene with the designer's ARGameUIController
    /// loads; the old overlay is hidden and replaced by the code-built HuntHud (Figma "Game" section).
    /// </summary>
    public sealed class HuntController : MonoBehaviour
    {
        public static HuntController Instance { get; private set; }

        const double CatchRadiusM = 30.0;        // closer than this -> the meme appears, Catch can be used
        const double LeaveRadiusM = 50.0;        // Near is left only beyond this (GPS jumps indoors, so no flicker at 30 m)
        const double MaxSearchRadiusM = 3000.0;  // cards farther than this are ignored (server returns all for now)
        const double CatchAllowedM = CatchRadiusM * 2;   // GPS tolerance when the photo is actually sent

        HuntHud hud;
        ClaimDto hunt;                           // the claimed card (exact coordinates) or null
        bool busy;                               // a network call / photo is in progress
        float nextCheck;                         // distance is evaluated twice a second, not every frame
        double lastDistanceM = -1;
        double lastAccuracyM;                    // GPS error radius of the last position (DEV label)
        bool near;

        // ---------- Automatic creation when the AR scene loads ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Hook()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Only the scene that contains the designer's AR controller is the hunt scene.
            var arUi = FindFirstObjectByType<ARGameUIController>();
            if (arUi == null) return;

            arUi.transform.root.gameObject.SetActive(false);    // old overlay: replaced by HuntHud
            new GameObject("HuntController").AddComponent<HuntController>();
        }

        // ---------- Lifecycle ----------

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        async void Start()
        {
            TemplateUiCleanup.Run();                        // no tap-to-place cubes / template prompts in the hunt
            hud = HuntHud.Create();
            hud.CatchRequested += OnCatchPressed;
            hud.StartHuntRequested += StartHunt;

            hunt = Session.ActiveHunt;                      // resume a hunt after restarting the app
            if (hunt == null)
            {
                hud.SetPhase(HuntPhase.NoHunt);
                if (AppNav.StartHuntOnOpen)                 // opened from the map's Camera / "Start hunt" button
                {
                    AppNav.StartHuntOnOpen = false;
                    StartHunt();
                }
                return;
            }

            hud.SetRarity(hunt.Rarity);
            hud.SetTarget(hunt.Lat, hunt.Lng);
            hud.SetPhase(HuntPhase.Walking);
            if (!await Geo.StartAsync())
                HuntErrors.LocationDenied();
        }

        void Update()
        {
            if (hunt == null || busy || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;

            if (!Geo.TryGetFix(out var fix)) return;
            lastDistanceM = Geo.DistanceMeters(fix.Lat, fix.Lng, hunt.Lat, hunt.Lng);
            lastAccuracyM = fix.AccuracyM;
            hud.SetDistance(lastDistanceM);

            // Entering Near needs <= 30 m, leaving it needs > 50 m (hysteresis), so a jumping GPS does not
            // switch back and forth. Only act when the state really changes.
            var wantNear = lastDistanceM <= (near ? LeaveRadiusM : CatchRadiusM);
            if (wantNear == near) return;
            near = wantNear;
            if (near)
            {
                MemeSpawner.Show(hunt.Rarity, hunt.CardId);              // the meme appears in the camera view
                Handheld.Vibrate();                         // "it's here!"
            }
            else
            {
                MemeSpawner.Hide();
            }
            hud.SetPhase(near ? HuntPhase.Near : HuntPhase.Walking);
        }

        // ---------- Start a hunt ----------

        /// <summary>Finds the nearest card and claims it. Safe to call from any button.</summary>
        public async void StartHunt()
        {
            if (busy || hunt != null) return;

            busy = true;
            hud.SetPhase(HuntPhase.Searching);
            var started = false;
            try
            {
                if (!await EnsurePlayer()) return;

                if (!await Geo.StartAsync())
                {
                    HuntErrors.LocationDenied();
                    return;
                }
                if (!Geo.TryGetFix(out var fix))
                {
                    HuntErrors.NoGps();
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
                    UI.Kit.Popup.Show("No memes here yet", "There are no memes around you right now. New ones appear every day, so check again later!");
                    return;
                }

                // 2) Claim it: the answer contains the EXACT place. Save it at once (survives an app restart).
                var claim = await Api.ClaimCard(best.Id, Session.PlayerId);
                Session.SaveHunt(claim);
                hunt = claim;
                near = false;
                nextCheck = 0f;                              // evaluate the distance on the next frame
                hud.SetRarity(claim.Rarity);
                hud.SetTarget(claim.Lat, claim.Lng);
                started = true;
            }
            catch (ApiException e)
            {
                HuntErrors.From(e);                          // e.g. offline, or someone claimed it a second ago
            }
            finally
            {
                busy = false;
                hud.SetPhase(started ? HuntPhase.Walking : HuntPhase.NoHunt);
            }
        }

        /// <summary>Creates the server player from the onboarding nickname if this device has none yet.</summary>
        static async Awaitable<bool> EnsurePlayer()
        {
            if (Session.HasPlayer) return true;
            var nickname = PlayerPrefs.GetString("Nickname", string.Empty);
            if (string.IsNullOrWhiteSpace(nickname)) nickname = "Hunter";
            var player = await Api.CreatePlayer(nickname);   // ApiException is handled by the caller
            Session.SavePlayer(player);
            return true;
        }

        /// <summary>Forget the local hunt (the card stays claimed on the server).</summary>
        public void CancelHunt()
        {
            hunt = null;
            near = false;
            lastDistanceM = -1;
            Session.ClearHunt();
            MemeSpawner.Hide();
            hud.SetDistance(-1);
            hud.ClearTarget();
            hud.SetPhase(HuntPhase.NoHunt);
        }

        // ---------- Catch: photo -> upload -> reveal ----------

        async void OnCatchPressed()
        {
            if (busy || hunt == null) return;

            // Position at the moment of pressing (before the photo is taken).
            if (!Geo.TryGetFix(out var fix))
            {
                HuntErrors.NoGps();
                return;
            }
            if (Geo.DistanceMeters(fix.Lat, fix.Lng, hunt.Lat, hunt.Lng) > CatchAllowedM)
            {
                UI.Kit.Popup.Show("Too far away", "The meme slipped away! Walk a bit closer and try again.");
                return;
            }

            busy = true;
            try
            {
                // 1) Photo of the AR view: every canvas and the meme itself are hidden for that frame.
                var jpeg = await PhotoCapture.TakeAsync();
                if (jpeg == null) return;                    // cancelled: the hunt stays active, the player can retry

                // 2) Upload (can take many seconds: the backend forwards it to doskaz.kz)
                hud.SetBusy("Catching the meme...");
                var result = await Api.SubmitPhoto(Session.PlayerId, hunt.CardId, fix.Lat, fix.Lng, jpeg);

                if (result.Status == "sent")
                {
                    var caught = hunt;
                    Session.ClearHunt();
                    hunt = null;
                    near = false;
                    lastDistanceM = -1;
                    MemeSpawner.Hide();
                    hud.SetBusy(null);
                    hud.SetDistance(-1);
                    hud.ClearTarget();
                    hud.SetPhase(HuntPhase.NoHunt);

                    var progress = PlayerProgress.RecordCatch(caught.CardId, caught.Rarity);
                    CatchReveal.Show(progress, caught.Rarity, result.Reward);
                    await RefreshPlayer();                    // new coin balance for Profile
                }
                else
                {
                    // The backend could not hand it to doskaz (e.g. expired token): the catch fails, the hunt stays.
                    HuntErrors.Generic("The meme got away this time. Please try catching it again in a moment.");
                }
            }
            catch (ApiException e)
            {
                HuntErrors.From(e);                           // offline / timeout / server error: the hunt stays active
            }
            finally
            {
                busy = false;
                hud.SetBusy(null);
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

            GUILayout.BeginArea(new Rect(8, 140, 190, 280));
            GUILayout.Label(hunt == null ? "No active hunt" : $"Distance: {lastDistanceM:F0} m (GPS ±{lastAccuracyM:F0} m)");

            GUI.enabled = !busy;
            if (hunt != null)
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
