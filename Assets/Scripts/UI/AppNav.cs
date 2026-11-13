using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemeHunter.UI
{
    /// <summary>
    /// Opens app screens from anywhere. The menu screens (Profile, Map, Collections, Notifications, MemeCard) live in
    /// the Profile scene behind a ScreenNavigator; the AR hunt is its own scene.
    /// </summary>
    public static class AppNav
    {
        public const string MainScene = "Profile";
        public const string HuntScene = "SampleScene";

        public const string Profile = "Profile";
        public const string Map = "Map";
        public const string Collections = "Collections";
        public const string Notifications = "Notifications";

        /// <summary>Screen to show once the main scene has loaded (read by ScreenNavigator.Start).</summary>
        public static string PendingScreen { get; private set; }

        /// <summary>True when the hunt scene was opened with the intention to start hunting right away.</summary>
        public static bool StartHuntOnOpen { get; set; }

        public static void OpenScreen(string screenId)
        {
            var navigator = Object.FindFirstObjectByType<ScreenNavigator>();
            if (navigator != null && navigator.Show(screenId))
                return;

            PendingScreen = screenId;
            SceneManager.LoadScene(MainScene);
        }

        /// <summary>The close (X) button everywhere: previous screen, or the Map if there is none.</summary>
        public static void Back()
        {
            var navigator = Object.FindFirstObjectByType<ScreenNavigator>();
            if (navigator != null) { navigator.Back(); return; }
            PendingScreen = Map;
            SceneManager.LoadScene(MainScene);
        }

        /// <summary>Called by ScreenNavigator: returns the pending screen once and forgets it.</summary>
        public static string TakePendingScreen()
        {
            var screen = PendingScreen;
            PendingScreen = null;
            return screen;
        }

        public static void OpenHunt(bool startHunting)
        {
            StartHuntOnOpen = startHunting;
            SceneManager.LoadScene(HuntScene);
        }
    }
}
