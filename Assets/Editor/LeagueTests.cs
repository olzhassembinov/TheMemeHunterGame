using System;
using System.Linq;
using MemeHunter.Game;
using MemeHunter.UI;
using UnityEditor;
using UnityEngine;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Checks the weekly league rules with fixed dates (no Play Mode, no server): week boundaries, the same rivals
    /// all week, rivals' XP only growing, the player's XP counted per week, promotion, a quiet week, demotion, a
    /// clock set back, and the top league. The Editor's saved progress and league are put back afterwards.
    /// Batch: Unity -batchmode -projectPath . -executeMethod MemeHunter.Editor.LeagueTests.Run (exit code 0 = pass).
    /// Also in the menu: Meme Hunter / Test weekly league.
    /// </summary>
    public static class LeagueTests
    {
        static int failures;

        public static void Run() => RunAll(exit: true);

        [MenuItem("Meme Hunter/Test weekly league")]
        static void RunFromMenu() => RunAll(exit: false);

        static void RunAll(bool exit)
        {
            failures = 0;
            var hadLeague = PlayerPrefs.HasKey("LocalLeague");
            var savedLeague = PlayerPrefs.GetString("LocalLeague", string.Empty);
            try
            {
                Tests();
            }
            catch (Exception e)
            {
                failures++;
                Debug.LogError("[LeagueTests] FAIL: exception " + e);
            }
            finally
            {
                if (hadLeague) PlayerPrefs.SetString("LocalLeague", savedLeague); else PlayerPrefs.DeleteKey("LocalLeague");
                PlayerPrefs.Save();
                PlayerProgress.Reload();
                LocalLeague.Reload();
            }

            Debug.Log(failures == 0 ? "[LeagueTests] PASS: all league checks" : $"[LeagueTests] FAIL: {failures} check(s) failed");
            if (exit) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static void Tests()
        {
            // Week A starts Monday 2 Nov 2026; B = 9 Nov, C = 16 Nov, D = 23 Nov.
            var a = new DateTime(2026, 11, 2);
            Check(a.DayOfWeek == DayOfWeek.Monday, "2 Nov 2026 is a Monday");
            Check(LocalLeague.WeekStart(a.AddDays(2).AddHours(15)) == a, "Wednesday afternoon belongs to week A");
            Check(LocalLeague.WeekStart(a.AddDays(7).AddMinutes(-1)) == a, "Sunday 23:59 still belongs to week A");
            Check(LocalLeague.WeekStart(a.AddDays(7)) == a.AddDays(7), "Monday 00:00 starts week B");

            // A new player: Common league, a group of 20, ranked.
            LocalLeague.Reset();
            PlayerProgress.UsePreviewCatches();
            var tuesday = LocalLeague.Current(a.AddDays(1).AddHours(10));
            Check(tuesday.League == MemeRarity.Common, "new players start in the Common League");
            Check(tuesday.Rows.Count == LocalLeague.GroupSize && tuesday.Rows.Count(r => r.IsPlayer) == 1, "20 rows, one of them the player");
            Check(tuesday.Rows.Select(r => r.Rank).SequenceEqual(Enumerable.Range(1, LocalLeague.GroupSize)), "ranks 1..20");
            Check(Ordered(tuesday), "sorted by XP, best first");
            Check(tuesday.Rows.Select(r => r.Name).Distinct().Count() == LocalLeague.GroupSize, "no name twice");

            // Same week, same rivals; their XP only grows during the week.
            var again = LocalLeague.Current(a.AddDays(1).AddHours(10));
            Check(tuesday.Rows.Select(r => r.Name + r.Xp).SequenceEqual(again.Rows.Select(r => r.Name + r.Xp)), "the same board when asked twice");
            var thursday = LocalLeague.Current(a.AddDays(3).AddHours(18));
            var sunday = LocalLeague.Current(a.AddDays(7).AddMinutes(-1));
            Check(Rivals(tuesday).All(r => Xp(thursday, r.Name) >= r.Xp) && Rivals(thursday).All(r => Xp(sunday, r.Name) >= r.Xp),
                "rivals' XP only grows (Tue <= Thu <= Sun)");
            Check(Rivals(sunday).Sum(r => r.Xp) > Rivals(tuesday).Sum(r => r.Xp), "rivals earn XP during the week");

            // The player's XP counts only this week's catches, per day.
            PlayerProgress.UsePreviewCatches((TopMeme(), a.AddDays(2).AddHours(12)), (TopMeme(), a.AddDays(-3)));
            var legend = PlayerProgress.Caught[0];
            var legendXp = PlayerProgress.XpFor(Rarity.Parse(legend.rarity));
            var withCatch = LocalLeague.Current(a.AddDays(3));
            Check(withCatch.Player.Xp == legendXp, $"player XP this week = {withCatch.Player.Xp} (expected {legendXp}, last week's catch not counted)");
            var days = LocalLeague.DailyXp(a.AddDays(3));
            Check(days[2] == legendXp && days.Sum() == legendXp, "daily XP: all on Wednesday");

            // Promotion: a big week A, then Monday of week B closes it.
            PlayerProgress.UsePreviewCatches(Enumerable.Range(0, 6).Select(i => (TopMeme(), a.AddDays(1).AddHours(i))).ToArray());
            var bigWeek = LocalLeague.Current(a.AddDays(6).AddHours(20));
            Check(bigWeek.Promotes(bigWeek.Player.Rank), $"{bigWeek.Player.Xp} XP puts the player in the promotion zone (rank {bigWeek.Player.Rank})");
            var weekB = LocalLeague.Current(a.AddDays(7).AddHours(9));
            var result = LocalLeague.TakeUnseenResult();
            Check(weekB.League == MemeRarity.Uncommon, "promoted to the Uncommon League");
            Check(result != null && result.From == MemeRarity.Common && result.To == MemeRarity.Uncommon && result.Rank <= LocalLeague.PromoteCount,
                "week result: Common -> Uncommon, top 5");
            Check(LocalLeague.TakeUnseenResult() == null, "the result is shown only once");
            Check(LocalLeague.WeeksPlayed == 1 && LocalLeague.BestRank == result?.Rank && LocalLeague.BestLeague == MemeRarity.Uncommon,
                "records: 1 week played, best rank and best league kept");
            Check(weekB.Player.Xp == 0, "a new week starts at 0 XP");

            // A quiet week B (no catches) changes nothing.
            var weekC = LocalLeague.Current(a.AddDays(14).AddHours(9));
            Check(weekC.League == MemeRarity.Uncommon && LocalLeague.TakeUnseenResult() == null && LocalLeague.WeeksPlayed == 1,
                "a week without catches: same league, no result, not counted");

            // Demotion: one small catch in week C is the bottom of the Uncommon group.
            PlayerProgress.UsePreviewCatches((SmallMeme(), a.AddDays(15).AddHours(12)));
            var smallWeek = LocalLeague.Current(a.AddDays(20).AddHours(23));
            Check(smallWeek.Demotes(smallWeek.Player.Rank) && smallWeek.XpToSafety > 0, $"one small catch is in the demotion zone (rank {smallWeek.Player.Rank}, {smallWeek.XpToSafety} XP to safety)");
            var weekD = LocalLeague.Current(a.AddDays(21).AddHours(8));
            result = LocalLeague.TakeUnseenResult();
            Check(weekD.League == MemeRarity.Common && result != null && result.To == MemeRarity.Common, "demoted back to the Common League");

            // A phone clock set back to an earlier week closes nothing.
            var earlier = LocalLeague.Current(a.AddDays(8));
            Check(earlier.League == MemeRarity.Common && LocalLeague.TakeUnseenResult() == null, "clock set back: no week closed");

            // Promotion zone maths and the top league.
            PlayerProgress.UsePreviewCatches();
            var empty = LocalLeague.Current(a.AddDays(3));
            Check(empty.XpToPromotion == Math.Max(1, empty.Rows[LocalLeague.PromoteCount - 1].Xp - empty.Player.Xp), "XP to the promotion zone");
            LocalLeague.UsePreviewState(MemeRarity.Legendary, 1, 5);
            PlayerProgress.UsePreviewCatches(Enumerable.Range(0, 10).Select(i => (TopMeme(), DateTime.Now.AddMinutes(-i))).ToArray());
            var top = LocalLeague.Current();
            Check(!top.CanPromote && top.XpToPromotion == 0 && !top.Promotes(1) && top.CanDemote, "Legendary: nothing above, the bottom 3 move down");
        }

        static string TopMeme() => MemeCatalog.All.OrderByDescending(m => m.Rarity).First().MemeId;
        static string SmallMeme() => MemeCatalog.All.OrderBy(m => m.Rarity).First().MemeId;

        static bool Ordered(LocalLeague.Week week)
        {
            for (var i = 1; i < week.Rows.Count; i++)
                if (week.Rows[i].Xp > week.Rows[i - 1].Xp) return false;
            return true;
        }

        static LocalLeague.Row[] Rivals(LocalLeague.Week week) => week.Rows.Where(r => !r.IsPlayer).ToArray();
        static int Xp(LocalLeague.Week week, string name) => week.Rows.First(r => r.Name == name).Xp;

        static void Check(bool ok, string what)
        {
            if (!ok) failures++;
            if (ok) Debug.Log("[LeagueTests] ok: " + what);
            else Debug.LogError("[LeagueTests] FAIL: " + what);
        }
    }
}
