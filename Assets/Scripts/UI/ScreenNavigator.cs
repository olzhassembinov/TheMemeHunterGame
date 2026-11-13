using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemeHunter.UI
{
    public sealed class ScreenNavigator : MonoBehaviour
    {
        [SerializeField] ScreenView[] screens = Array.Empty<ScreenView>();
        [SerializeField] string initialScreenId;

        /// <summary>The home screen: Back ends here, and opening it starts a fresh history.</summary>
        public const string HomeScreenId = "Map";
        const int MaxHistory = 30;

        // Screens visited before the current one (newest last). Static so it survives a trip to the AR scene.
        static readonly List<string> history = new List<string>();

        ScreenView activeScreen;
        readonly List<ScreenView> registeredScreens = new List<ScreenView>();

        public ScreenView ActiveScreen => activeScreen;

        public void RegisterScreen(ScreenView screen)
        {
            if (screen != null && !registeredScreens.Contains(screen))
                registeredScreens.Add(screen);
        }

        public void SetInitialScreen(string screenId)
        {
            initialScreenId = screenId;
        }

        void Start()
        {
            foreach (var screen in screens)
                RegisterScreen(screen);

            foreach (var screen in registeredScreens)
            {
                if (screen != null)
                    screen.SetVisible(false);
            }

            // A screen requested from another scene (e.g. the AR hunt's side menu) wins over the initial one.
            var pending = AppNav.TakePendingScreen();
            if (!string.IsNullOrWhiteSpace(pending) && Show(pending))
                return;

            if (!string.IsNullOrWhiteSpace(initialScreenId))
                Show(initialScreenId);
        }

        /// <summary>Opens a screen and remembers the current one, so Back can return to it.</summary>
        public bool Show(string screenId) => Show(screenId, true);

        bool Show(string screenId, bool remember)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                return false;

            foreach (var screen in registeredScreens)
            {
                if (screen == null || !string.Equals(screen.ScreenId, screenId, StringComparison.Ordinal))
                    continue;

                if (screenId == HomeScreenId)
                    history.Clear();                                   // the Map is the root: nothing behind it
                else if (remember && activeScreen != null && activeScreen != screen)
                {
                    history.Add(activeScreen.ScreenId);
                    if (history.Count > MaxHistory) history.RemoveAt(0);
                }

                if (activeScreen != null && activeScreen != screen)
                    activeScreen.SetVisible(false);

                activeScreen = screen;
                activeScreen.SetVisible(true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns to the previous screen (Profile -> Collections -> Meme card, then back, back = Profile).
        /// With no earlier screen left it opens the Map.
        /// </summary>
        public bool Back()
        {
            while (history.Count > 0)
            {
                var previous = history[history.Count - 1];
                history.RemoveAt(history.Count - 1);
                if (activeScreen != null && previous == activeScreen.ScreenId) continue;
                if (Show(previous, false)) return true;
            }
            return Show(HomeScreenId, false);
        }
    }
}