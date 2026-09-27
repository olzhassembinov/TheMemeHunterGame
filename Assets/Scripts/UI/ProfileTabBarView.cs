using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class ProfileTabBarView : MonoBehaviour
    {
        [SerializeField] Button[] tabs = Array.Empty<Button>();
        [SerializeField] Graphic[] underlines = Array.Empty<Graphic>();
        [SerializeField] string[] tabIds = { "I AM", "MY FRIENDS", "GLOBAL" };
        [SerializeField] Color activeColor = MemeHunterUiColors.DarkNavy;
        [SerializeField] Color inactiveColor = MemeHunterUiColors.DarkNavy;

        public UnityEvent<string> SelectionChanged = new UnityEvent<string>();

        int selectedIndex;

        void Awake()
        {
            for (var index = 0; index < tabs.Length; index++)
            {
                var capturedIndex = index;
                if (tabs[index] != null)
                    tabs[index].onClick.AddListener(() => Select(capturedIndex));
            }
            ApplySelection();
        }

        public void Select(string tabId)
        {
            var index = Array.IndexOf(tabIds, tabId);
            if (index >= 0)
                Select(index);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= tabs.Length || index >= tabIds.Length)
                return;

            selectedIndex = index;
            ApplySelection();
            SelectionChanged.Invoke(tabIds[index]);
        }

        void ApplySelection()
        {
            for (var index = 0; index < tabs.Length; index++)
            {
                var text = tabs[index] != null ? tabs[index].GetComponentInChildren<TMPro.TMP_Text>() : null;
                if (text != null)
                    text.color = index == selectedIndex ? activeColor : inactiveColor;
                if (index < underlines.Length && underlines[index] != null)
                    underlines[index].enabled = index == selectedIndex;
            }
        }
    }
}