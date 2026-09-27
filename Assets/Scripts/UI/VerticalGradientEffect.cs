using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [DisallowMultipleComponent]
    public sealed class VerticalGradientEffect : BaseMeshEffect
    {
        [SerializeField] Color topColor = Color.white;
        [SerializeField] Color bottomColor = Color.gray;

        static readonly List<UIVertex> Vertices = new List<UIVertex>();

        public void SetColors(Color top, Color bottom)
        {
            topColor = top;
            bottomColor = bottom;
            graphic?.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper.currentVertCount == 0)
                return;

            Vertices.Clear();
            vertexHelper.GetUIVertexStream(Vertices);

            var minY = float.MaxValue;
            var maxY = float.MinValue;
            foreach (var vertex in Vertices)
            {
                minY = Mathf.Min(minY, vertex.position.y);
                maxY = Mathf.Max(maxY, vertex.position.y);
            }

            var height = Mathf.Max(maxY - minY, Mathf.Epsilon);
            for (var index = 0; index < Vertices.Count; index++)
            {
                var vertex = Vertices[index];
                var amount = Mathf.Clamp01((vertex.position.y - minY) / height);
                var gradientColor = Color.Lerp(bottomColor, topColor, amount);
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