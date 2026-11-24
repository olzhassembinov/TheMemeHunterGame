using System.Collections;
using System.Linq;
using MemeHunter.Game;
using MemeHunter.UI;
using MemeHunter.UI.Avatars;
using MemeHunter.UI.Hunt;
using MemeHunter.UI.Profile;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Walks through every scene and page without the server, pressing the real buttons:
    /// Welcome -> Profile (avatar picker) -> close -> Map (direction bar, navigation column, MH toggle) ->
    /// Leaderboard (Global, Me) -> Collections (filter) -> Notifications -> Profile -> Donate -> Hunt (camera, side
    /// menu) -> Leaderboard from the camera -> camera -> Back. Android Back is pressed in between.
    /// Saves a screenshot of each step, logs which page is on screen, and fails if anything did not work.
    /// Batch: Unity -batchmode -projectPath . -executeMethod MemeHunter.Editor.AppFlowSimulation.Run (env UI_PREVIEW_DIR).
    /// Also in the menu: Meme Hunter / Simulate app flow.
    /// </summary>
    public static class AppFlowSimulation
    {
        const string Scene = "Assets/Scenes/WelcomeScreen.unity";

        public static void Run() => SimulationRunner.Start<AppFlowDriver>(Scene, quit: true);

        [MenuItem("Meme Hunter/Simulate app flow")]
        static void RunFromMenu() => SimulationRunner.Start<AppFlowDriver>(Scene, quit: false);
    }

    public sealed class AppFlowDriver : SimulationDriver
    {
        const string AvatarKey = "AvatarIndex";                   // AvatarCatalog's saved choice (restored afterwards)
        static readonly string[] MapNavigation = { "Profile", "Notifications", "Collections", "Leaderboard", "Donate" };
        static readonly string[] CameraMenu = { "Profile", "Map", "Notifications", "Collections", "Leaderboard", "Donate" };

        int failures;

        protected override IEnumerator Run()
        {
            // No avatar chosen yet, so Profile asks for one: it must ask once, and only while Profile is shown.
            PlayerPrefs.DeleteKey(AvatarKey);
            PlayerPrefs.Save();

            yield return Settle(1.5f);
            Expect("Welcome", "flow_01_welcome", pickers: 0);

            AppNav.OpenScreen(AppNav.Profile);                    // DEV menu: Profile (from another scene)
            yield return Settle();
            Expect(AppNav.Profile, "flow_02_profile", pickers: 1);
            Press("Accept");                                      // keep the highlighted avatar
            yield return Settle(0.3f);
            Check(AvatarCatalog.HasChoice && !AvatarPicker.IsOpen, "avatar saved and the picker closed");

            Press("Close");                                       // Profile's round close button: Back -> the Map
            yield return Settle();
            Expect(AppNav.Map, "flow_03_map", pickers: 0);

            // The Map: direction bar at the bottom, navigation column, MH toggle.
            Check(FindFirstObjectByType<DirectionPanel>() != null, "direction bar on the Map");
            Check(MapNavigation.All(Visible) && Visible("Menu Toggle") && Visible("Hunt"), "all navigation buttons visible");
            Press("Menu Toggle");
            yield return null;
            Check(!MapNavigation.Any(Visible) && Visible("Menu Toggle"), "MH hides the navigation");
            Press("Menu Toggle");
            yield return null;
            Check(MapNavigation.All(Visible), "MH shows the navigation again");

            // Leaderboard: both pages, then Android Back.
            Press("Leaderboard");
            yield return Settle();
            Expect(AppNav.Leaderboard, "flow_04_leaderboard_global");
            var rows = Named("Row") + Named("You");
            Check(Named("Step 1") == 1 && Named("Step 2") == 1 && Named("Step 3") == 1 && rows == LocalLeague.GroupSize - 3,
                $"podium + {rows} rows = a group of {LocalLeague.GroupSize}");
            Check(Named("Your Rank") == 1, "player's own row pinned at the bottom");
            Press("ME");
            yield return Settle(0.5f);
            Check(LeaderboardScreen.Shown == LeaderboardScreen.Page.Me && Named("This Week") == 1 && Named("League Goal") == 1, "ME page");
            Shot("flow_05_leaderboard_me");
            Press("GLOBAL");
            yield return Settle(0.5f);
            Check(LeaderboardScreen.Shown == LeaderboardScreen.Page.Global, "GLOBAL page again");
            BackButton.Handle();
            yield return Settle();
            Expect(AppNav.Map, "flow_06_back_to_map");

            // Collections with a filter, Notifications, Profile (no picker now), each closed with Android Back.
            Press("Collections");
            yield return Settle();
            Press("Filter Rare");
            yield return Settle(0.3f);
            Expect(AppNav.Collections, "flow_07_collections_rare");
            Press("Filter Rare");                                 // off again (the filter is kept while the app runs)
            BackButton.Handle();
            yield return Settle();
            Expect(AppNav.Map, null);

            Press("Notifications");
            yield return Settle();
            Expect(AppNav.Notifications, "flow_08_notifications");
            BackButton.Handle();
            yield return Settle();
            Expect(AppNav.Map, null);

            Press("Profile");
            yield return Settle();
            Expect(AppNav.Profile, null, pickers: 0);
            BackButton.Handle();
            yield return Settle();
            Expect(AppNav.Map, null);

            // Donate: the overlay, the "Coming soon" popup, Back closes both.
            Press("Donate");
            yield return Settle(0.5f);
            Check(GameObject.Find("Donate Screen") != null, "Donate screen open");
            Press("Donate 1");
            yield return Settle(0.5f);
            Shot("flow_09_donate_popup");
            BackButton.Handle();                                  // closes the popup
            yield return Settle(0.3f);
            BackButton.Handle();                                  // closes the Donate screen
            yield return Settle(0.3f);
            Check(GameObject.Find("Donate Screen") == null, "Back closed the popup and the Donate screen");
            Expect(AppNav.Map, null);

            // Hunt button -> the camera. Without a server the hunt cannot start: "No connection" (or a started hunt).
            Press("Hunt");
            yield return Settle(3f);
            var hunt = HuntController.Instance != null;
            Check(hunt, $"Hunt opened the camera scene ({SceneManager.GetActiveScene().name})");
            var popup = FindFirstObjectByType<UI.Kit.Popup>();
            Log($"after Hunt: {(popup != null ? "popup '" + popup.Title.text + "'" : "no popup")}, active hunt: {Net.Session.ActiveHunt != null}");
            Shot("flow_10_camera_after_hunt");
            if (popup != null) BackButton.Handle();
            yield return Settle(0.5f);
            if (Net.Session.ActiveHunt == null)
                Check(CameraMenu.All(Visible) && Visible("Menu Toggle"), "camera side menu: all buttons visible");
            Shot("flow_11_camera_menu");

            // From the camera straight to the Leaderboard (another scene), then Back to the Map.
            Press("Leaderboard");
            yield return Settle(2f);
            Expect(AppNav.Leaderboard, "flow_12_leaderboard_from_camera");
            BackButton.Handle();
            yield return Settle();
            Expect(AppNav.Map, null);

            AppNav.OpenHunt(startHunting: false);                 // DEV menu: SampleScene (camera)
            yield return Settle(2f);
            BackButton.Handle();                                  // Android Back on the camera: the Map
            yield return Settle(2f);
            Expect(AppNav.Map, "flow_13_camera_back_to_map", pickers: 0);
            BackButton.Handle();                                  // on the Map: the app goes to the background, the page stays
            yield return Settle(0.3f);
            Expect(AppNav.Map, null);

            Failed = failures > 0;
            Log(Failed ? $"FAIL: {failures} check(s) failed" : "PASS: every scene, page and button worked");
        }

        static IEnumerator Settle(float seconds = 1f)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        void Check(bool ok, string what)
        {
            if (!ok) failures++;
            Log($"{(ok ? "ok" : "FAIL")}: {what}");
        }

        /// <summary>Presses the active button with this name (like a tap).</summary>
        void Press(string name)
        {
            var button = FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == name && b.isActiveAndEnabled && b.interactable);
            if (button == null) { Check(false, $"button '{name}' is on screen"); return; }
            button.onClick.Invoke();
            Log("pressed " + name);
        }

        static bool Visible(string name) =>
            FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b => b.name == name && b.isActiveAndEnabled);

        static int Named(string name) =>
            FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Count(t => t.name == name && t.gameObject.activeInHierarchy);

        /// <summary>
        /// Checks that exactly this page is on screen (and, when given, how many avatar pickers are open),
        /// then takes a screenshot (when a name is given).
        /// </summary>
        void Expect(string screenId, string shot, int pickers = -1)
        {
            var visible = FindObjectsByType<ScreenView>(FindObjectsSortMode.None)      // active ones only
                .Where(v => v.gameObject.activeInHierarchy).Select(v => v.ScreenId).OrderBy(s => s).ToList();
            var openPickers = FindObjectsByType<UI.Kit.Popup>(FindObjectsSortMode.None)
                .Count(p => p.Title != null && p.Title.text == "Choose your hunter");
            var ok = visible.Count == 1 && visible[0] == screenId && (pickers < 0 || openPickers == pickers);
            if (!ok) failures++;
            var shown = visible.Count == 0 ? "nothing" : string.Join(", ", visible);
            var pickerNote = pickers < 0 ? "" : $", avatar pickers: {openPickers} (expected {pickers})";
            Log($"{(ok ? "ok" : "FAIL")}: expected {screenId}, on screen: {shown}{pickerNote} (scene {SceneManager.GetActiveScene().name})");
            if (shot != null) Shot(shot);
        }
    }
}
