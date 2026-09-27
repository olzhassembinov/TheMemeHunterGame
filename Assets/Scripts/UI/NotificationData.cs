using UnityEngine;

namespace MemeHunter.UI
{
    [CreateAssetMenu(menuName = "Meme Hunter/Notification Data")]
    public sealed class NotificationData : ScriptableObject
    {
        [SerializeField] string title;
        [SerializeField, TextArea(2, 4)] string description;
        [SerializeField] string timestamp;
        [SerializeField] Sprite thumbnail;
        [SerializeField] bool unread;

        public string Title => title;
        public string Description => description;
        public string Timestamp => timestamp;
        public Sprite Thumbnail => thumbnail;
        public bool Unread => unread;
    }
}