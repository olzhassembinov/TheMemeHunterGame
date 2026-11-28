using MemeHunter.Net;

namespace MemeHunter.UI
{
    /// <summary>
    /// Player ranks. The server sends only ids ("rank_1" .. "rank_5") and XP; the names live here.
    /// Minimum XP per rank must match app/progression.py on the backend.
    /// </summary>
    public static class Ranks
    {
        static readonly (string id, string name, int minXp)[] All =
        {
            ("rank_1", "Rookie", 0),
            ("rank_2", "Scout", 500),
            ("rank_3", "Hunter", 1500),
            ("rank_4", "Tracker", 3500),
            ("rank_5", "Legend", 7000),
        };

        public static int Count => All.Length;

        /// <summary>1 .. 5 (unknown ids count as 1).</summary>
        public static int Number(string id)
        {
            for (var i = 0; i < All.Length; i++)
                if (All[i].id == id) return i + 1;
            return 1;
        }

        public static string Name(string id) => All[Number(id) - 1].name;

        /// <summary>The rank number an XP amount reaches (1 .. 5).</summary>
        public static int NumberForXp(int xp)
        {
            var number = 1;
            for (var i = 0; i < All.Length; i++)
                if (xp >= All[i].minXp) number = i + 1;
            return number;
        }

        public static string NameOf(int number) => All[UnityEngine.Mathf.Clamp(number, 1, All.Length) - 1].name;

        /// <summary>XP inside the current rank and the XP the rank spans, for the progress bar. Top rank: full bar.</summary>
        public static (int current, int needed) Progress(PlayerDto player)
        {
            if (player == null) return (0, All[1].minXp);
            var floor = All[Number(player.Rank) - 1].minXp;
            var current = UnityEngine.Mathf.Max(0, player.Xp - floor);
            if (player.XpToNextRank == null) return (current, UnityEngine.Mathf.Max(1, current));
            return (current, current + player.XpToNextRank.Value);
        }
    }
}
