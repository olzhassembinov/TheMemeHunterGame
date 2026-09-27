using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class ProfilePhotoView : MonoBehaviour
    {
        [SerializeField] Image photoImage;
        [SerializeField] Sprite placeholderSprite;

        public void SetPhoto(Sprite photo)
        {
            if (photoImage == null)
                return;

            photoImage.sprite = photo != null ? photo : placeholderSprite;
            photoImage.enabled = photoImage.sprite != null;
        }
    }
}