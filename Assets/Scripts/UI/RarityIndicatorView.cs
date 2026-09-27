using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public enum MemeRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    public sealed class RarityIndicatorView : MonoBehaviour
    {
        [SerializeField] Image marker;
        [SerializeField] TMP_Text label;
        [SerializeField] Sprite commonSprite;
        [SerializeField] Sprite uncommonSprite;
        [SerializeField] Sprite rareSprite;
        [SerializeField] Sprite legendarySprite;

        public void SetRarity(MemeRarity rarity)
        {
            if (marker == null)
                return;

            marker.sprite = rarity switch
            {
                MemeRarity.Common => commonSprite,
                MemeRarity.Uncommon => uncommonSprite,
                MemeRarity.Rare => rareSprite,
                MemeRarity.Legendary => legendarySprite,
                _ => commonSprite
            };
            marker.color = Color.white;
            if (label != null)
                label.text = rarity.ToString();
        }

        public void SetRarity(string rarityName, Color color)
        {
            if (marker != null)
                marker.color = color;
            if (label != null)
                label.text = rarityName;
        }
    }
}