using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public enum ARGameUIState
    {
        Freeze,
        GetCloser,
        Catch
    }

    [DisallowMultipleComponent]
    public sealed class ARGameUIController : MonoBehaviour
    {
        [Serializable]
        public sealed class StateChangedEvent : UnityEvent<ARGameUIState> { }

        [SerializeField] ARGameUIState initialState = ARGameUIState.Freeze;
        [SerializeField] GameObject freezeSidebar;
        [SerializeField] Image[] sidebarThumbnails = Array.Empty<Image>();
        [SerializeField] Image stateThumbnail;
        [SerializeField] TMP_Text primaryStatusText;
        [SerializeField] TMP_Text secondaryStatusText;
        [SerializeField] Button actionButton;
        [SerializeField] TMP_Text actionButtonLabel;
        [SerializeField] Graphic actionButtonBackground;

        public StateChangedEvent StateChanged = new StateChangedEvent();
        public UnityEvent CatchRequested = new UnityEvent();

        public ARGameUIState CurrentState { get; private set; }

        static readonly Color StatusGreen = new Color32(209, 255, 159, 255);
        static readonly Color DisabledLabel = new Color32(128, 128, 128, 255);

        void Awake()
        {
            if (actionButton != null)
            {
                actionButton.transition = Selectable.Transition.None;
                actionButton.onClick.AddListener(HandleActionPressed);
            }

            SetState(initialState, false);
        }

        public void SetState(ARGameUIState state)
        {
            SetState(state, true);
        }

        public void SetSidebarThumbnails(IReadOnlyList<Sprite> sprites)
        {
            for (var index = 0; index < sidebarThumbnails.Length; index++)
            {
                var sprite = sprites != null && index < sprites.Count ? sprites[index] : null;
                SetThumbnail(sidebarThumbnails[index], sprite);
            }
        }

        public void SetStateThumbnail(Sprite sprite)
        {
            SetThumbnail(stateThumbnail, sprite);
        }

        void SetState(ARGameUIState state, bool notify)
        {
            CurrentState = state;
            var isFreeze = state == ARGameUIState.Freeze;
            var isGetCloser = state == ARGameUIState.GetCloser;
            var canCatch = state == ARGameUIState.Catch;

            if (freezeSidebar != null)
                freezeSidebar.SetActive(isFreeze);
            if (stateThumbnail != null)
                stateThumbnail.gameObject.SetActive(!isFreeze);

            SetStatus(primaryStatusText, isGetCloser ? "FREEZE" : canCatch ? "Catch!" : string.Empty, isGetCloser || canCatch);
            SetStatus(secondaryStatusText, isGetCloser ? "Get closer" : string.Empty, isGetCloser);

            if (actionButton != null)
                actionButton.interactable = canCatch;
            if (actionButtonLabel != null)
            {
                actionButtonLabel.text = canCatch ? "Catch" : "Not yet...";
                actionButtonLabel.color = canCatch ? Color.white : DisabledLabel;
            }

            if (actionButtonBackground != null)
                actionButtonBackground.color = Color.white;

            if (notify)
                StateChanged.Invoke(state);
        }

        void HandleActionPressed()
        {
            if (CurrentState == ARGameUIState.Catch)
                CatchRequested.Invoke();
        }

        static void SetThumbnail(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        static void SetStatus(TMP_Text text, string value, bool visible)
        {
            if (text == null)
                return;

            text.text = value;
            text.color = StatusGreen;
            text.gameObject.SetActive(visible);
        }
    }
}