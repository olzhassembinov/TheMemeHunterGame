using System.Collections.Generic;
using System.Linq;
using MemeHunter.Game;
using MemeHunter.UI.Kit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemeHunter.UI.Profile
{
    /// <summary>
    /// Replaces the old Profile / Collections / Meme Card screens with 1:1 copies of the Figma "Profile" section
    /// (158:15, 203:4, 196:159) built from code, and makes the Map the home screen.
    /// Hooks into the main scene at load time, so the scene file is not edited.
    /// </summary>
    public static class ProfileSection
    {
        public const string MemeCardId = "MemeCard";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Hook() => SceneManager.sceneLoaded += (_, _) => Install();

        static void Install()
        {
            var navigator = Object.FindFirstObjectByType<ScreenNavigator>(FindObjectsInactive.Include);
            if (navigator == null) return;
            navigator.SetInitialScreen(AppNav.Map);                            // the Map is the main screen

            // Players who started before the consent screens existed are asked once here.
            if (!Onboarding.Consent.Accepted)
                Onboarding.ConsentFlow.Run(null);

            HideByName("Persistent UI");
            // Old close buttons (e.g. "Notifications Back") went to a fixed screen: make them real Back buttons.
            foreach (var button in Object.FindObjectsByType<ScreenNavigationButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (button.name.EndsWith(" Back")) button.GoesBack = true;                                       // old chrome: not in the Figma screens
            foreach (var view in Object.FindObjectsByType<ScreenView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                switch (view.ScreenId)
                {
                    case AppNav.Profile: Replace<ProfileScreen>(view); break;
                    case AppNav.Collections: Replace<CollectionScreen>(view); break;
                    case MemeCardId: Replace<MemeCardScreen>(view); break;
                }
            }
        }

        static void Replace<T>(ScreenView view) where T : FigmaScreen
        {
            if (view.GetComponentInChildren<T>(true) != null) return;

            // Old presenters live on the view itself: switch them off before their Start runs.
            foreach (var behaviour in view.GetComponents<MonoBehaviour>())
                if (!(behaviour is ScreenView)) behaviour.enabled = false;
            foreach (Transform child in view.transform)
                child.gameObject.SetActive(false);

            var root = Ui.Rect(typeof(T).Name, view.transform).Stretch();
            root.gameObject.AddComponent<T>();
        }

        static void HideByName(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name) t.gameObject.SetActive(false);
        }

        // ---------- Data shared by the screens ----------

        /// <summary>All catalog memes, rarest first (Figma shows legendaries at the top).</summary>
        public static IEnumerable<MemeData> CatalogRarestFirst() =>
            MemeCatalog.All.OrderByDescending(m => m.Rarity).ThenBy(m => m.DisplayName);

        public static int Count(MemeData meme) => PlayerProgress.CountOf(meme.MemeId);
    }

    /// <summary>A code-built screen that rebuilds its content each time it becomes visible (fresh data).</summary>
    public abstract class FigmaScreen : MonoBehaviour
    {
        protected RectTransform Root { get; private set; }

        void OnEnable() => Render();

        /// <summary>Builds the screen from scratch (also used by the editor UI previews).</summary>
        public void Render()
        {
            if (Root == null) Root = (RectTransform)transform;
            for (var i = Root.childCount - 1; i >= 0; i--)
            {
                var child = Root.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);   // edit mode: UI previews
            }

            var bg = Ui.Rounded("Background", Root, Fig.Background, 0f);
            bg.rectTransform.Stretch();
            Build(Root);
        }

        protected abstract void Build(RectTransform root);

        /// <summary>The round close button of every Figma screen (63 px, 17 px above the bottom edge): goes Back.</summary>
        protected static void CloseButton(RectTransform root)
        {
            var theme = UiTheme.Get();
            var close = Ui.IconButton("Close", root, theme != null ? theme.discardButton : null, AppNav.Back);
            ((RectTransform)close.transform).AtBottom(218f, 1000f, 63f, 63f);
        }
    }
}
