using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MemeDetailOpenButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] MemeData memeData;
        [SerializeField] MemeDetailPresenter detailPresenter;
        [SerializeField] ScreenNavigator navigator;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            button.onClick.AddListener(OpenDetail);
        }

        public void OpenDetail()
        {
            if (detailPresenter != null)
                detailPresenter.Bind(memeData);
            if (navigator != null)
                navigator.Show("MemeCard");
        }
    }
}