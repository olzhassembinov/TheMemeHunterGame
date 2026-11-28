using System;
using System.Collections.Generic;
using MemeHunter.Net;
using Newtonsoft.Json;
using UnityEngine;

namespace MemeHunter.Game
{
    /// <summary>
    /// The player's catches as the server knows them (GET /players/{id}/collection), kept on the phone between
    /// launches so the screens have something to show offline. Each server card is shown as a meme from the
    /// catalog chosen by its card id (MemeCatalog.ForCard), so the same card always looks the same.
    /// PlayerProgress stays for the DEV test meme and the editor previews.
    /// </summary>
    public static class ServerProgress
    {
        const string Key = "ServerCollection";

        static CollectionDto collection;
        static bool loaded;
        static Dictionary<string, int> countByMeme;

        /// <summary>Fires after a fresh collection arrived from the server.</summary>
        public static event Action Changed;

        /// <summary>The last known collection, or null if it was never downloaded on this phone.</summary>
        public static CollectionDto Collection
        {
            get
            {
                if (!loaded) Load();
                return collection;
            }
        }

        public static bool HasCollection => Collection != null;

        /// <summary>How many caught cards show this meme.</summary>
        public static int CountOf(string memeId)
        {
            if (Collection == null || string.IsNullOrEmpty(memeId)) return 0;
            return countByMeme.TryGetValue(memeId, out var n) ? n : 0;
        }

        /// <summary>Caught cards per server rarity ("common", "rare", "epic", "legendary").</summary>
        public static int CountOfRarity(string serverRarity)
        {
            if (Collection?.ByRarity == null) return 0;
            return Collection.ByRarity.TryGetValue(serverRarity, out var n) ? n : 0;
        }

        /// <summary>Downloads the collection. Offline: keeps the last known one (no error shown).</summary>
        public static async Awaitable Refresh()
        {
            if (!Session.HasPlayer) return;
            try
            {
                var fresh = await Api.GetCollection(Session.PlayerId);
                if (fresh == null) return;
                Set(fresh);
                PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(fresh));
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
            catch (ApiException)
            {
                // offline or server error: the screens keep the cached copy
            }
        }

        /// <summary>Re-reads the profile (coins, XP, rank, booster, streak). Offline: keeps the last known one.</summary>
        public static async Awaitable RefreshPlayer()
        {
            if (!Session.HasPlayer) return;
            try { Session.SetPlayer(await Api.GetPlayer(Session.PlayerId)); }
            catch (ApiException) { /* offline: keep the last known profile */ }
        }

        /// <summary>Drops the cached collection (the player was forgotten / saved data cleared).</summary>
        public static void Forget()
        {
            PlayerPrefs.DeleteKey(Key);
            collection = null;
            countByMeme = null;
            loaded = true;
        }

        /// <summary>Drops the in-memory copy so the next read comes from PlayerPrefs.</summary>
        public static void Reload() => loaded = false;

        static void Load()
        {
            loaded = true;
            collection = null;
            var json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            try { Set(JsonConvert.DeserializeObject<CollectionDto>(json)); }
            catch (JsonException) { collection = null; }
        }

        static void Set(CollectionDto value)
        {
            collection = value;
            loaded = true;
            countByMeme = new Dictionary<string, int>();
            if (value?.Cards == null) return;
            foreach (var card in value.Cards)
            {
                var meme = MemeCatalog.ForCard(card.CardId, card.Rarity);
                if (meme == null) continue;
                countByMeme.TryGetValue(meme.MemeId, out var n);
                countByMeme[meme.MemeId] = n + 1;
            }
        }
    }
}
