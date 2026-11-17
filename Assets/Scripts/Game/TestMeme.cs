using MemeHunter.Net;
using UnityEngine;

namespace MemeHunter.Game
{
    /// <summary>
    /// DEV ONLY (ApiConfig.ShowDevTools): a meme that exists only on this phone, for trying the hunt without the
    /// backend. It is placed about 80 m north of the player and behaves like a claimed card (map pin, compass, fog,
    /// AR meme, Catch, card reveal), but Catch takes no photo and sends nothing: no server, no doskaz.kz.
    /// The backend has no "create card" endpoint, so real memes can only be added on the server.
    /// </summary>
    public static class TestMeme
    {
        public const string CardPrefix = "test-meme-";
        const double MetersPerDegreeLat = 111320.0;

        public static bool IsTest(string cardId) => cardId != null && cardId.StartsWith(CardPrefix);

        /// <summary>Coins shown on the reveal (the real reward comes from the server, so this one is local only).</summary>
        public static int Reward(string rarity)
        {
            switch (UI.Rarity.Parse(rarity))
            {
                case UI.MemeRarity.Legendary: return 100;
                case UI.MemeRarity.Rare: return 50;
                case UI.MemeRarity.Uncommon: return 25;
                default: return 10;
            }
        }

        /// <summary>A test card `metersNorth` from the given position. Saved as the active hunt, like a real claim.</summary>
        public static ClaimDto SpawnNear(double lat, double lng, string rarity = "legendary", double metersNorth = 80.0)
        {
            var claim = new ClaimDto
            {
                CardId = CardPrefix + System.DateTime.UtcNow.Ticks,
                Lat = lat + metersNorth / MetersPerDegreeLat,
                Lng = lng,
                Rarity = rarity
            };
            Session.SaveHunt(claim);
            Debug.Log($"[TestMeme] {rarity} test meme placed {metersNorth:F0} m north ({claim.Lat:F6}, {claim.Lng:F6})");
            return claim;
        }
    }
}
