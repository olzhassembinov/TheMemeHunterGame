using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class RadialGradientGraphic : MaskableGraphic
    {
        [SerializeField] Color centerColor = MemeHunterUiColors.Teal;
        [SerializeField] Color edgeColor = MemeHunterUiColors.LightPurple;
        [SerializeField, Range(12, 96)] int segments = 48;

        public void SetColors(Color center, Color edge)
        {
            centerColor = center;
            edgeColor = edge;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            vertexHelper.AddVert(center, centerColor * color, Vector2.one * 0.5f);

            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertexHelper.AddVert(position, edgeColor * color, Vector2.zero);
            }

            for (var index = 0; index < segments; index++)
                vertexHelper.AddTriangle(0, index + 1, (index + 1) % segments + 1);
        }
    }
}