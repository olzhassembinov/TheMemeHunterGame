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
    /// (158:15, 203:4, 196:159) built from code, adds the Leaderboard page, and makes the Map the home screen.
    /// Hooks into the main scene at load time, so the scene file is not edited.
    /// </summary>
    public static class ProfileSection
    {
        public const string MemeCardId = "MemeCard";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Hook() => SceneManager.sceneLoaded += (scene, _) => Install(scene);

        static void Install(Scene scene)
        {
            // Only the main scene's own navigator: the Welcome canvas persists into every scene with a navigator
            // that must keep showing the Welcome screen.
            var navigator = AppNav.NavigatorIn(scene);
            if (navigator == null) return;
            navigator.SetInitialScreen(AppNav.Map);                            // the Map is the main screen

            // Players who started before the consent screens existed are asked once here.
            if (!Onboarding.Consent.Accepted)
                Onboarding.ConsentFlow.Run(null);

            HideByName("Persistent UI");
            // Old close buttons (e.g. "Notifications Back") went to a fixed screen: make them real Back buttons.
            foreach (var button in Object.FindObjectsByType<ScreenNavigationButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (button.name.EndsWith(" Back")) button.GoesBack = true;                                       // old chrome: not in the Figma screens
            ScreenView profile = null;
            var hasLeaderboard = false;
            foreach (var view in Object.FindObjectsByType<ScreenView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (view.gameObject.scene != scene) continue;
                switch (view.ScreenId)
                {
                    case AppNav.Profile: Replace<ProfileScreen>(view); profile = view; break;
                    case AppNav.Collections: Replace<CollectionScreen>(view); break;
                    case MemeCardId: Replace<MemeCardScreen>(view); break;
                    case AppNav.Leaderboard: hasLeaderboard = true; break;
                }
            }
            if (!hasLeaderboard && profile != null)
                AddLeaderboard(navigator, profile);
        }

        /// <summary>
        /// The Leaderboard page has no designed screen in the scene: a new page next to Profile, registered with the
        /// navigator before its Start runs (sceneLoaded comes first), so it opens and closes like the others.
        /// </summary>
        static void AddLeaderboard(ScreenNavigator navigator, ScreenView profile)
        {
            var go = new GameObject("Leaderboard", typeof(RectTransform));
            go.SetActive(false);                                                  // built when it is first shown
            var rect = (RectTransform)go.transform;
            var template = (RectTransform)profile.transform;
            rect.SetParent(template.parent, false);
            rect.SetSiblingIndex(template.GetSiblingIndex() + 1);
            rect.anchorMin = template.anchorMin;
            rect.anchorMax = template.anchorMax;
            rect.pivot = template.pivot;
            rect.anchoredPosition = template.anchoredPosition;
            rect.sizeDelta = template.sizeDelta;

            var view = go.AddComponent<ScreenView>();
            view.Configure(AppNav.Leaderboard);
            Ui.Rect(nameof(LeaderboardScreen), rect).Stretch().gameObject.AddComponent<LeaderboardScreen>();
            navigator.RegisterScreen(view);
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
            MemeCatalog.All.OrderByDescending(m => Rarity.Order(m.Rarity)).ThenBy(m => m.DisplayName);

        /// <summary>
        /// How many times the player caught this meme: from the server collection once it was downloaded on this
        /// phone, otherwise from the phone's own record (editor previews, DEV test memes).
        /// </summary>
        public static int Count(MemeData meme) =>
            ServerProgress.HasCollection ? ServerProgress.CountOf(meme.MemeId) : PlayerProgress.CountOf(meme.MemeId);
    }

    /// <summary>A code-built screen that rebuilds its content each time it becomes visible (fresh data).</summary>
    public abstract class FigmaScreen : MonoBehaviour
    {
        protected RectTransform Root { get; private set; }
        RectTransform frame;

        void OnEnable() => Render();

        void OnRectTransformDimensionsChange() => Fit();

        /// <summary>
        /// The 498 x 1080 Figma frame, centred and scaled down when the screen is shorter or narrower (16:9 phones
        /// are ~550 x 978, very tall ones ~480 wide), so nothing is cut off or covered by the close button.
        /// </summary>
        public void Fit()
        {
            if (frame == null || Root == null) return;
            var size = Root.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;
            var scale = Mathf.Min(1f, size.x / frame.sizeDelta.x, size.y / frame.sizeDelta.y);
            frame.localScale = new Vector3(scale, scale, 1f);
        }

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

            // Before, the screens were laid out from the left edge: wider phones got an empty strip on the right and
            // shorter ones (16:9) had the bottom cards under the close button. See Fit().
            frame = Ui.Rect("Frame", Root);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            frame.sizeDelta = Ui.ReferenceResolution;
            Build(frame);
            Fit();
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
