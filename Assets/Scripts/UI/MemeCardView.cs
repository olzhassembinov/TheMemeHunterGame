using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public class MemeCardView : MonoBehaviour
    {
        [SerializeField] protected Image cardFrame;
        [SerializeField] protected Image artwork;
        [SerializeField] protected TMP_Text titleText;
        [SerializeField] protected RarityIndicatorView rarityIndicator;

        public void SetArtwork(Sprite sprite)
        {
            if (artwork != null)
            {
                artwork.sprite = sprite;
                artwork.enabled = sprite != null;
            }
        }

        public void Bind(Sprite sprite = null, string title = null, string rarity = null, Color rarityColor = new Color())
        {
            if(sprite == null && title == null && rarity == null && rarityColor == null) Debug.LogError("Bind got an empty sprite. HOW? Idk.");

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