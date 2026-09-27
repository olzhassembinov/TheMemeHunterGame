using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace MemeHunter.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class GradientButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Graphic background;
        [SerializeField] TMP_Text label;
        [SerializeField] Color enabledTint = Color.white;
        [SerializeField] Color disabledTint = MemeHunterUiColors.CommonGrey;
        [SerializeField] Color labelColor = MemeHunterUiColors.DarkNavy;

        UnityAction assignedAction;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (background == null)
                background = button.targetGraphic;
            ApplyInteractableVisuals();
        }

        public void SetAction(UnityAction action)
        {
            if (assignedAction != null)
                button.onClick.RemoveListener(assignedAction);
            assignedAction = action;
            if (action != null)
                button.onClick.AddListener(action);
        }

        public void SetLabel(string value)
        {
            if (label != null)
                label.text = value;
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
            ApplyInteractableVisuals();
        }

        public void SetEnabledColors(Color backgroundColor, Color textColor)
        {
            enabledTint = backgroundColor;
            labelColor = textColor;
            ApplyInteractableVisuals();
        }

        void ApplyInteractableVisuals()
        {
            if (background != null)
                background.color = button != null && button.interactable ? enabledTint : disabledTint;
            if (label != null)
                label.color = button != null && button.interactable ? labelColor : MemeHunterUiColors.DarkNavy;
        }
    }
}