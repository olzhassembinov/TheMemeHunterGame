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

        /// <summary>When set, the button works as Back (previous screen, or the Map) instead of a fixed destination.</summary>
        public bool GoesBack { get; set; }

        public void Navigate()
        {
            if (navigator == null) return;
            if (GoesBack) navigator.Back();
            else navigator.Show(destinationScreenId);
        }
    }
}