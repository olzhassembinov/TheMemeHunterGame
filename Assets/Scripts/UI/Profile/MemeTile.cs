using System;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Profile
{
    /// <summary>
    /// The meme card tile from Figma ("Meme1", 191.875 x 286), used on Profile (best cards) and Collections:
    /// 25 % water-blue rounded card, picture 159x132 with a 1 px water border, Jaro name, "Rarity:/Price:" labels
    /// in Anuphan 12 and the values in Jaro 12, letter-spaced, in the rarity colour with a glow.
    /// Uncaught memes are silhouettes ("???"); empty slots are the dashed outline.
    /// </summary>
    public static class MemeTile
    {
        public const float Width = 191.875f;
        public const float Height = 286f;

        public static RectTransform Card(Transform parent, MemeData meme, bool owned, int count, Action onClick)
        {
            var root = Ui.Rect(meme.DisplayName, parent);
            root.sizeDelta = new Vector2(Width, Height);

            var bg = Ui.Rounded("Background", root, Fig.Water.WithAlpha(0.25f), 20f);
            bg.rectTransform.Stretch();

            // Picture: water-blue 1 px border, radius 25, cropped to fill.
            var frame = Ui.Rounded("Picture Border", root, Fig.Water, 25f);
            frame.raycastTarget = false;
            frame.rectTransform.At(17f, 16f, 159f, 132f);
            var picture = Ui.CroppedPicture("Picture", frame.transform, meme.Artwork, 24f);
            picture.Stretch(1f);
            if (!owned)
            {
                foreach (var img in picture.GetComponentsInChildren<Image>())
                    img.color = new Color(0.08f, 0.1f, 0.2f, 1f);                   // silhouette
            }

            var name = Fig.Display(root, owned ? meme.DisplayName : "???", 20f, Fig.MainText);
            name.alignment = TextAlignmentOptions.Center;
            name.rectTransform.At(0f, 157f, Width, 32f);
            name.enableAutoSizing = true;
            name.fontSizeMin = 14f;
            name.fontSizeMax = 20f;

            var color = Rarity.ColorOf(meme.Rarity);
            Label(root, "Rarity:", 206f);
            Label(root, "Price:", 230f);
            Value(root, meme.Rarity.ToString().ToUpperInvariant(), 206f, color);
            Value(root, owned ? meme.Price + "$" : "?", 230f, color);

            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return root;
        }

        public static RectTransform Empty(Transform parent, Action onClick = null)
        {
            var root = Ui.Rect("Empty Slot", parent);
            root.sizeDelta = new Vector2(Width, Height);
            var dashes = Ui.Rect("Dashes", root).Stretch().gameObject.AddComponent<DashedBorderGraphic>();
            dashes.color = new Color(0f, 0f, 0f, 0.25f);
            if (onClick != null)
            {
                var button = dashes.gameObject.AddComponent<Button>();
                button.targetGraphic = dashes;
                button.onClick.AddListener(() => onClick());
            }
            else dashes.raycastTarget = false;
            return root;
        }

        static void Label(RectTransform root, string text, float y)
        {
            var label = Fig.Body(root, text, 12f, Fig.MainText);
            label.rectTransform.At(17f, y, 76f, 15f);
        }

        static void Value(RectTransform root, string text, float y, Color color)
        {
            var value = Fig.Display(root, text, 12f, color);
            value.alignment = TextAlignmentOptions.Right;
            value.Tracking(4.92f);
            value.Glow(color);
            value.rectTransform.At(75f, y, 108f, 15.3f);
        }
    }
}
