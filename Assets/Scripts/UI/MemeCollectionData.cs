using UnityEngine;

namespace MemeHunter.UI
{
    [CreateAssetMenu(menuName = "Meme Hunter/Meme Collection")]
    public sealed class MemeCollectionData : ScriptableObject
    {
        [SerializeField] MemeData[] memes;

        public MemeData[] Memes => memes;
    }
}