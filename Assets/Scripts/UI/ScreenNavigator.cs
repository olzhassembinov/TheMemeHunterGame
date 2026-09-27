using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemeHunter.UI
{
    public sealed class ScreenNavigator : MonoBehaviour
    {
        [SerializeField] ScreenView[] screens = Array.Empty<ScreenView>();
        [SerializeField] string initialScreenId;

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

            if (!string.IsNullOrWhiteSpace(initialScreenId))
                Show(initialScreenId);
        }

        public bool Show(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                return false;

            foreach (var screen in registeredScreens)
            {
                if (screen == null || !string.Equals(screen.ScreenId, screenId, StringComparison.Ordinal))
                    continue;

                if (activeScreen != null && activeScreen != screen)
                    activeScreen.SetVisible(false);

                activeScreen = screen;
                activeScreen.SetVisible(true);
                return true;
            }

            return false;
        }

        public bool Back()
        {
            return Show(initialScreenId);
        }
    }
}