using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class NotificationItemView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] Graphic unreadMarker;
        [SerializeField] Image thumbnail;

        public void Bind(NotificationData data)
        {
            if (data == null)
                return;

            Bind(data.Title, data.Description, data.Timestamp, data.Unread);
            if (thumbnail != null)
            {
                thumbnail.sprite = data.Thumbnail;
                thumbnail.enabled = data.Thumbnail != null;
            }
        }

        public void Bind(string title, string message, string time, bool isUnread)
        {
            if (titleText != null)
                titleText.text = title;
            if (messageText != null)
                messageText.text = message;
            if (timeText != null)
                timeText.text = time;
            if (unreadMarker != null)
                unreadMarker.enabled = isUnread;
        }
    }
}