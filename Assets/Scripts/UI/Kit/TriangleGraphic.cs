using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Kit
{
    /// <summary>A chunky arrow head pointing up (rotate the RectTransform to aim it).</summary>
    public sealed class TriangleGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            vh.AddVert(new Vector2(r.center.x, r.yMax), color, Vector2.zero);       // tip
            vh.AddVert(new Vector2(r.xMax, r.yMin), color, Vector2.zero);
            vh.AddVert(new Vector2(r.center.x, r.yMin + r.height * 0.28f), color, Vector2.zero);   // notch
            vh.AddVert(new Vector2(r.xMin, r.yMin), color, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }
}
