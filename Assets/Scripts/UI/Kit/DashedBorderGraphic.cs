using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Kit
{
    /// <summary>Dashed rounded-rect outline (Figma empty card slot: border 5px dashed, radius 20).</summary>
    public sealed class DashedBorderGraphic : MaskableGraphic
    {
        public float CornerRadius = 20f;
        public float Thickness = 5f;
        public float Dash = 20f;
        public float Gap = 15f;

        static readonly List<Vector2> path = new List<Vector2>();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var inset = Thickness * 0.5f;                       // the stroke is centred inside the rect, like CSS borders
            r = new Rect(r.xMin + inset, r.yMin + inset, r.width - Thickness, r.height - Thickness);
            var radius = Mathf.Min(CornerRadius - inset, r.width / 2f, r.height / 2f);

            // Closed path around the rounded rect.
            path.Clear();
            const int seg = 8;
            void Corner(Vector2 c, float start)
            {
                for (var i = 0; i <= seg; i++)
                {
                    var a = (start + 90f * i / seg) * Mathf.Deg2Rad;
                    path.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            }
            Corner(new Vector2(r.xMax - radius, r.yMax - radius), 0f);
            Corner(new Vector2(r.xMin + radius, r.yMax - radius), 90f);
            Corner(new Vector2(r.xMin + radius, r.yMin + radius), 180f);
            Corner(new Vector2(r.xMax - radius, r.yMin + radius), 270f);
            path.Add(path[0]);

            // Walk along the path, emitting short quads only while "pen down".
            var period = Dash + Gap;
            var travelled = 0f;
            for (var i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                var len = Vector2.Distance(a, b);
                var t = 0f;
                while (t < len)
                {
                    var phase = (travelled + t) % period;
                    var step = phase < Dash ? Mathf.Min(Dash - phase, len - t) : Mathf.Min(period - phase, len - t);
                    if (phase < Dash)
                        Quad(vh, Vector2.Lerp(a, b, t / len), Vector2.Lerp(a, b, (t + step) / len));
                    t += step;
                }
                travelled += len;
            }
        }

        void Quad(VertexHelper vh, Vector2 p0, Vector2 p1)
        {
            var dir = (p1 - p0);
            if (dir.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-dir.y, dir.x).normalized * (Thickness * 0.5f);
            var i = vh.currentVertCount;
            vh.AddVert(p0 - n, color, Vector2.zero);
            vh.AddVert(p0 + n, color, Vector2.zero);
            vh.AddVert(p1 + n, color, Vector2.zero);
            vh.AddVert(p1 - n, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
