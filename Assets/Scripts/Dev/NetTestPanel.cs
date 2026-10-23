using System;
using System.Collections.Generic;
using System.Globalization;
using MemeHunter.Game;
using MemeHunter.Net;
using UnityEngine;

namespace MemeHunter.Dev
{
    /// <summary>
    /// Developer-only test screen (opened from the DEV button). Walks through the whole camera chain by hand:
    /// ping -> create player -> find a card -> claim it -> take a photo and upload it.
    /// Every button calls the real Api methods and prints the result in the log area.
    /// </summary>
    public sealed class NetTestPanel : MonoBehaviour
    {
        public bool Visible;

        string urlField;
        string nickname = "Tester";
        string cardId = "";
        // Default test position: near KBTU, Almaty. Replace with the place where your test card is.
        string latText = "43.257300";
        string lngText = "76.928800";

        readonly List<string> lines = new List<string>();
        Vector2 scroll;
        bool busy;

        void Start()
        {
            urlField = ApiConfig.BaseUrl;
            Log("Base URL: " + ApiConfig.BaseUrl);
            Log(Session.HasPlayer ? "Saved player id: " + Session.PlayerId : "No saved player yet");
        }

        void OnGUI()
        {
            if (!Visible) return;

            var s = DevGui.Scale();
            var w = Screen.width / s;
            var h = Screen.height / s;
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none);   // dim background so text is readable
            GUILayout.BeginArea(new Rect(8, 40, w - 16, h - 48));

            GUILayout.Label("Server address");
            GUILayout.BeginHorizontal();
            urlField = GUILayout.TextField(urlField, GUILayout.Height(28));
            if (GUILayout.Button("Save", GUILayout.Width(60), GUILayout.Height(28)))
            {
                ApiConfig.SetOverride(urlField);
                Log("Base URL now: " + ApiConfig.BaseUrl);
            }
            GUILayout.EndHorizontal();

            GUI.enabled = !busy;   // grey out buttons while a request runs
            Buttons();
            GUI.enabled = true;

            GUILayout.Space(6);
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var line in lines)
                GUILayout.Label(line);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        void Buttons()
        {
            // 1. Is the backend reachable at all?
            if (Button("1. Ping server (GET unknown player)"))
                Run(async () =>
                {
                    try
                    {
                        await Api.GetPlayer("00000000-0000-0000-0000-000000000000");
                        Log("Unexpected: server returned a player for the zero id");
                    }
                    catch (ApiException e) when (e.StatusCode == 404)
                    {
                        Log("OK: server reachable (404 'Player not found' as expected)");
                    }
                });

            // 2. Register a throw-away player and remember its id.
            nickname = GUILayout.TextField(nickname, GUILayout.Height(28));
            if (Button("2. Create player with this nickname"))
                Run(async () =>
                {
                    var p = await Api.CreatePlayer(nickname);
                    Session.SavePlayer(p);
                    Log($"Created: {p.DisplayName} id={p.Id} level={p.Level} coins={p.CurrencyBalance}");
                });

            // 3. Position used for the nearby search and for the submission.
            GUILayout.BeginHorizontal();
            GUILayout.Label("lat", GUILayout.Width(26));
            latText = GUILayout.TextField(latText, GUILayout.Height(28));
            GUILayout.Label("lng", GUILayout.Width(26));
            lngText = GUILayout.TextField(lngText, GUILayout.Height(28));
            GUILayout.EndHorizontal();

            if (Button("3. Nearby cards (fills card id with the first one)"))
                Run(async () =>
                {
                    if (!TryPosition(out var lat, out var lng)) return;
                    var cards = await Api.GetNearby(lat, lng);
                    Log($"{cards.Length} card(s) from server:");
                    foreach (var c in cards)
                        Log($" • {c.Rarity}  ~({c.ApproxLat:F3}, {c.ApproxLng:F3})  id={c.Id}");
                    if (cards.Length > 0)
                        cardId = cards[0].Id;
                });

            GUILayout.Label("Card id");
            cardId = GUILayout.TextField(cardId, GUILayout.Height(28));

            // 4. Claim reveals the exact place; we put it into lat/lng so the submission matches the card.
            if (Button("4. Claim this card (fills exact lat/lng)"))
                Run(async () =>
                {
                    if (!RequirePlayerAndCard()) return;
                    var claim = await Api.ClaimCard(cardId, Session.PlayerId);
                    latText = claim.Lat.ToString("F6", CultureInfo.InvariantCulture);
                    lngText = claim.Lng.ToString("F6", CultureInfo.InvariantCulture);
                    Log($"Claimed {claim.Rarity} card at {latText}, {lngText}");
                });

            // 5. The camera feature: photo -> POST /submissions.
            if (Button("5. TAKE PHOTO + SUBMIT"))
                Run(async () =>
                {
                    if (!RequirePlayerAndCard() || !TryPosition(out var lat, out var lng)) return;

                    var jpeg = await PhotoCapture.TakeAsync();     // camera on the phone, file picker in the Editor
                    if (jpeg == null) { Log("Photo cancelled"); return; }
                    Log($"Photo ready: {jpeg.Length / 1024} KB. Uploading (can take up to a minute)...");

                    var result = await Api.SubmitPhoto(Session.PlayerId, cardId, lat, lng, jpeg);
                    Log($"Submission {result.SubmissionId}: status={result.Status} reward={result.Reward}");
                    Toast.Show(result.Status == "sent"
                        ? $"Meme caught! +{result.Reward} coins"
                        : "Could not send the photo right now.");

                    var player = await Api.GetPlayer(Session.PlayerId);   // refreshed coin balance
                    Session.SetPlayer(player);
                    Log($"Coins now: {player.CurrencyBalance}");
                });

            if (Button("Forget saved player"))
            {
                Session.Clear();
                Log("Saved player cleared");
            }
        }

        // ----- helpers -----

        static bool Button(string text) => GUILayout.Button(text, GUILayout.Height(34));

        bool RequirePlayerAndCard()
        {
            if (!Session.HasPlayer) { Log("Create a player first (button 2)"); return false; }
            if (string.IsNullOrWhiteSpace(cardId)) { Log("No card id: press 3 or paste one"); return false; }
            return true;
        }

        bool TryPosition(out double lat, out double lng)
        {
            // Accept both "43.25" and "43,25" typed on a phone keyboard.
            var okLat = double.TryParse(latText.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out lat);
            var okLng = double.TryParse(lngText.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out lng);
            if (!okLat || !okLng) Log("lat/lng must be numbers like 43.257300");
            return okLat && okLng;
        }

        /// <summary>Runs an async action, blocks double taps, and logs any ApiException instead of crashing.</summary>
        async void Run(Func<Awaitable> action)
        {
            if (busy) return;
            busy = true;
            try { await action(); }
            catch (ApiException e) { Log($"ERROR {e.StatusCode}: {e.Message}"); Toast.Show(e.Message); }
            catch (Exception e) { Log("EXCEPTION: " + e); }
            finally { busy = false; }
        }

        void Log(string text)
        {
            Debug.Log("[NetTest] " + text);
            lines.Add(text);
            if (lines.Count > 40) lines.RemoveAt(0);
            scroll.y = float.MaxValue;   // auto-scroll to the newest line
        }
    }
}
