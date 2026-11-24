using System.Collections;
using System.Linq;
using MemeHunter.Game;
using MemeHunter.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Walks through every scene and page without the server, the way the page buttons and the DEV menu do it:
    /// Welcome -> Profile -> Collections -> back -> Map -> Notifications -> back -> Camera (AR) -> back to the Map.
    /// Saves a screenshot of each step, logs which page is on screen, and fails if a page did not open.
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
        const string AvatarKey = "AvatarIndex";                   // AvatarCatalog's saved choice

        int failures;

        protected override IEnumerator Run()
        {
            // No avatar chosen yet, so Profile asks for one: it must ask once, and only while Profile is shown.
            var savedAvatar = PlayerPrefs.GetInt(AvatarKey, -1);
            PlayerPrefs.DeleteKey(AvatarKey);

            yield return Settle(1.5f);
            Expect("Welcome", "flow_1_welcome", pickers: 0);

            AppNav.OpenScreen(AppNav.Profile);                    // DEV menu: Profile (from another scene)
            yield return Settle();
            Expect(AppNav.Profile, "flow_2_profile", pickers: 1);

            AppNav.OpenScreen(AppNav.Collections);                // Profile: empty card slot -> Collections
            yield return Settle();
            Expect(AppNav.Collections, "flow_3_collections", pickers: 0);

            AppNav.Back();                                        // Collections: close button
            yield return Settle();
            Expect(AppNav.Profile, "flow_4_back_to_profile", pickers: 1);

            AppNav.OpenScreen(AppNav.Map);                        // DEV menu: Map
            yield return Settle();
            Expect(AppNav.Map, "flow_5_map", pickers: 0);

            AppNav.OpenScreen(AppNav.Notifications);
            yield return Settle();
            Expect(AppNav.Notifications, "flow_6_notifications");

            AppNav.Back();                                        // Notifications: Back button
            yield return Settle();
            Expect(AppNav.Map, "flow_7_back_to_map");

            AppNav.OpenHunt(startHunting: false);                 // DEV menu: SampleScene (camera)
            yield return Settle(2f);
            var hunt = HuntController.Instance != null;
            Log($"{(hunt ? "ok" : "FAIL")}: camera scene {SceneManager.GetActiveScene().name}, hunt controller: {hunt}");
            if (!hunt) failures++;
            Shot("flow_8_camera");

            AppNav.Back();                                        // from the camera back to the Map (scene load)
            yield return Settle(2f);
            Expect(AppNav.Map, "flow_9_camera_back_to_map", pickers: 0);

            if (savedAvatar >= 0) PlayerPrefs.SetInt(AvatarKey, savedAvatar);   // give the editor its choice back
            PlayerPrefs.Save();

            Failed = failures > 0;
            Log(Failed ? $"FAIL: {failures} step(s) did not open the expected page" : "PASS: every scene and page opened");
        }

        static IEnumerator Settle(float seconds = 1f)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        /// <summary>
        /// Checks that exactly this page is on screen (and, when given, how many avatar pickers are open),
        /// then takes a screenshot.
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
            Shot(shot);
        }
    }
}
