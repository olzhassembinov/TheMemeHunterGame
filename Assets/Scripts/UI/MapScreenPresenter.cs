using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class MapScreenPresenter : MonoBehaviour
    {
        [SerializeField] MapLocationCollectionData locationData;
        [SerializeField] MemeLocationDot markerPrefab;
        [SerializeField] RectTransform markerArea;

        void Start()
        {
            Populate(locationData);
        }

        public void Populate(MapLocationCollectionData data)
        {
            locationData = data;
            if (markerArea == null || markerPrefab == null || locationData == null || locationData.Locations == null)
                return;

            for (var index = markerArea.childCount - 1; index >= 0; index--)
                Destroy(markerArea.GetChild(index).gameObject);

            foreach (var location in locationData.Locations)
            {
                if (location == null || location.Meme == null)
                    continue;

                var marker = Instantiate(markerPrefab, markerArea, false);
                marker.Bind(location.Meme);
                var rect = marker.transform as RectTransform;
                var normalized = location.NormalizedPosition;
                rect.anchorMin = normalized;
                rect.anchorMax = normalized;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(27f, 27f);
            }
        }
    }
}