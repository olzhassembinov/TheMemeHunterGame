using System.Linq;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;

namespace MemeHunter.UI.Profile
{
    /// <summary>
    /// Figma "Meme card" (196:159): MEME CARD header, the picture 383x432 with a 5 px light rarity border and a
    /// glow, the name in Jaro 50, Rarity / Price rows and the description. The close button goes Back (to the screen
    /// the card was opened from).
    /// </summary>
    public sealed class MemeCardScreen : FigmaScreen
    {
        static MemeData shown;

        public static void Open(MemeData meme)
        {
            Select(meme);
            AppNav.OpenScreen(ProfileSection.MemeCardId);
        }

        /// <summary>Chooses the card to show without navigating (editor previews).</summary>
        public static void Select(MemeData meme) => shown = meme;

        protected override void Build(RectTransform root)
        {
            Fig.Line(root, 0f, 52f, 101f, Fig.MainText);
            Fig.Line(root, 397f, 52f, 101f, Fig.MainText);
            var header = Fig.Body(root, "MEME CARD", 15f, Fig.SubText, TextWeight.Bold);
            header.alignment = TextAlignmentOptions.Center;
            header.Tracking(1.65f);
            header.rectTransform.At(145f, 43f, 206f, 18f);

            CloseButton(root);

            var meme = shown != null ? shown : ProfileSection.CatalogRarestFirst().FirstOrDefault();
            if (meme == null) return;
            var color = Rarity.ColorOf(meme.Rarity);

            // Glow (Figma: box-shadow 0 0 16px rarity colour) as soft layers behind the picture.
            for (var i = 3; i >= 1; i--)
            {
                var glow = Ui.Rounded("Glow", root, color.WithAlpha(0.16f), 40f + i * 5f);
                glow.raycastTarget = false;
                glow.rectTransform.At(58f - i * 5f, 110f - i * 5f, 383f + i * 10f, 432f + i * 10f);
            }

            // 5 px border in a lighter tint of the rarity colour, picture cropped to fill, radius 40.
            var border = Ui.Rounded("Picture Border", root, Color.Lerp(color, Color.white, 0.45f), 40f);
            border.raycastTarget = false;
            border.rectTransform.At(58f, 110f, 383f, 432f);
            Ui.CroppedPicture("Picture", border.transform, meme.Artwork, 35f).Stretch(5f);

            var name = Fig.Display(root, meme.DisplayName, 50f, Fig.MainText);
            name.alignment = TextAlignmentOptions.Center;
            name.enableAutoSizing = true;
            name.fontSizeMin = 28f;
            name.fontSizeMax = 50f;
            name.rectTransform.At(58f, 572f, 383f, 56f);

            Row(root, "Rarity:", meme.Rarity.ToString().ToUpperInvariant(), 636f, color);
            Row(root, "Price:", meme.Price + "$", 671f, color);

            // Description: Anuphan 18, first line indented like the Figma text.
            var description = Fig.Body(root, "<space=48>" + meme.Description, 18f, Fig.MainText);
            description.alignment = TextAlignmentOptions.TopLeft;
            description.enableAutoSizing = true;
            description.fontSizeMin = 13f;
            description.fontSizeMax = 18f;
            description.rectTransform.At(58f, 738f, 383f, 210f);
        }

        static void Row(RectTransform root, string label, string value, float y, Color color)
        {
            var l = Fig.Body(root, label, 20f, Fig.MainText, TextWeight.Light);
            l.alignment = TextAlignmentOptions.MidlineLeft;
            l.rectTransform.At(123f, y, 120f, 24f);

            var v = Fig.Display(root, value, 20f, color);
            v.alignment = TextAlignmentOptions.MidlineRight;
            v.Tracking(8.2f);
            v.Glow(color);
            v.rectTransform.At(196f, y, 180f, 24f);
        }
    }
}
