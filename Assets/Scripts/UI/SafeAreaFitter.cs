using UnityEngine;

namespace MemeHunter.UI
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] RectTransform target;

        Rect lastSafeArea;
        int lastScreenWidth;
        int lastScreenHeight;

        public RectTransform Target
        {
            get => target;
            set => target = value;
        }

        void Awake()
        {
            if (target == null)
                target = transform as RectTransform;
            ApplySafeArea();
        }

        void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
                ApplySafeArea();
        }

        void ApplySafeArea()
        {
            if (target == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            var safeArea = Screen.safeArea;
            target.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            target.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }
}