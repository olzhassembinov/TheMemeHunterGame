using System;
using MemeHunter.Game;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;

namespace MemeHunter.UI.Hunt
{
    /// <summary>
    /// The Map's direction bar, docked to the bottom edge like the route panels of 2GIS and Strava: a rectangular
    /// navy panel with a direction tile (lime arrow toward the meme, turned by where the phone points, and an N mark),
    /// what the target is (nearest meme or the claimed one, with its rarity), the distance in big lime digits, and
    /// the Hunt button that opens the camera (gold halo when a meme is close). It replaced the distance pill and
    /// the round compass that used to sit at the top of the Map.
    /// </summary>
    public sealed class DirectionPanel : MonoBehaviour
    {
        /// <summary>Visible height above the bottom edge: other Map elements stay above this.</summary>
        public const float Height = 118f;
        const float Overhang = 32f;          // drawn below the edge, so only the top corners show as rounded
        const float Radius = 20f;
        const float TextLeft = 118f;         // where the caption / distance start (right of the direction tile)
        const float TextRight = 104f;        // space kept free for the Hunt button

        static readonly Color Navy = new Color(0.15f, 0.18f, 0.36f, 0.96f);

        RectTransform needle;
        RectTransform northRing;
        TriangleGraphic arrow;
        TextMeshProUGUI caption;
        TextMeshProUGUI distance;
        RoundedRectGraphic rarityDot;
        RoundedRectGraphic huntHalo;
        bool hasTarget;
        double targetLat, targetLng;
        float shownAngle;
        bool glow;

        public static DirectionPanel Create(RectTransform parent, Action onHunt)
        {
            // Raycast target on: a tap on the panel must not pan the map underneath.
            var background = Ui.Rounded("Direction Panel", parent, Navy, Radius);
            var rect = background.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, -Overhang);
            rect.sizeDelta = new Vector2(0f, Height + Overhang);

            var panel = background.gameObject.AddComponent<DirectionPanel>();
            panel.Build(rect, onHunt);
            return panel;
        }

        // ---------- Called by MapHud ----------

        public void SetTarget(double lat, double lng)
        {
            hasTarget = true;
            targetLat = lat;
            targetLng = lng;
        }

        public void ClearTarget() => hasTarget = false;

        /// <summary>Caption (e.g. "NEAREST MEME · RARE"), its rarity colour (null: no dot) and the big value line.</summary>
        public void Show(string what, Color? rarity, string value)
        {
            caption.text = what;
            rarityDot.gameObject.SetActive(rarity.HasValue);
            if (rarity.HasValue) rarityDot.color = rarity.Value;
            caption.rectTransform.offsetMin = new Vector2(rarity.HasValue ? TextLeft + 18f : TextLeft, 12f);
            distance.text = value;
        }

        /// <summary>Gold pulsing halo around the Hunt button (a meme is close or a hunt is active).</summary>
        public void SetHuntGlow(bool on) => glow = on;

        // ---------- Per frame ----------

        void Update()
        {
            if (Geo.TryGetFix(out var fix)) PointFrom(fix.Lat, fix.Lng, smooth: true);
            Pulse(Time.unscaledTime);
        }

        /// <summary>Turns the arrow toward the target as seen from this position (the editor preview calls it once).</summary>
        public void PointFrom(double lat, double lng, bool smooth = false)
        {
            var heading = GeoCompass.Heading;
            northRing.localRotation = Quaternion.Euler(0f, 0f, heading);       // N stays on real north
            arrow.gameObject.SetActive(hasTarget);
            if (!hasTarget) return;

            var relative = GeoCompass.Bearing(lat, lng, targetLat, targetLng) - heading;
            // Smoothed so a noisy compass does not make the arrow jitter.
            shownAngle = smooth ? Mathf.LerpAngle(shownAngle, relative, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime)) : relative;
            needle.localRotation = Quaternion.Euler(0f, 0f, -shownAngle);      // UI rotation is counter-clockwise
        }

        public void Pulse(float time)
        {
            huntHalo.gameObject.SetActive(glow);
            if (!glow) return;
            var k = 0.5f + 0.5f * Mathf.Sin(time * 4f);
            huntHalo.color = MemeHunterUiColors.LegendaryGold.WithAlpha(0.35f + 0.4f * k);
            huntHalo.transform.localScale = Vector3.one * (1f + 0.07f * k);
        }

        // ---------- Construction ----------

        void Build(RectTransform panel, Action onHunt)
        {
            var theme = UiTheme.Get();

            // Everything is laid out in the visible part (above the overhang).
            var content = Ui.Rect("Content", panel);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(0f, Overhang);
            content.offsetMax = Vector2.zero;

            // Bottom-sheet handle.
            var handle = Ui.Rounded("Handle", content, new Color(1f, 1f, 1f, 0.22f), 2.5f);
            handle.raycastTarget = false;
            handle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(44f, 5f));

            // Direction tile: rounded square, an N on a ring that turns with the heading, the lime arrow on top.
            var tile = Ui.Rounded("Direction", content, new Color(1f, 1f, 1f, 0.09f), 18f);
            tile.raycastTarget = false;
            tile.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(18f, -4f), new Vector2(82f, 82f));
            northRing = Ui.Rect("North Ring", tile.transform).Stretch(3f);
            var north = Ui.Text("N", northRing, "N", 13f, new Color(1f, 1f, 1f, 0.85f));
            if (theme != null) north.font = theme.Body(TextWeight.Bold);
            north.rectTransform.Place(new Vector2(0.5f, 1f), Vector2.zero, new Vector2(20f, 16f));
            needle = Ui.Rect("Needle", tile.transform).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(82f, 82f));
            arrow = Ui.Rect("Arrow", needle).gameObject.AddComponent<TriangleGraphic>();
            arrow.color = MemeHunterUiColors.Lime;
            arrow.raycastTarget = false;
            arrow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(32f, 40f));

            // Caption with a rarity dot, then the distance in Jaro.
            rarityDot = Ui.Rounded("Rarity", content, Color.white, 6f);
            rarityDot.raycastTarget = false;
            rarityDot.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(TextLeft, 17f), new Vector2(12f, 12f));

            caption = Ui.Text("Caption", content, string.Empty, 14f, MemeHunterUiColors.LightPurple);
            if (theme != null) caption.font = theme.Body(TextWeight.Bold);
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            caption.characterSpacing = 6f;
            Between(caption.rectTransform, TextLeft, 12f, 34f);

            distance = Ui.Text("Distance", content, string.Empty, 44f, MemeHunterUiColors.Lime, display: true);
            distance.alignment = TextAlignmentOptions.MidlineLeft;
            distance.textWrappingMode = TextWrappingModes.NoWrap;
            distance.enableAutoSizing = true;
            distance.fontSizeMin = 22f;
            distance.fontSizeMax = 44f;
            Between(distance.rectTransform, TextLeft, -42f, 12f);

            // Hunt button (camera) on the right, with its halo behind and a small label under it.
            huntHalo = Ui.Rounded("Hunt Halo", content, MemeHunterUiColors.LegendaryGold, 36f);
            huntHalo.raycastTarget = false;
            Centre(huntHalo.rectTransform, new Vector2(-52f, 12f), 72f);
            var hunt = Ui.IconButton("Hunt", content, theme != null ? theme.cameraIcon : null, onHunt);
            Centre((RectTransform)hunt.transform, new Vector2(-52f, 12f), 62f);
            var label = Ui.Text("Hunt Label", content, "HUNT", 12f, Color.white);
            if (theme != null) label.font = theme.Body(TextWeight.Bold);
            label.characterSpacing = 8f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(-52f, -40f);
            label.rectTransform.sizeDelta = new Vector2(90f, 16f);
            huntHalo.gameObject.SetActive(false);
        }

        /// <summary>A text line spanning from `left` to the Hunt button, between two heights around the middle.</summary>
        static void Between(RectTransform rect, float left, float bottom, float top)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-TextRight, top);
        }

        /// <summary>A square of `size` centred at `centre` from the right-middle of the panel.</summary>
        static void Centre(RectTransform rect, Vector2 centre, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centre;
            rect.sizeDelta = new Vector2(size, size);
        }
    }
}
