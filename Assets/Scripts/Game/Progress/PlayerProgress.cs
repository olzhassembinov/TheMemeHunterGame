using System;
using System.Collections.Generic;
using System.Linq;
using MemeHunter.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace MemeHunter.Game
{
    /// <summary>
    /// What the player caught and their XP, saved on the device (the backend has no collection / XP endpoint yet).
    /// Rewards are always instant: nothing here is ever "pending" (thesis rule, see project PDF).
    /// </summary>
    public static class PlayerProgress
    {
        const string Key = "PlayerProgress";

        [Serializable]
        public sealed class CaughtMeme
        {
            public string memeId;
            public string cardId;
            public string rarity;
            public long caughtAtUnix;
        }

        [Serializable]
        sealed class Data
        {
            public int xp;
            public List<CaughtMeme> caught = new List<CaughtMeme>();
        }

        public readonly struct CatchResult
        {
            public readonly MemeData Meme;
            public readonly bool IsDuplicate;
            public readonly int XpGained;
            public readonly bool LeveledUp;

            public CatchResult(MemeData meme, bool duplicate, int xp, bool leveledUp)
            {
                Meme = meme; IsDuplicate = duplicate; XpGained = xp; LeveledUp = leveledUp;
            }
        }

        public static event Action Changed;

        static Data data;
        static Data Current => data ??= Load();

        public static int Xp => Current.xp;
        public static IReadOnlyList<CaughtMeme> Caught => Current.caught;

        /// <summary>Distinct memes owned (duplicates are counted once; they are turned into coins by the server reward).</summary>
        public static bool Owns(string memeId) => Current.caught.Any(c => c.memeId == memeId);

        public static int CountOf(string memeId) => Current.caught.Count(c => c.memeId == memeId);

        // ---------- Levels: 100 XP for level 2, each next level needs 50 XP more ----------

        public static int Level => LevelFor(Xp);

        public static int LevelFor(int xp)
        {
            var level = 1;
            var need = 100;
            while (xp >= need) { xp -= need; level++; need += 50; }
            return level;
        }

        /// <summary>XP inside the current level and the XP the level needs (for the progress bar).</summary>
        public static (int current, int needed) LevelProgress()
        {
            var xp = Xp;
            var need = 100;
            while (xp >= need) { xp -= need; need += 50; }
            return (xp, need);
        }

        public static int XpFor(MemeRarity rarity) => rarity switch
        {
            MemeRarity.Uncommon => 25,
            MemeRarity.Rare => 50,
            MemeRarity.Legendary => 100,
            _ => 10
        };

        /// <summary>Stores a catch and its XP. Returns what the reveal popup should show.</summary>
        public static CatchResult RecordCatch(string cardId, string serverRarity)
        {
            var meme = MemeCatalog.ForCard(cardId, serverRarity);
            var rarity = Rarity.Parse(serverRarity);
            var duplicate = meme != null && Owns(meme.MemeId);
            var levelBefore = Level;
            var xp = XpFor(rarity);

            Current.xp += xp;
            Current.caught.Add(new CaughtMeme
            {
                memeId = meme != null ? meme.MemeId : string.Empty,
                cardId = cardId,
                rarity = rarity.ToString(),
                caughtAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            Save();
            return new CatchResult(meme, duplicate, xp, Level > levelBefore);
        }

        /// <summary>Editor previews only: pretend these memes were caught (nothing is saved).</summary>
        public static void UsePreviewData(int xp, params (string memeId, int count)[] caught)
        {
            data = new Data { xp = xp };
            foreach (var (memeId, count) in caught)
                for (var i = 0; i < count; i++)
                    data.caught.Add(new CaughtMeme { memeId = memeId, rarity = MemeCatalog.Find(memeId)?.Rarity.ToString() });
        }

        public static void Reset()
        {
            data = new Data();
            Save();
        }

        static Data Load()
        {
            var json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json)) return new Data();
            try { return JsonConvert.DeserializeObject<Data>(json) ?? new Data(); }
            catch (JsonException) { return new Data(); }
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(Current));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
