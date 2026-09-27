using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class MemeCardView : MonoBehaviour
    {
        [SerializeField] Image cardFrame;
        [SerializeField] Image artwork;
        [SerializeField] TMP_Text titleText;
        [SerializeField] RarityIndicatorView rarityIndicator;

        public void SetArtwork(Sprite sprite)
        {
            if (artwork != null)
            {
                artwork.sprite = sprite;
                artwork.enabled = sprite != null;
            }
        }

        public void Bind(Sprite sprite, string title, string rarity, Color rarityColor)
        {
            SetArtwork(sprite);

            if (titleText != null)
            {
                titleText.text = title;
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }
            if (rarityIndicator != null)
            {
                rarityIndicator.SetRarity(rarity, rarityColor);
                rarityIndicator.gameObject.SetActive(Enum.TryParse(rarity, true, out MemeRarity _));
            }
        }

        public void Bind(MemeData data)
        {
            if (data == null)
                return;

            Bind(data.Artwork, data.DisplayName, string.Empty, MemeHunterUiColors.CommonGrey);
            SetRarity(data.Rarity);
        }

        public void SetRarity(MemeRarity rarity)
        {
            if (rarityIndicator == null)
                return;

            rarityIndicator.SetRarity(rarity);
            rarityIndicator.gameObject.SetActive(true);
        }
    }
}