using UnityEngine;

namespace MemeHunter.UI
{
    [CreateAssetMenu(menuName = "Meme Hunter/Map Location Collection")]
    public sealed class MapLocationCollectionData : ScriptableObject
    {
        [SerializeField] MapLocationData[] locations;

        public MapLocationData[] Locations => locations;
    }
}