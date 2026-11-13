using MemeHunter.Game;
using TMPro;
using UnityEngine;

namespace MemeHunter.UI.Kit
{
    /// <summary>
    /// Navigation compass (design fix "стрелка ... цифро блок с дистанцией"): a navy disc with a lime arrow that points
    /// toward the meme relative to where the phone faces, a small N mark, and the distance underneath.
    /// Call SetTarget(lat, lng) or ClearTarget(); it updates itself every frame.
    /// </summary>
    public sealed class CompassWidget : MonoBehaviour
    {
        const float Size = 86f;

        RectTransform arrow;
        RectTransform northMark;
        TextMeshProUGUI distance;
        bool hasTarget;
        double targetLat, targetLng;
        float shownAngle;

        public static CompassWidget Create(Transform parent)
        {
            var root = Ui.Rect("Compass", parent);
            root.sizeDelta = new Vector2(150f, Size + 40f);
            var widget = root.gameObject.AddComponent<CompassWidget>();
            widget.Build(root);
            root.gameObject.SetActive(false);
            return widget;
        }

        public void SetTarget(double lat, double lng)
        {
            hasTarget = true;
            targetLat = lat;
            targetLng = lng;
            gameObject.SetActive(true);
        }

        public void ClearTarget()
        {
            hasTarget = false;
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!hasTarget || !Geo.TryGetFix(out var fix)) return;

            var heading = GeoCompass.Heading;
            var bearing = GeoCompass.Bearing(fix.Lat, fix.Lng, targetLat, targetLng);
            // Smooth the needle so a noisy compass does not make it jitter.
            shownAngle = Mathf.LerpAngle(shownAngle, bearing - heading, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            arrow.localRotation = Quaternion.Euler(0f, 0f, -shownAngle);           // UI rotation is counter-clockwise
            northMark.parent.localRotation = Quaternion.Euler(0f, 0f, heading);   // N stays on real north

            var m = Geo.DistanceMeters(fix.Lat, fix.Lng, targetLat, targetLng);
            distance.text = m >= 1000 ? (m / 1000.0).ToString("0.0") + " km" : Mathf.RoundToInt((float)m) + " m";
        }

        void Build(RectTransform root)
        {
            var navy = new Color(0.15f, 0.18f, 0.36f, 0.85f);
            var disc = Ui.Rounded("Disc", root, Color.white, Size / 2f);
            disc.raycastTarget = false;
            disc.rectTransform.Place(new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Size, Size));
            var face = Ui.Rounded("Face", disc.transform, navy, Size / 2f - 3f);
            face.raycastTarget = false;
            face.rectTransform.Stretch(3f);

            // N mark on a ring that turns against the heading.
            var ring = Ui.Rect("North Ring", face.transform).Stretch();
            var n = Ui.Text("N", ring, "N", 14f, Color.white, display: true);
            northMark = n.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(20f, 16f));

            var needle = Ui.Rect("Needle", face.transform).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Size, Size));
            var tri = Ui.Rect("Arrow", needle).gameObject.AddComponent<TriangleGraphic>();
            tri.color = MemeHunterUiColors.Lime;
            tri.raycastTarget = false;
            tri.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(34f, 42f));
            arrow = needle;

            var pill = Ui.Rounded("Distance", root, navy, 16f);
            pill.raycastTarget = false;
            pill.rectTransform.Place(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(120f, 32f));
            distance = Ui.Text("Value", pill.transform, string.Empty, 22f, MemeHunterUiColors.Lime, display: true);
            distance.rectTransform.Stretch();
            distance.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
