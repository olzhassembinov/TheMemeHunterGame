using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.Game
{
    /// <summary>
    /// Short white flash after a photo is taken: ShutterFlash.Play().
    /// Creates its own overlay Canvas, no prefab or scene object needed, and removes itself when done.
    /// </summary>
    public static class ShutterFlash
    {
        public static void Play()
        {
            var root = new GameObject("ShutterFlash");

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;                 // above the game UI, just below the Toast (999)

            var imageGo = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(root.transform, false);
            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = imageGo.GetComponent<Image>();
            image.raycastTarget = false;               // never blocks touches
            image.color = new Color(1f, 1f, 1f, 0.9f);

            root.AddComponent<ShutterFlashView>().Init(image);
        }
    }

    public sealed class ShutterFlashView : MonoBehaviour
    {
        const float Duration = 0.25f;

        Image image;
        float elapsed;

        public void Init(Image target) => image = target;

        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / Duration);
            if (image != null)
                image.color = new Color(1f, 1f, 1f, 0.9f * (1f - t));
            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
