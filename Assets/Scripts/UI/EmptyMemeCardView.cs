using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class EmptyMemeCardView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image outline;

        UnityAction assignedAction;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        public void SetAction(UnityAction action)
        {
            if (assignedAction != null)
                button.onClick.RemoveListener(assignedAction);
            assignedAction = action;
            if (action != null)
                button.onClick.AddListener(action);
        }

        public void SetOutlineColor(Color color)
        {
            if (outline != null)
                outline.color = color;
        }
    }
}