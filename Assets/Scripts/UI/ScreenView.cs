using UnityEngine;

namespace MemeHunter.UI
{
    public sealed class ScreenView : MonoBehaviour
    {
        [SerializeField] string screenId;

        public string ScreenId => screenId;

        public void Configure(string id)
        {
            screenId = id;
        }

        internal void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }
    }
}