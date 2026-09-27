using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class RoundedRectGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] float cornerRadius = 12f;
        [SerializeField, Range(2, 16)] int cornerSegments = 6;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            var corners = new[]
            {
                (new Vector2(rect.xMax - radius, rect.yMin + radius), -90f),
                (new Vector2(rect.xMax - radius, rect.yMax - radius), 0f),
                (new Vector2(rect.xMin + radius, rect.yMax - radius), 90f),
                (new Vector2(rect.xMin + radius, rect.yMin + radius), 180f)
            };

            vertexHelper.AddVert(rect.center, color, Vector2.one * 0.5f);
            var vertexIndex = 1;
            foreach (var corner in corners)
            {
                for (var segment = 0; segment <= cornerSegments; segment++)
                {
                    var angle = (corner.Item2 + segment * 90f / cornerSegments) * Mathf.Deg2Rad;
                    var position = corner.Item1 + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    vertexHelper.AddVert(position, color, Vector2.zero);
                    vertexIndex++;
                }
            }

            var perimeterCount = vertexIndex - 1;
            for (var index = 0; index < perimeterCount; index++)
                vertexHelper.AddTriangle(0, index + 1, (index + 1) % perimeterCount + 1);
        }
    }
}