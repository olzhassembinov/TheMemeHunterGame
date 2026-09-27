using System;
using UnityEngine;

namespace MemeHunter.UI
{
    [Serializable]
    public sealed class MapLocationData
    {
        [SerializeField] MemeData meme;
        [SerializeField] Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);

        public MemeData Meme => meme;
        public Vector2 NormalizedPosition => normalizedPosition;
    }
}