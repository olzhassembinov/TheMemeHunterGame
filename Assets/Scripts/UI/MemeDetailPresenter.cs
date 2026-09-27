using TMPro;
using UnityEngine;

namespace MemeHunter.UI
{
    public sealed class MemeDetailPresenter : MonoBehaviour
    {
        [SerializeField] MemeData memeData;
        [SerializeField] TMP_Text titleText;
        [SerializeField] MemeCardView memeCard;
        [SerializeField] TMP_Text rarityValueText;
        [SerializeField] TMP_Text priceValueText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] ScreenNavigator navigator;

        public void Bind(MemeData data)
        {
            if (data == null)
                return;

            memeData = data;
            if (titleText != null)
                titleText.text = data.DisplayName;
            if (memeCard != null)
                memeCard.Bind(data);
            if (rarityValueText != null)
            {
                rarityValueText.text = data.Rarity.ToString().ToUpperInvariant();
                rarityValueText.color = RarityColor(data.Rarity);
            }
            if (priceValueText != null)
            {
                priceValueText.text = data.Price + "$";
                priceValueText.color = MemeHunterUiColors.BrightGreen;
            }
            if (descriptionText != null)
                descriptionText.text = data.Description;
        }

        public void GoBack()
        {
            if (navigator != null)
                navigator.Show("Profile");
        }

        static Color RarityColor(MemeRarity rarity)
        {
            return rarity switch
            {
                MemeRarity.Common => MemeHunterUiColors.CommonGrey,
                MemeRarity.Uncommon => MemeHunterUiColors.UncommonGreen,
                MemeRarity.Rare => MemeHunterUiColors.RarePurple,
                MemeRarity.Legendary => MemeHunterUiColors.BrightGreen,
                _ => MemeHunterUiColors.DarkNavy
            };
        }
    }
}