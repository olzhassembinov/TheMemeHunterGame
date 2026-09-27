using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class NotificationListPresenter : MonoBehaviour
    {
        [SerializeField] NotificationCollectionData collection;
        [SerializeField] NotificationItemView itemPrefab;
        [SerializeField] RectTransform contentRoot;
        [SerializeField] float itemSpacing = 24f;
        [SerializeField] float trailingContentPadding = 64f;

        void Start()
        {
            Populate(collection);
        }

        public void Populate(NotificationCollectionData data)
        {
            collection = data;
            if (collection == null || itemPrefab == null || contentRoot == null)
                return;

            var itemRect = itemPrefab.transform as RectTransform;
            if (itemRect == null)
                return;

            for (var index = contentRoot.childCount - 1; index >= 0; index--)
                Destroy(contentRoot.GetChild(index).gameObject);

            var notifications = collection.Notifications;
            if (notifications == null)
                return;

            var itemHeight = itemRect.sizeDelta.y;
            var totalHeight = notifications.Length == 0 ? 0f : notifications.Length * itemHeight + (notifications.Length - 1) * itemSpacing + trailingContentPadding;
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);

            for (var index = 0; index < notifications.Length; index++)
            {
                var dataItem = notifications[index];
                if (dataItem == null)
                    continue;

                var item = Instantiate(itemPrefab, contentRoot, false);
                item.Bind(dataItem);
                var rect = item.transform as RectTransform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -index * (itemHeight + itemSpacing));
                rect.sizeDelta = new Vector2(458f, itemHeight);
            }
        }
    }
}