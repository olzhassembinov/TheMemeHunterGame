using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.Net
{
    /// <summary>
    /// Short message at the bottom of the screen: Toast.Show("Saved!").
    /// Creates its own overlay Canvas on first use, survives scene changes, needs no prefab.
    /// </summary>
    public static class Toast
    {
        static ToastView view;

        public static void Show(string message, float seconds = 3f)
        {
            Debug.Log("[Toast] " + message);          // also visible in the Console / logcat
            if (view == null)
                view = ToastView.Create();
            view.Show(message, seconds);
        }
    }

    public sealed class ToastView : MonoBehaviour
    {
        GameObject panel;
        TMP_Text label;
        float hideAt;

        public static ToastView Create()
        {
            var root = new GameObject("Toast");
            DontDestroyOnLoad(root);

            // Overlay canvas drawn above everything (sorting order 999), scaled like a 1080x2340 phone.
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2340);
            scaler.matchWidthOrHeight = 0.5f;

            var view = root.AddComponent<ToastView>();

            // Dark bar anchored to the bottom, 90 % of the screen width.
            view.panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            view.panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)view.panel.transform;
            rect.anchorMin = new Vector2(0.05f, 0.08f);
            rect.anchorMax = new Vector2(0.95f, 0.08f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 170f);
            var bg = view.panel.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);
            bg.raycastTarget = false;                 // never blocks button presses

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(view.panel.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(30f, 15f);
            textRect.offsetMax = new Vector2(-30f, -15f);
            view.label = textGo.GetComponent<TextMeshProUGUI>();
            view.label.fontSize = 42;
            view.label.alignment = TextAlignmentOptions.Center;
            view.label.color = Color.white;
            view.label.raycastTarget = false;

            view.panel.SetActive(false);
            return view;
        }

        public void Show(string message, float seconds)
        {
            label.text = message;
            panel.SetActive(true);
            hideAt = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (panel.activeSelf && Time.unscaledTime >= hideAt)
                panel.SetActive(false);
        }
    }
}
