using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class XpProgressView : MonoBehaviour
    {
        [SerializeField] RectTransform track;
        [SerializeField] RectTransform progress;
        [SerializeField] TMP_Text valueText;
        [SerializeField] Image trackImage;
        [SerializeField] Image progressImage;
        [SerializeField] HorizontalGradientEffect gradient;

        public void SetColors(Color trackColor, Color fillStart, Color fillEnd)
        {
            if (trackImage != null)
                trackImage.color = trackColor;
            if (progressImage != null)
                progressImage.color = Color.white;
            if (gradient != null)
                gradient.SetColors(fillStart, fillEnd);
        }

        public void Bind(int currentXp, int xpForNextLevel)
        {
            var required = Mathf.Max(1, xpForNextLevel);
            var current = Mathf.Max(0, currentXp);
            var ratio = Mathf.Clamp01((float)current / required);

            if (track != null && progress != null)
                progress.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, track.rect.width * ratio);
            if (valueText != null)
                valueText.text = current + "/" + required;
        }
    }
}