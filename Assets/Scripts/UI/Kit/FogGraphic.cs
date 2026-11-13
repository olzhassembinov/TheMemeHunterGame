using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Kit
{
    /// <summary>
    /// Full-screen haze with a clearer oval in the middle (PDF spec: "fog that clears as you get closer").
    /// Density 0 = invisible, 1 = thick fog; the HUD drives it from the distance to the meme.
    /// </summary>
    public sealed class FogGraphic : MaskableGraphic
    {
        const int Segments = 48;

        [SerializeField, Range(0f, 1f)] float density = 0.5f;
        [SerializeField, Range(0.1f, 1f)] float clearRadius = 0.55f;   // size of the clearer oval (fraction of the screen)

        public float Density
        {
            get => density;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, density)) return;
                density = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (density <= 0.001f) return;

            var rect = GetPixelAdjustedRect();
            var center = rect.center;
            var inner = new Color(color.r, color.g, color.b, color.a * density * 0.55f);   // thinner fog in the middle
            var outer = new Color(color.r, color.g, color.b, color.a * density);

            vh.AddVert(center, inner, Vector2.zero);
            for (var i = 0; i < Segments; i++)
            {
                var angle = i * Mathf.PI * 2f / Segments;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // Oval ring (clearer zone border) ...
                var ring = center + new Vector2(dir.x * rect.width, dir.y * rect.height) * (0.5f * clearRadius);
                vh.AddVert(ring, inner, Vector2.zero);

                // ... and the same direction projected onto the screen edge (thick fog).
                var scale = Mathf.Min(
                    Mathf.Abs(dir.x) > 1e-4f ? rect.width * 0.5f / Mathf.Abs(dir.x) : float.MaxValue,
                    Mathf.Abs(dir.y) > 1e-4f ? rect.height * 0.5f / Mathf.Abs(dir.y) : float.MaxValue);
                vh.AddVert(center + dir * scale, outer, Vector2.zero);
            }

            for (var i = 0; i < Segments; i++)
            {
                var ring = 1 + i * 2;
                var edge = ring + 1;
                var nextRing = 1 + (i + 1) % Segments * 2;
                var nextEdge = nextRing + 1;
                vh.AddTriangle(0, ring, nextRing);
                vh.AddTriangle(ring, edge, nextEdge);
                vh.AddTriangle(ring, nextEdge, nextRing);
            }
        }
    }
}
