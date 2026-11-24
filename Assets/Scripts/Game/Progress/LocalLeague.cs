using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MemeHunter.Net;
using MemeHunter.UI;
using MemeHunter.UI.Avatars;
using Newtonsoft.Json;
using UnityEngine;

namespace MemeHunter.Game
{
    /// <summary>
    /// The weekly league behind the Leaderboard, simulated on this phone until the backend has a leaderboard
    /// endpoint. Four leagues named after the rarities: Common -> Uncommon -> Rare -> Legendary.
    /// A week runs Monday to Monday (local time). The player is in a group of 20: their own XP of the week (from the
    /// saved catches) against 19 generated rivals whose XP grows during the week. When a week ends, the top 5 move
    /// up a league and the bottom 3 move down; a week without a catch changes nothing (no penalty for a break).
    /// Rivals stay the same all week (seeded by week, league and player), so the board never reshuffles.
    /// A server version only has to fill the same Week / Row data.
    /// </summary>
    public static class LocalLeague
    {
        public const int GroupSize = 20;
        public const int PromoteCount = 5;
        public const int DemoteCount = 3;
        const string Key = "LocalLeague";

        /// <summary>A rival's XP at the end of an average week, per league (a catch gives 10 / 25 / 50 / 100 XP).</summary>
        static readonly float[] TypicalXp = { 90f, 160f, 260f, 400f };
        static readonly (int min, int max)[] RivalLevels = { (2, 6), (4, 9), (7, 13), (10, 18) };

        static readonly string[] RivalNames =
        {
            "DogeWhisperer", "SigmaSnail", "BhopBaron", "StonksSam", "NyanRunner", "PepeScout", "ChadTracker",
            "RickRoller", "SuchWowHunter", "MemeLord_KZ", "AlmatyAce", "SteppeSprinter", "KBTU_Kid", "BaursakBoss",
            "SnowLeopard", "KumysKing", "AportPicker", "TianShanTrek", "MedeuMemer", "KokTobeKid", "GigaGoose",
            "BruhBrigade", "VibeCheck99", "NoScopeNurik", "LagSwitchLeo", "PixelPilaf", "ZhetysuZoomer",
            "CringeCrusher", "MonkeMode", "YeetYurt", "BigBrainBek", "TouchGrassTim", "CatJamCaptain", "FrogFrenzy",
            "LoafLover", "WaffleWarden", "MemeMagnet", "TrollfaceTom", "PogChampPolina", "SkibidiSkier"
        };

        public sealed class Row
        {
            public string Name;
            public int Avatar;          // index into AvatarCatalog.All (-1: none chosen)
            public int Level;
            public int Xp;              // XP earned this week
            public bool IsPlayer;
            public int Rank;            // 1 = first place
        }

        public sealed class Week
        {
            public MemeRarity League;
            public DateTime Start;      // Monday 00:00, local time
            public DateTime End;        // the next Monday 00:00
            public List<Row> Rows;      // ranked, first place first
            public Row Player;

            public bool CanPromote => League < MemeRarity.Legendary;
            public bool CanDemote => League > MemeRarity.Common;
            public bool Promotes(int rank) => CanPromote && rank <= PromoteCount;
            public bool Demotes(int rank) => CanDemote && rank > Rows.Count - DemoteCount;

            /// <summary>XP the player still needs to reach the promotion zone (0 when in it, or in the top league).</summary>
            public int XpToPromotion => !CanPromote || Player.Rank <= PromoteCount ? 0 : Math.Max(1, Rows[PromoteCount - 1].Xp - Player.Xp);

            /// <summary>XP the player needs to leave the demotion zone (0 when safe).</summary>
            public int XpToSafety => !Demotes(Player.Rank) ? 0 : Math.Max(1, Rows[Rows.Count - DemoteCount - 1].Xp - Player.Xp + 1);
        }

        /// <summary>How a finished week ended for the player (shown once on the Leaderboard).</summary>
        public sealed class Result
        {
            public MemeRarity From;
            public MemeRarity To;
            public int Rank;
            public int Xp;
        }

        sealed class State
        {
            public string week;         // Monday of the week the league belongs to (yyyy-MM-dd)
            public int league;
            public int bestLeague;
            public int bestRank;        // 0 = no finished week yet
            public int weeksPlayed;
            public Result unseen;       // the last finished week, until the Leaderboard showed it
        }

        static State state;
        static State Data => state ??= Load();

        // ---------- What the Leaderboard reads (Current() first: it closes a finished week) ----------

        public static MemeRarity League => (MemeRarity)Data.league;
        public static MemeRarity BestLeague => (MemeRarity)Math.Max(Data.bestLeague, Data.league);
        public static int BestRank => Data.bestRank;
        public static int WeeksPlayed => Data.weeksPlayed;

        /// <summary>This week's standings. A week that ended since the last visit is closed first (promotion etc.).</summary>
        public static Week Current() => Current(DateTime.Now);

        public static Week Current(DateTime now)
        {
            Sync(now);
            var start = WeekStart(now);
            var progress = (float)((now - start).TotalDays / 7.0);
            return Build(start, (MemeRarity)state.league, Mathf.Clamp01(progress));
        }

        /// <summary>The last finished week if the player has not been told yet; afterwards it returns null.</summary>
        public static Result TakeUnseenResult()
        {
            var result = Data.unseen;
            if (result == null) return null;
            Data.unseen = null;
            Save();
            return result;
        }

        /// <summary>XP per day (Monday first) of the week that contains `now`, from the saved catches.</summary>
        public static int[] DailyXp(DateTime now)
        {
            var start = WeekStart(now);
            var days = new int[7];
            foreach (var caught in PlayerProgress.Caught)
            {
                var day = (int)Math.Floor((LocalTime(caught) - start).TotalDays);
                if (day >= 0 && day < 7) days[day] += XpOf(caught);
            }
            return days;
        }

        public static int XpBetween(DateTime from, DateTime to) =>
            PlayerProgress.Caught.Where(c => InRange(c, from, to)).Sum(XpOf);

        public static int CatchesBetween(DateTime from, DateTime to) =>
            PlayerProgress.Caught.Count(c => InRange(c, from, to));

        /// <summary>Monday 00:00 of the week that contains `now`.</summary>
        public static DateTime WeekStart(DateTime now)
        {
            var day = now.Date;
            return day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        }

        /// <summary>Forgets the league, e.g. after the DEV "Clear saved data".</summary>
        public static void Reset()
        {
            state = null;
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        /// <summary>Drops the in-memory copy so the next read comes from PlayerPrefs (tests restore saved data this way).</summary>
        public static void Reload() => state = null;

        /// <summary>Editor previews only: a fixed league state for this week (nothing is saved).</summary>
        public static void UsePreviewState(MemeRarity league, int bestRank, int weeksPlayed)
        {
            state = new State
            {
                week = Id(WeekStart(DateTime.Now)), league = (int)league, bestLeague = (int)league,
                bestRank = bestRank, weeksPlayed = weeksPlayed
            };
        }

        // ---------- Weeks ----------

        /// <summary>Closes the stored week once a new one has started, then remembers the current week.</summary>
        static void Sync(DateTime now)
        {
            state = Data;
            var current = WeekStart(now);
            var id = Id(current);
            if (state.week == id) return;

            // Only a week in the past is closed (a phone clock set back does not count as a finished week).
            if (state.week != null && TryParse(state.week, out var stored) && stored < current)
                Close(stored);
            state.week = id;
            Save();
        }

        static void Close(DateTime start)
        {
            var from = (MemeRarity)state.league;
            var week = Build(start, from, 1f);                     // rivals at their final XP
            if (week.Player.Xp <= 0) return;                       // no catch that week: nothing changes

            var rank = week.Player.Rank;
            var to = week.Promotes(rank) ? from + 1 : week.Demotes(rank) ? from - 1 : from;
            state.weeksPlayed++;
            state.bestRank = state.bestRank == 0 ? rank : Math.Min(state.bestRank, rank);
            state.league = (int)to;
            state.bestLeague = Math.Max(state.bestLeague, (int)to);
            state.unseen = new Result { From = from, To = to, Rank = rank, Xp = week.Player.Xp };
            Debug.Log($"[League] week of {Id(start)} closed: rank {rank}, {week.Player.Xp} XP, {from} -> {to}");
        }

        /// <summary>The group for one week. `progress` is how much of the week has passed (0..1).</summary>
        static Week Build(DateTime start, MemeRarity league, float progress)
        {
            var random = new System.Random(Hash(Id(start) + "|" + (int)league + "|" + PlayerKey()));
            var avatars = Math.Max(1, AvatarCatalog.All.Length);
            var names = RivalNames.OrderBy(_ => random.Next()).Take(GroupSize - 1).ToList();
            var typical = TypicalXp[(int)league];
            var levels = RivalLevels[(int)league];

            // The random numbers are drawn in the same order whatever the progress, so a rival keeps their
            // name, face, level and final XP all week; only how much of it they have earned so far changes.
            var rows = new List<Row>(GroupSize);
            for (var i = 0; i < GroupSize - 1; i++)
            {
                var spread = Mathf.Pow(i / (float)(GroupSize - 2), 1.35f);         // a quiet week .. a very busy one
                var finalXp = typical * (0.15f + 1.95f * spread) * (0.85f + 0.3f * (float)random.NextDouble());
                var pace = 0.6f + (float)random.NextDouble();                      // < 1 early bird, > 1 weekend rush
                var headStart = 0.08f * (float)random.NextDouble();
                var done = progress >= 1f ? 1f : Mathf.Clamp01(headStart + (1f - headStart) * Mathf.Pow(progress, pace));
                rows.Add(new Row
                {
                    Name = names[i],
                    Avatar = random.Next(avatars),
                    Level = random.Next(levels.min, levels.max + 1),
                    Xp = Mathf.FloorToInt(finalXp * done / 5f) * 5
                });
            }

            var player = new Row
            {
                Name = PlayerName(),
                Avatar = AvatarCatalog.SelectedIndex,
                Level = PlayerProgress.Level,
                Xp = XpBetween(start, start.AddDays(7)),
                IsPlayer = true
            };
            rows.Add(player);

            // Ties go to the player.
            rows = rows.OrderByDescending(r => r.Xp).ThenByDescending(r => r.IsPlayer).ThenBy(r => r.Name, StringComparer.Ordinal).ToList();
            for (var i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;
            return new Week { League = league, Start = start, End = start.AddDays(7), Rows = rows, Player = player };
        }

        // ---------- Helpers ----------

        public static string PlayerName()
        {
            var name = PlayerPrefs.GetString("Nickname", string.Empty).Trim();
            return string.IsNullOrEmpty(name) ? "Hunter" : name;
        }

        static string PlayerKey() => Session.HasPlayer ? Session.PlayerId : PlayerName();

        static DateTime LocalTime(PlayerProgress.CaughtMeme caught) =>
            DateTimeOffset.FromUnixTimeSeconds(caught.caughtAtUnix).LocalDateTime;

        static bool InRange(PlayerProgress.CaughtMeme caught, DateTime from, DateTime to)
        {
            var at = LocalTime(caught);
            return at >= from && at < to;
        }

        static int XpOf(PlayerProgress.CaughtMeme caught) => PlayerProgress.XpFor(Rarity.Parse(caught.rarity));

        static string Id(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        static bool TryParse(string id, out DateTime day) =>
            DateTime.TryParseExact(id, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);

        /// <summary>FNV-1a: the same seed on every device and run (string.GetHashCode is not guaranteed to be).</summary>
        static int Hash(string text)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in text) { hash ^= c; hash *= 16777619u; }
                return (int)hash;
            }
        }

        static State Load()
        {
            var json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json)) return new State();
            try { return JsonConvert.DeserializeObject<State>(json) ?? new State(); }
            catch (JsonException) { return new State(); }
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(Data));
            PlayerPrefs.Save();
        }
    }
}
