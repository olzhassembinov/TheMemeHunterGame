using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class ScreenNavigationButton : MonoBehaviour
    {
        [SerializeField] CircularActionButton circularButton;
        [SerializeField] Button button;
        [SerializeField] ScreenNavigator navigator;
        [SerializeField] string destinationScreenId;

        void Awake()
        {
            if (circularButton != null)
                circularButton.SetAction(Navigate);
            else if (button != null)
                button.onClick.AddListener(Navigate);
        }

        public void Navigate()
        {
            if (navigator != null)
                navigator.Show(destinationScreenId);
        }
    }
}