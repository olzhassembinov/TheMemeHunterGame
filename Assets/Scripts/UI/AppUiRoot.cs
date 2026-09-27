using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [RequireComponent(typeof(Canvas))]
    public sealed class AppUiRoot : MonoBehaviour
    {
        const float ReferenceWidth = 498f;
        const float ReferenceHeight = 1080f;

        [SerializeField] bool persistAcrossScenes = true;
        [SerializeField] RectTransform safeAreaRoot;
        [SerializeField] RectTransform screensRoot;
        [SerializeField] RectTransform persistentRoot;
        [SerializeField] GameObject persistentBottomDecorationPrefab;
        [SerializeField] string welcomeSceneName = "WelcomeScreen";

        static AppUiRoot instance;
        ScreenView welcomeView;

        public RectTransform ScreensRoot => screensRoot;
        public RectTransform PersistentRoot => persistentRoot;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            ConfigureCanvas();
            EnsureHierarchy();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SetWelcomeVisible(SceneManager.GetActiveScene());

            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this)
                instance = null;
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetWelcomeVisible(scene);
        }

        void SetWelcomeVisible(Scene scene)
        {
            if (welcomeView != null)
                welcomeView.SetVisible(string.Equals(scene.name, welcomeSceneName, System.StringComparison.Ordinal));
        }

        void ConfigureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        void EnsureHierarchy()
        {
            safeAreaRoot = EnsureRectTransform(safeAreaRoot, "Safe Area", transform);
            var safeArea = safeAreaRoot.GetComponent<SafeAreaFitter>();
            if (safeArea == null)
                safeArea = safeAreaRoot.gameObject.AddComponent<SafeAreaFitter>();
            safeArea.Target = safeAreaRoot;

            screensRoot = EnsureRectTransform(screensRoot, "Screens", safeAreaRoot);
            persistentRoot = EnsureRectTransform(persistentRoot, "Persistent UI", safeAreaRoot);

            if (persistentBottomDecorationPrefab != null && persistentRoot.GetComponentInChildren<PersistentBottomDecoration>(true) == null)
                Instantiate(persistentBottomDecorationPrefab, persistentRoot, false);

            var welcomeViewTransform = EnsureRectTransform(null, "Welcome", screensRoot);
            welcomeView = welcomeViewTransform.GetComponent<ScreenView>();
            if (welcomeView == null)
                welcomeView = welcomeViewTransform.gameObject.AddComponent<ScreenView>();
            welcomeView.Configure("Welcome");

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (child != safeAreaRoot)
                    child.SetParent(welcomeViewTransform, false);
            }

            var navigator = screensRoot.GetComponent<ScreenNavigator>();
            if (navigator == null)
                navigator = screensRoot.gameObject.AddComponent<ScreenNavigator>();
            navigator.RegisterScreen(welcomeView);
            navigator.SetInitialScreen("Welcome");
        }

        static RectTransform EnsureRectTransform(RectTransform current, string objectName, Transform parent)
        {
            if (current == null)
            {
                var child = new GameObject(objectName, typeof(RectTransform));
                current = child.GetComponent<RectTransform>();
            }

            current.SetParent(parent, false);
            current.anchorMin = Vector2.zero;
            current.anchorMax = Vector2.one;
            current.anchoredPosition = Vector2.zero;
            current.sizeDelta = Vector2.zero;
            current.localScale = Vector3.one;
            return current;
        }
    }
}