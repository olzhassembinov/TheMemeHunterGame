using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MemeHunter.UI.Kit
{
    /// <summary>
    /// Helpers for copying Figma frames 1:1. Coordinates are Figma's: x/y from the frame's top-left, in the
    /// 498x1080 reference space that the app canvases use.
    /// </summary>
    public static class Fig
    {
        public static readonly Color MainText = Hex("262E5B");       // var(--main-text)
        public static readonly Color SubText = Hex("A6AED7");        // var(--required-color)
        public static readonly Color Background = Hex("C3F5FF");     // var(--background)
        public static readonly Color Water = Hex("57A8FF");          // var(--water): card tint / image border
        public static readonly Color XpTrack = Hex("43E7E5");
        public static readonly Color XpFrom = Hex("55BBFF");
        public static readonly Color XpTo = Hex("0099FF");
        public static readonly Color AvatarRing = Hex("7BB8FF");

        /// <summary>Place by top-left corner like Figma (anchored to the top-left of the parent).</summary>
        public static RectTransform At(this RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>Place by top-left corner relative to the parent's BOTTOM edge (for elements that sit at the bottom).</summary>
        public static RectTransform AtBottom(this RectTransform rect, float x, float yFromTop, float w, float h, float frameHeight = 1080f)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, frameHeight - yFromTop);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public static TextMeshProUGUI Body(Transform parent, string value, float size, Color color, TextWeight weight = TextWeight.Regular)
        {
            var text = Ui.Text("Text", parent, value, size, color);
            var theme = UiTheme.Get();
            if (theme != null) text.font = theme.Body(weight);
            text.alignment = TextAlignmentOptions.Left;
            return text;
        }

        public static TextMeshProUGUI Display(Transform parent, string value, float size, Color color)
        {
            var text = Ui.Text("Title", parent, value, size, color, display: true);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// <summary>Figma letter-spacing in px -> TMP character spacing (1/100 em).</summary>
        public static void Tracking(this TMP_Text text, float pixels) => text.characterSpacing = pixels / text.fontSize * 100f;

        static readonly Dictionary<(Material, Color), Material> glowMaterials = new Dictionary<(Material, Color), Material>();

        /// <summary>Figma text-shadow 0 0 7px colour: a soft outer glow in the same colour (TMP glow).</summary>
        public static void Glow(this TMP_Text text, Color color)
        {
            var baseMaterial = text.font != null ? text.font.material : null;
            if (baseMaterial == null) return;
            if (!glowMaterials.TryGetValue((baseMaterial, color), out var material) || material == null)
            {
                var shader = Shader.Find("TextMeshPro/Distance Field");
                material = new Material(baseMaterial);
                if (shader != null) material.shader = shader;
                material.EnableKeyword(ShaderUtilities.Keyword_Glow);
                material.SetColor(ShaderUtilities.ID_GlowColor, new Color(color.r, color.g, color.b, 0.75f));
                material.SetFloat(ShaderUtilities.ID_GlowOuter, 0.55f);
                material.SetFloat(ShaderUtilities.ID_GlowPower, 0.7f);
                glowMaterials[(baseMaterial, color)] = material;
            }
            text.fontSharedMaterial = material;
        }

        /// <summary>1 px line (Figma header / section lines).</summary>
        public static RectTransform Line(Transform parent, float x, float y, float w, Color color)
        {
            var line = Ui.Rounded("Line", parent, color, 0f);
            line.raycastTarget = false;
            return line.rectTransform.At(x, y, w, 1f);
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
