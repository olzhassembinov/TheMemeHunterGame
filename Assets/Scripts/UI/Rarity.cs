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

        /// <summary>
        /// The server sends "common" / "rare" / "epic" / "legendary". The game's Uncommon tier is the server's Epic
        /// ("epic" maps to it), so the saved memes keep their numbers. Unknown values count as Common.
        /// </summary>
        public static MemeRarity Parse(string value)
        {
            var text = value?.Trim();
            if (string.Equals(text, "epic", System.StringComparison.OrdinalIgnoreCase)) return MemeRarity.Uncommon;
            return System.Enum.TryParse(text, true, out MemeRarity rarity) ? rarity : MemeRarity.Common;
        }

        /// <summary>The name the player sees: the Uncommon tier is called Epic.</summary>
        public static string Name(MemeRarity rarity) => rarity == MemeRarity.Uncommon ? "Epic" : rarity.ToString();

        /// <summary>"a rare meme" / "an epic meme" (capital first letter when the sentence starts with it).</summary>
        public static string MemeWithArticle(string serverRarity, bool capital = false)
        {
            var word = string.IsNullOrWhiteSpace(serverRarity) ? string.Empty : serverRarity.Trim().ToLowerInvariant();
            var text = word.Length == 0 ? "a meme" : ("aeiou".IndexOf(word[0]) >= 0 ? "an " : "a ") + word + " meme";
            return capital ? char.ToUpperInvariant(text[0]) + text.Substring(1) : text;
        }

        public static Color ColorOf(string serverRarity) => ColorOf(Parse(serverRarity));
    }
}
