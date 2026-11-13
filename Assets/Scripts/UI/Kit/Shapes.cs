using UnityEngine;

namespace MemeHunter.UI.Kit
{
    /// <summary>Simple shapes drawn from rounded rects (no extra sprites needed).</summary>
    public static class Shapes
    {
        /// <summary>A heart (Figma Donate screens): two circles on a square turned 45 degrees.</summary>
        public static RectTransform Heart(string name, Transform parent, Color color, float size)
        {
            var root = Ui.Rect(name, parent);
            root.sizeDelta = new Vector2(size, size);
            var lobe = size * 0.56f;

            var square = Ui.Rounded("Point", root, color, lobe * 0.12f);
            square.raycastTarget = false;
            square.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -size * 0.06f), new Vector2(lobe, lobe));
            square.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            foreach (var side in new[] { -1f, 1f })
            {
                var circle = Ui.Rounded("Lobe", root, color, lobe * 0.5f);
                circle.raycastTarget = false;
                circle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(side * lobe * 0.36f, size * 0.14f), new Vector2(lobe, lobe));
            }
            return root;
        }
    }
}
