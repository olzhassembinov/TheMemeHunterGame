using UnityEngine;

namespace MemeHunter.UI
{
    [CreateAssetMenu(menuName = "Meme Hunter/Meme Data")]
    public sealed class MemeData : ScriptableObject
    {
        [SerializeField] string memeId;
        [SerializeField] string displayName;
        [SerializeField] MemeRarity rarity;
        [SerializeField, Min(0)] int price;
        [SerializeField, TextArea(3, 10)] string description;
        [SerializeField] Sprite artwork;

        public string MemeId => memeId;
        public string DisplayName => displayName;
        public MemeRarity Rarity => rarity;
        public int Price => price;
        public string Description => description;
        public Sprite Artwork => artwork;
    }
}