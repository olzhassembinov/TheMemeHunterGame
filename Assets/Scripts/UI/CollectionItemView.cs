using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class CollectionItemView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image artwork;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        public void Bind(Sprite sprite, string title, int itemCount)
        {
            if (artwork != null)
            {
                artwork.sprite = sprite;
                artwork.enabled = sprite != null;
            }

            if (titleText != null)
                titleText.text = title;
            if (countText != null)
                countText.text = itemCount.ToString();
        }

        public void SetAction(UnityAction action)
        {
            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(action);
        }
    }
}