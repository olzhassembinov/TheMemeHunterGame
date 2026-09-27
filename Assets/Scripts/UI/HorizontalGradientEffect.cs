using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [DisallowMultipleComponent]
    public sealed class HorizontalGradientEffect : BaseMeshEffect
    {
        [SerializeField] Color leftColor = MemeHunterUiColors.Teal;
        [SerializeField] Color rightColor = MemeHunterUiColors.BrightGreen;

        static readonly List<UIVertex> Vertices = new List<UIVertex>();

        public void SetColors(Color left, Color right)
        {
            leftColor = left;
            rightColor = right;
            graphic?.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper.currentVertCount == 0)
                return;

            Vertices.Clear();
            vertexHelper.GetUIVertexStream(Vertices);
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            foreach (var vertex in Vertices)
            {
                minX = Mathf.Min(minX, vertex.position.x);
                maxX = Mathf.Max(maxX, vertex.position.x);
            }

            var width = Mathf.Max(maxX - minX, Mathf.Epsilon);
            for (var index = 0; index < Vertices.Count; index++)
            {
                var vertex = Vertices[index];
                var gradientColor = Color.Lerp(leftColor, rightColor, Mathf.Clamp01((vertex.position.x - minX) / width));
                vertex.color = new Color32(
                    (byte)(vertex.color.r * gradientColor.r),
                    (byte)(vertex.color.g * gradientColor.g),
                    (byte)(vertex.color.b * gradientColor.b),
                    (byte)(vertex.color.a * gradientColor.a));
                Vertices[index] = vertex;
            }

            vertexHelper.Clear();
            vertexHelper.AddUIVertexTriangleStream(Vertices);
        }
    }
}