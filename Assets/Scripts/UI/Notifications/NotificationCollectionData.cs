using UnityEngine;

namespace MemeHunter.UI
{
    [CreateAssetMenu(menuName = "Meme Hunter/Notification Collection")]
    public sealed class NotificationCollectionData : ScriptableObject
    {
        [SerializeField] NotificationData[] notifications;

        public NotificationData[] Notifications => notifications;
    }
}