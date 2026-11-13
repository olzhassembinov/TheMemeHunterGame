using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemeHunter.UI.Kit
{
    /// <summary>
    /// Small factory for UI built from code in the Figma style (498x1080 reference canvas, like the designed scenes).
    /// Used by popups, the avatar picker and the hunt overlays, so none of them need scene or prefab edits.
    /// </summary>
    public static class Ui
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(498f, 1080f);

        // Figma "Pop-up Notifications" (364:118) card colours.
        public static readonly Color PopupFill = MemeHunterUiColors.LightCyanBackground;
        public static readonly Color PopupBorder = MemeHunterUiColors.LightPurple;
        public static readonly Color Ink = new Color32(17, 17, 17, 255);
        public static readonly Color Dim = new Color(0.10f, 0.14f, 0.20f, 0.62f);

        /// <summary>A screen-space overlay canvas above the scene UI. Optionally survives scene loads.</summary>
        public static RectTransform OverlayCanvas(string name, int sortingOrder, bool persistent = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (persistent)
                UnityEngine.Object.DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return (RectTransform)go.transform;
        }

        /// <summary>Buttons on a code-built canvas need an EventSystem; scenes normally have one already.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            // CanvasRenderer up front: Unity 6 UGUI does not always add it when a Graphic/Mask is added from code.
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        /// <summary>Places a rect by anchor point (0..1) with a pixel offset and size.</summary>
        public static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return rect;
        }

        public static RoundedRectGraphic Rounded(string name, Transform parent, Color color, float radius)
        {
            var rect = Rect(name, parent);
            var graphic = rect.gameObject.AddComponent<RoundedRectGraphic>();
            graphic.color = color;
            graphic.CornerRadius = radius;
            return graphic;
        }

        /// <summary>A rounded card with an outline, drawn as two rounded rects (the outline one behind).</summary>
        public static RectTransform BorderedCard(string name, Transform parent, Color fill, Color border, float radius, float borderWidth)
        {
            var outer = Rounded(name, parent, border, radius);
            var inner = Rounded("Fill", outer.transform, fill, Mathf.Max(0f, radius - borderWidth));
            inner.rectTransform.Stretch(borderWidth);
            inner.raycastTarget = false;
            return outer.rectTransform;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, bool preserveAspect = true)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, bool display = false)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            var theme = UiTheme.Get();
            if (theme != null)
                text.font = display ? theme.displayFont : theme.bodyFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// A picture that fills a rounded box and is cropped (not stretched), like the meme cards in Figma.
        /// Returns the box; the picture is its child.
        /// </summary>
        public static RectTransform CroppedPicture(string name, Transform parent, Sprite sprite, float radius)
        {
            var box = Rounded(name, parent, new Color32(220, 236, 255, 255), radius);
            box.raycastTarget = false;
            box.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            var picture = Image("Picture", box.transform, sprite, false);
            picture.rectTransform.anchorMin = Vector2.zero;
            picture.rectTransform.anchorMax = Vector2.one;
            picture.rectTransform.sizeDelta = Vector2.zero;
            if (sprite != null)
            {
                var fitter = picture.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
            return box.rectTransform;
        }

        /// <summary>Round Figma icon button (Accept / Discard / any 'Blue Buttons' sprite).</summary>
        public static Button IconButton(string name, Transform parent, Sprite sprite, Action onClick)
        {
            var image = Image(name, parent, sprite);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Blue gradient pill with a label (Figma CONTINUE / Catch buttons).</summary>
        public static Button PillButton(string name, Transform parent, string label, Action onClick, float fontSize = 28f)
        {
            var theme = UiTheme.Get();
            var image = Image(name, parent, theme != null ? theme.pillButton : null, false);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", image.transform, label, fontSize, Color.white);
            text.rectTransform.Stretch();
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            return button;
        }
    }
}
