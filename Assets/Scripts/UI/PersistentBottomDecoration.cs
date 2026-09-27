using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [DisallowMultipleComponent]
    public sealed class PersistentBottomDecoration : MonoBehaviour
    {
        [SerializeField] RectTransform layoutRoot;
        [SerializeField] RectTransform leftLineRect;
        [SerializeField] RectTransform rightLineRect;
        [SerializeField] Image leftLine;
        [SerializeField] Image rightLine;
        [SerializeField] float lineWidth = 109f;
        [SerializeField] float lineHeight = 1f;
        [SerializeField] float centerGap = 32f;
        [SerializeField] float bottomInset = 16f;
        [SerializeField] Color lineColor = MemeHunterUiColors.DarkNavy;
        [SerializeField] bool persistAcrossScenes = true;

        static PersistentBottomDecoration instance;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            ApplyLayout();
            if (persistAcrossScenes)
                DontDestroyOnLoad(transform.root.gameObject);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void OnValidate()
        {
            ApplyLayout();
        }

        public void SetLayout(float width, float height, float gap, float safeAreaBottomInset)
        {
            lineWidth = Mathf.Max(0f, width);
            lineHeight = Mathf.Max(0f, height);
            centerGap = Mathf.Max(0f, gap);
            bottomInset = Mathf.Max(0f, safeAreaBottomInset);
            ApplyLayout();
        }

        public void SetColor(Color color)
        {
            lineColor = color;
            ApplyLayout();
        }

        void ApplyLayout()
        {
            if (layoutRoot == null || leftLineRect == null || rightLineRect == null)
                return;

            layoutRoot.anchorMin = new Vector2(0.5f, 0f);
            layoutRoot.anchorMax = new Vector2(0.5f, 0f);
            layoutRoot.pivot = new Vector2(0.5f, 0f);
            layoutRoot.anchoredPosition = new Vector2(0f, bottomInset);
            layoutRoot.sizeDelta = new Vector2(lineWidth * 2f + centerGap, lineHeight);

            ConfigureLine(leftLineRect, -centerGap * 0.5f - lineWidth * 0.5f);
            ConfigureLine(rightLineRect, centerGap * 0.5f + lineWidth * 0.5f);

            if (leftLine != null)
                leftLine.color = lineColor;
            if (rightLine != null)
                rightLine.color = lineColor;
        }

        void ConfigureLine(RectTransform rect, float xPosition)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(xPosition, 0f);
            rect.sizeDelta = new Vector2(lineWidth, lineHeight);
        }
    }
}