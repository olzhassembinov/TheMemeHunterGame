using TMPro;
using UnityEngine;

namespace MemeHunter.UI
{
    public sealed class FormInputView : MonoBehaviour
    {
        [SerializeField] TMP_InputField inputField;
        [SerializeField] TMP_Text validationText;

        public string Value
        {
            get => inputField != null ? inputField.text : string.Empty;
            set
            {
                if (inputField != null)
                    inputField.text = value;
            }
        }

        public void SetPlaceholder(string placeholder)
        {
            if (inputField != null && inputField.placeholder is TMP_Text placeholderText)
                placeholderText.text = placeholder;
        }

        public void SetFieldSize(float width, float height)
        {
            if (inputField != null && inputField.transform is RectTransform rectTransform)
            {
                rectTransform.sizeDelta = new Vector2(width, height);

                if (transform is RectTransform rootRect)
                {
                    rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                    rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + 26f);
                }
            }
        }

        public void SetValidation(string message, bool isValid)
        {
            if (validationText == null)
                return;

            validationText.text = message;
            validationText.color = isValid ? MemeHunterUiColors.DarkNavy : MemeHunterUiColors.RarePurple;
            validationText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}