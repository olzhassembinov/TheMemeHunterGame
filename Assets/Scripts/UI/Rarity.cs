using UnityEngine;

namespace MemeHunter.UI
{
    /// <summary>One place for rarity colours and for turning the server's rarity text into MemeRarity.</summary>
    public static class Rarity
    {
        public static Color ColorOf(MemeRarity rarity) => rarity switch
        {
            MemeRarity.Common => MemeHunterUiColors.CommonGrey,
            MemeRarity.Uncommon => MemeHunterUiColors.UncommonGreen,
            MemeRarity.Rare => MemeHunterUiColors.RarePurple,
            MemeRarity.Legendary => MemeHunterUiColors.LegendaryGold,
            _ => MemeHunterUiColors.CommonGrey
        };

        /// <summary>The server sends "common" / "uncommon" / "rare" / "legendary"; unknown values count as Common.</summary>
        public static MemeRarity Parse(string value)
        {
            return System.Enum.TryParse(value?.Trim(), true, out MemeRarity rarity) ? rarity : MemeRarity.Common;
        }

        public static Color ColorOf(string serverRarity) => ColorOf(Parse(serverRarity));
    }
}
