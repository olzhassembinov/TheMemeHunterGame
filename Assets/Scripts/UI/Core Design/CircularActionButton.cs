using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class CircularActionButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] RadialGradientGraphic background;
        [SerializeField] Image icon;

        UnityAction assignedAction;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (background == null)
                background = GetComponent<RadialGradientGraphic>();
            if (button != null && background != null)
                button.targetGraphic = background;
        }

        public void SetAction(UnityAction action)
        {
            if (assignedAction != null)
                button.onClick.RemoveListener(assignedAction);
            assignedAction = action;
            if (action != null)
                button.onClick.AddListener(action);
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon == null)
                return;

            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        public void SetGradientColors(Color center, Color edge)
        {
            if (background != null)
                background.SetColors(center, edge);
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
    }
}