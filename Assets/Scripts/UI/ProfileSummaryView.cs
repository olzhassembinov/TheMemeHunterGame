using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class ProfileSummaryView : MonoBehaviour
    {
        [SerializeField] Image avatar;
        [SerializeField] TMP_Text playerNameText;
        [SerializeField] TMP_Text collectionCountText;

        public void Bind(Sprite avatarSprite, string playerName, int collectionCount)
        {
            if (avatar != null)
            {
                avatar.sprite = avatarSprite;
                avatar.enabled = avatarSprite != null;
            }

            if (playerNameText != null)
                playerNameText.text = playerName;
            if (collectionCountText != null)
                collectionCountText.text = collectionCount.ToString();
        }
    }
}