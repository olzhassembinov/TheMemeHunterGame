using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class PageHeaderView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] Image leadingLine;
        [SerializeField] Image trailingLine;

        public void SetTitle(string title)
        {
            if (titleText != null)
                titleText.text = title;
        }

        public void SetLineVisibility(bool visible)
        {
            if (leadingLine != null)
                leadingLine.enabled = visible;
            if (trailingLine != null)
                trailingLine.enabled = visible;
        }
    }
}