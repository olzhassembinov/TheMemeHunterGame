using System.Linq;
using MemeHunter.UI;
using UnityEngine;

namespace MemeHunter.Game
{
    /// <summary>
    /// All meme cards the game knows (Resources/Memes/*.asset, MemeData). The server only sends a rarity per card,
    /// so the client decides which meme of that rarity the card is (stable per card id).
    /// </summary>
    public static class MemeCatalog
    {
        static MemeData[] all;

        public static MemeData[] All => all ??= Resources.LoadAll<MemeData>("Memes")
            .Where(m => m != null && !string.IsNullOrEmpty(m.MemeId))
            .OrderBy(m => m.Rarity).ThenBy(m => m.DisplayName)
            .ToArray();

        public static MemeData Find(string memeId) => All.FirstOrDefault(m => m.MemeId == memeId);

        /// <summary>The meme behind a server card: same card id -> always the same meme of that rarity.</summary>
        public static MemeData ForCard(string cardId, string serverRarity)
        {
            var rarity = Rarity.Parse(serverRarity);
            var pool = All.Where(m => m.Rarity == rarity).ToArray();
            if (pool.Length == 0) pool = All;
            if (pool.Length == 0) return null;
            var hash = 17;
            foreach (var c in cardId ?? string.Empty) hash = hash * 31 + c;
            return pool[(hash & 0x7fffffff) % pool.Length];
        }
    }
}
