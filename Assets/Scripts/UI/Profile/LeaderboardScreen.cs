using System;
using System.Collections.Generic;
using System.Linq;
using MemeHunter.Game;
using MemeHunter.Net;
using MemeHunter.UI.Avatars;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Profile
{
    /// <summary>
    /// The Leaderboard (not in Figma yet; built in the style of the Figma screens): LEADERBOARD header, a ME / GLOBAL
    /// switch, and the weekly league from LocalLeague.
    /// GLOBAL: league card with the four leagues (Common -> Legendary, rarity colours), a top-3 podium, ranks 4-20
    /// with the promotion (top 5) and demotion (bottom 3) zones, and the player's own row pinned at the bottom.
    /// ME: the player's card, this week's XP / rank / catches, XP per day, the league goal and all-time records.
    /// The only social list in the game; there are no friends.
    /// Since the server has a global XP leaderboard (GET /leaderboard) the weekly league is switched off
    /// (Features.WeeklyLeague) and the same screen shows the server data: GLOBAL = podium, places 4-20 and the
    /// player's own place pinned at the bottom; ME = rank, XP to the next rank, place, streak and caught cards.
    /// </summary>
    public sealed class LeaderboardScreen : FigmaScreen
    {
        public enum Page { Me, Global }

        /// <summary>Rivals are simulated on the phone until the server leaderboard exists: say so in small print.</summary>
        const bool ShowSimulatedNote = true;

        const float Side = 18f;             // screen margin
        const float Width = 462f;           // 498 - 2 x 18
        const float ContentTop = 152f;      // below the header and the switch
        const float CloseArea = 95f;        // the round close button at the bottom
        const float FooterHeight = 66f;

        static readonly Color CardFill = Fig.Water.WithAlpha(0.25f);
        static readonly Color RowFill = new Color(1f, 1f, 1f, 0.62f);
        static readonly Color PlayerFill = Fig.Hex("E4F0FF");
        static readonly Color PlayerBorder = MemeHunterUiColors.FormBlue;
        static readonly Color PromoteInk = Fig.Hex("1E9E48");
        static readonly Color DemoteInk = Fig.Hex("E5484D");
        static readonly Color Muted = Fig.MainText.WithAlpha(0.6f);
        static readonly Color Silver = Fig.Hex("AEBBD3");
        static readonly Color Bronze = Fig.Hex("D9965B");
        static readonly Color PillFrom = Fig.Hex("8FBCFF");
        static readonly Color PillTo = Fig.Hex("3A98E4");

        static Page page = Page.Global;

        /// <summary>The open page (kept while the app runs). Settable for editor previews.</summary>
        public static Page Shown { get => page; set => page = value; }

        static LeaderboardDto board;          // last server answer, kept while the app runs
        static bool loadFailed;
        bool requestedThisVisit;

        protected override void Build(RectTransform root)
        {
            var title = Fig.Display(root, "LEADERBOARD", 45f, Fig.MainText);
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = true;
            title.fontSizeMin = 30f;
            title.fontSizeMax = 45f;
            title.rectTransform.At(88f, 22f, 322f, 61f);
            Fig.Line(root, 0f, 52f, 90f, Fig.MainText);
            Fig.Line(root, 408f, 52f, 90f, Fig.MainText);

            BuildSwitch(root);
            if (Features.WeeklyLeague)
            {
                var week = LocalLeague.Current();
                if (page == Page.Global) BuildGlobal(root, week);
                else BuildMe(root, week);
                CloseButton(root);
                if (Application.isPlaying) StartCoroutine(ShowWeekResultWhenShown());
                return;
            }

            if (page == Page.Global) BuildServerGlobal(root);
            else BuildServerMe(root);
            CloseButton(root);
            if (Application.isPlaying && !requestedThisVisit)
            {
                requestedThisVisit = true;
                LoadFromServer();
            }
        }

        void OnDisable() => requestedThisVisit = false;

        async void LoadFromServer()
        {
            try
            {
                board = await Api.GetLeaderboard(Session.HasPlayer ? Session.PlayerId : null, 20);
                loadFailed = false;
            }
            catch (ApiException)
            {
                loadFailed = board == null;                                // keep showing the last board offline
            }
            await ServerProgress.RefreshPlayer();                          // rank, XP to the next rank, streak (ME page)
            await ServerProgress.Refresh();                                // caught cards per rarity (ME page)
            if (this != null && isActiveAndEnabled) Render();
        }

        // ---------- ME / GLOBAL switch ----------

        void BuildSwitch(RectTransform root)
        {
            var track = Ui.Rounded("Switch", root, Fig.MainText.WithAlpha(0.08f), 23f);
            track.raycastTarget = false;
            track.rectTransform.At(99f, 92f, 300f, 46f);
            Segment(track.rectTransform, "ME", Page.Me, 0f);
            Segment(track.rectTransform, "GLOBAL", Page.Global, 150f);
        }

        void Segment(RectTransform track, string label, Page target, float x)
        {
            var active = page == target;
            var pill = Ui.Rounded(label, track, active ? Color.white : new Color(1f, 1f, 1f, 0f), 20f);
            if (active) pill.gameObject.AddComponent<HorizontalGradientEffect>().SetColors(PillFrom, PillTo);
            pill.rectTransform.At(x + 3f, 3f, 144f, 40f);
            var text = Fig.Display(pill.transform, label, 21f, active ? Color.white : Fig.MainText);
            text.alignment = TextAlignmentOptions.Center;
            text.Tracking(1.5f);
            text.rectTransform.Stretch();

            var button = pill.gameObject.AddComponent<Button>();
            button.targetGraphic = pill;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                if (page == target) return;
                page = target;
                Render();
            });
        }

        // ---------- GLOBAL ----------

        void BuildGlobal(RectTransform root, LocalLeague.Week week)
        {
            var content = ScrollArea(root, CloseArea + FooterHeight + 10f);
            var y = 4f;

            y = LeagueCard(content, week, y) + 14f;
            y = Podium(content, week, y) + 12f;

            // Ranks 4..20 with the zone separators.
            foreach (var row in week.Rows.Where(r => r.Rank > 3))
            {
                if (week.CanDemote && row.Rank == week.Rows.Count - LocalLeague.DemoteCount + 1)
                    y = Separator(content, y, "↓  DEMOTION ZONE  ↓", DemoteInk);
                y = RankRow(content, week, row, y) + 8f;
                if (week.CanPromote && row.Rank == LocalLeague.PromoteCount)
                    y = Separator(content, y, "↑  PROMOTION ZONE  ↑", PromoteInk);
            }

            if (ShowSimulatedNote)
            {
                var note = Fig.Body(content, "Rivals are simulated on this phone until online leagues open.", 13f, Muted);
                note.alignment = TextAlignmentOptions.Center;
                note.rectTransform.At(Side, y + 4f, Width, 36f);
                y += 44f;
            }
            content.sizeDelta = new Vector2(0f, y + 16f);

            PlayerFooter(root, week);
        }

        float LeagueCard(RectTransform content, LocalLeague.Week week, float y)
        {
            var card = Ui.Rounded("League", content, CardFill, 26f);
            card.raycastTarget = false;
            card.rectTransform.At(Side, y, Width, 150f);

            // The four leagues on a line; the current one large with the trophy, the higher ones faded.
            Fig.Line(card.transform, 60f, 46f, Width - 120f, Fig.MainText.WithAlpha(0.18f));
            for (var i = 0; i < 4; i++)
            {
                var tier = (MemeRarity)i;
                var current = tier == week.League;
                var size = current ? 66f : 38f;
                var centreX = Width * (i + 0.5f) / 4f;
                var badge = LeagueBadge(card.transform, tier, size, current);
                badge.At(centreX - size / 2f, 46f - size / 2f, size, size);
                if (tier > week.League)
                    badge.gameObject.AddComponent<CanvasGroup>().alpha = 0.35f;
            }

            var name = Fig.Display(card.transform, LeagueName(week.League).ToUpperInvariant(), 30f, TierInk(week.League));
            name.alignment = TextAlignmentOptions.Center;
            name.Glow(Rarity.ColorOf(week.League));
            name.rectTransform.At(0f, 84f, Width, 36f);

            var rules = week.CanPromote && week.CanDemote ? "Top 5 move up · bottom 3 move down"
                : week.CanPromote ? "Top 5 move up"
                : "Top league · bottom 3 move down";
            var line = Fig.Body(card.transform, $"{rules} · ends in {TimeLeft(week.End)}", 15f, Fig.MainText);
            line.alignment = TextAlignmentOptions.Center;
            line.rectTransform.At(8f, 120f, Width - 16f, 22f);
            return y + 150f;
        }

        float Podium(RectTransform content, LocalLeague.Week week, float y)
        {
            const float height = 270f;                  // room for the crown above first place
            // Columns: 2nd (left), 1st (middle), 3rd (right). Block heights make the podium steps.
            var places = new (int index, float centreX, float avatar, float block, Color medal)[]
            {
                (1, 99f, 82f, 56f, Silver),
                (0, 249f, 102f, 82f, MemeHunterUiColors.LegendaryGold),
                (2, 399f, 82f, 40f, Bronze),
            };
            foreach (var place in places)
            {
                if (place.index >= week.Rows.Count) continue;
                var row = week.Rows[place.index];
                var blockTop = y + height - place.block;
                var blockWidth = place.index == 0 ? 136f : 126f;

                var block = Ui.Rounded("Step " + row.Rank, content, place.medal.WithAlpha(0.55f), 16f);
                block.raycastTarget = false;
                block.rectTransform.At(place.centreX - blockWidth / 2f, blockTop, blockWidth, place.block);
                var number = Fig.Display(block.transform, row.Rank.ToString(), place.index == 0 ? 40f : 32f, Color.white);
                number.alignment = TextAlignmentOptions.Center;
                number.rectTransform.At(0f, 0f, blockWidth, place.block);

                var xp = Fig.Display(content, row.Xp + " XP", 18f, Fig.MainText);
                xp.alignment = TextAlignmentOptions.Center;
                xp.rectTransform.At(place.centreX - 70f, blockTop - 25f, 140f, 22f);

                var name = Fig.Body(content, row.Name, 15f, row.IsPlayer ? PlayerBorder : Fig.MainText, TextWeight.Bold);
                name.alignment = TextAlignmentOptions.Center;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Ellipsis;
                name.rectTransform.At(place.centreX - 70f, blockTop - 47f, 140f, 22f);

                var avatarTop = blockTop - 52f - place.avatar;
                var avatar = Avatar(content, row.Avatar, place.avatar, place.medal, place.index == 0 ? 5f : 4f);
                avatar.At(place.centreX - place.avatar / 2f, avatarTop, place.avatar, place.avatar);

                // Rank chip on the avatar's lower right.
                var chip = Ui.Rounded("Chip", content, place.medal, 14f);
                chip.raycastTarget = false;
                chip.rectTransform.At(place.centreX + place.avatar * 0.5f - 24f, avatarTop + place.avatar - 26f, 28f, 28f);
                var chipText = Fig.Display(chip.transform, row.Rank.ToString(), 17f, Color.white);
                chipText.alignment = TextAlignmentOptions.Center;
                chipText.rectTransform.Stretch();

                if (place.index == 0) Crown(content, place.centreX, avatarTop - 6f);
            }
            return y + height;
        }

        float RankRow(RectTransform content, LocalLeague.Week week, LocalLeague.Row row, float y)
        {
            var rect = Row(content, row.IsPlayer, y);

            var rankColor = week.Promotes(row.Rank) ? PromoteInk : week.Demotes(row.Rank) ? DemoteInk : Fig.MainText;
            var rank = Fig.Display(rect, row.Rank.ToString(), 22f, rankColor);
            rank.alignment = TextAlignmentOptions.Center;
            rank.rectTransform.At(6f, 0f, 42f, 60f);

            Avatar(rect, row.Avatar, 42f, row.IsPlayer ? PlayerBorder : Color.white, 3f).At(52f, 9f, 42f, 42f);
            NameAndLevel(rect, row);

            var xp = Fig.Display(rect, row.Xp + " XP", 21f, Fig.MainText);
            xp.alignment = TextAlignmentOptions.MidlineRight;
            xp.rectTransform.At(300f, 0f, 146f, 60f);
            return y + 60f;
        }

        static RectTransform Row(RectTransform content, bool player, float y)
        {
            RectTransform rect;
            if (player)
                rect = Ui.BorderedCard("You", content, PlayerFill, PlayerBorder, 18f, 2.5f);
            else
            {
                var fill = Ui.Rounded("Row", content, RowFill, 18f);
                fill.raycastTarget = false;
                rect = fill.rectTransform;
            }
            rect.At(Side, y, Width, 60f);
            return rect;
        }

        static void NameAndLevel(RectTransform rect, LocalLeague.Row row)
        {
            var name = Fig.Body(rect, row.Name, 17f, Fig.MainText, TextWeight.Bold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.rectTransform.At(106f, 9f, 190f, 24f);
            var level = Fig.Body(rect, row.IsPlayer ? $"Level {row.Level} · you" : $"Level {row.Level}", 13f, Muted);
            level.rectTransform.At(106f, 33f, 190f, 18f);
        }

        static float Separator(RectTransform content, float y, string label, Color ink)
        {
            var text = Fig.Body(content, label, 13f, ink, TextWeight.Bold);
            text.alignment = TextAlignmentOptions.Center;
            text.Tracking(1.4f);
            text.rectTransform.At(Side + 110f, y + 2f, Width - 220f, 22f);
            Fig.Line(content, Side, y + 13f, 104f, ink.WithAlpha(0.6f));
            Fig.Line(content, Side + Width - 104f, y + 13f, 104f, ink.WithAlpha(0.6f));
            return y + 32f;
        }

        /// <summary>The player's row, always visible above the close button, with what to do next.</summary>
        static void PlayerFooter(RectTransform root, LocalLeague.Week week)
        {
            var me = week.Player;
            var card = Ui.BorderedCard("Your Rank", root, PlayerFill, PlayerBorder, 20f, 2.5f);
            card.AtBottom(Side, 1080f - CloseArea - FooterHeight, Width, FooterHeight);

            var rank = Fig.Display(card, "#" + me.Rank, 24f, Fig.MainText);
            rank.alignment = TextAlignmentOptions.Center;
            rank.enableAutoSizing = true;
            rank.fontSizeMin = 16f;
            rank.fontSizeMax = 24f;
            rank.rectTransform.At(6f, 0f, 50f, FooterHeight);
            Avatar(card, me.Avatar, 44f, PlayerBorder, 3f).At(58f, 11f, 44f, 44f);

            var name = Fig.Body(card, me.Name, 16f, Fig.MainText, TextWeight.Bold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.rectTransform.At(112f, 10f, 200f, 22f);
            var (status, color) = Status(week);
            var line = Fig.Body(card, status, 13f, color, TextWeight.Bold);
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.rectTransform.At(112f, 34f, 210f, 20f);

            var xp = Fig.Display(card, me.Xp + " XP", 22f, Fig.MainText);
            xp.alignment = TextAlignmentOptions.MidlineRight;
            xp.rectTransform.At(316f, 0f, 132f, FooterHeight);
        }

        static (string text, Color color) Status(LocalLeague.Week week)
        {
            if (week.Demotes(week.Player.Rank)) return ($"↓ {week.XpToSafety} XP to leave the demotion zone", DemoteInk);
            if (week.Promotes(week.Player.Rank)) return ("↑ In the promotion zone", PromoteInk);
            if (week.CanPromote) return ($"{week.XpToPromotion} XP to the promotion zone", Fig.MainText);
            return ("Safe in the top league", PromoteInk);
        }

        // ---------- ME ----------

        void BuildMe(RectTransform root, LocalLeague.Week week)
        {
            var content = ScrollArea(root, CloseArea);
            var me = week.Player;
            var now = DateTime.Now;
            var y = 4f;

            // Player card.
            var card = Ui.Rounded("Player", content, CardFill, 26f);
            card.raycastTarget = false;
            card.rectTransform.At(Side, y, Width, 150f);
            Avatar(card.transform, me.Avatar, 112f, Rarity.ColorOf(week.League), 5f).At(18f, 19f, 112f, 112f);
            var name = Fig.Display(card.transform, me.Name, 32f, Fig.MainText);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.enableAutoSizing = true;
            name.fontSizeMin = 20f;
            name.fontSizeMax = 32f;
            name.rectTransform.At(148f, 22f, 300f, 40f);
            var league = Fig.Body(card.transform, $"{LeagueName(week.League).ToUpperInvariant()} · #{me.Rank} OF {week.Rows.Count}", 15f,
                TierInk(week.League), TextWeight.Bold);
            league.Tracking(1.2f);
            league.rectTransform.At(148f, 66f, 300f, 22f);
            var (current, needed) = PlayerProgress.LevelProgress();
            var level = Fig.Body(card.transform, $"Level {PlayerProgress.Level} · {current}/{needed} XP to level {PlayerProgress.Level + 1}", 15f, Fig.MainText);
            level.rectTransform.At(148f, 92f, 300f, 22f);
            y += 162f;

            // This week.
            var weekCatches = LocalLeague.CatchesBetween(week.Start, week.End);
            y = Tiles(content, y, ("WEEK XP", me.Xp.ToString(), Fig.MainText), ("RANK", "#" + me.Rank, Fig.MainText),
                ("CATCHES", weekCatches.ToString(), Fig.MainText)) + 12f;
            y = DailyChart(content, y, LocalLeague.DailyXp(now), week) + 12f;
            y = GoalCard(content, y, week) + 12f;

            // Records and all-time numbers.
            var best = LocalLeague.BestRank;
            var bestLeague = LocalLeague.BestLeague;
            y = Tiles(content, y, ("BEST RANK", best > 0 ? "#" + best : "—", Fig.MainText),
                ("TOP LEAGUE", bestLeague.ToString().ToUpperInvariant(), TierInk(bestLeague)),
                ("WEEKS", LocalLeague.WeeksPlayed.ToString(), Fig.MainText)) + 12f;
            var owned = MemeCatalog.All.Count(m => PlayerProgress.Owns(m.MemeId));
            y = Tiles(content, y, ("TOTAL XP", PlayerProgress.Xp.ToString(), Fig.MainText),
                ("CAUGHT", PlayerProgress.Caught.Count.ToString(), Fig.MainText),
                ("COLLECTION", $"{owned}/{MemeCatalog.All.Length}", Fig.MainText));
            content.sizeDelta = new Vector2(0f, y + 20f);
        }

        static float Tiles(RectTransform content, float y, params (string label, string value, Color ink)[] tiles)
        {
            const float gap = 10f;
            var width = (Width - gap * (tiles.Length - 1)) / tiles.Length;
            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = Ui.Rounded(tiles[i].label, content, CardFill, 20f);
                tile.raycastTarget = false;
                tile.rectTransform.At(Side + i * (width + gap), y, width, 96f);
                var label = Fig.Body(tile.transform, tiles[i].label, 12f, Muted, TextWeight.Bold);
                label.alignment = TextAlignmentOptions.Center;
                label.Tracking(1.2f);
                label.rectTransform.At(0f, 14f, width, 16f);
                var value = Fig.Display(tile.transform, tiles[i].value, 30f, tiles[i].ink);
                value.alignment = TextAlignmentOptions.Center;
                value.enableAutoSizing = true;
                value.fontSizeMin = 16f;
                value.fontSizeMax = 30f;
                value.rectTransform.At(6f, 36f, width - 12f, 44f);
            }
            return y + 96f;
        }

        static float DailyChart(RectTransform content, float y, int[] days, LocalLeague.Week week)
        {
            const float height = 200f, baseline = 162f, maxBar = 96f, barWidth = 30f;
            var card = Ui.Rounded("This Week", content, CardFill, 24f);
            card.raycastTarget = false;
            card.rectTransform.At(Side, y, Width, height);

            var title = Fig.Body(card.transform, "XP THIS WEEK", 13f, Muted, TextWeight.Bold);
            title.Tracking(1.4f);
            title.rectTransform.At(20f, 14f, 220f, 18f);
            var left = Fig.Body(card.transform, "ends in " + TimeLeft(week.End), 13f, Muted);
            left.alignment = TextAlignmentOptions.Right;
            left.rectTransform.At(Width - 240f, 14f, 220f, 18f);

            var today = Mathf.Clamp((int)(DateTime.Now.Date - week.Start).TotalDays, 0, 6);
            var top = Mathf.Max(50, days.Max());
            var slot = (Width - 40f) / 7f;
            var letters = new[] { "M", "T", "W", "T", "F", "S", "S" };
            for (var i = 0; i < 7; i++)
            {
                var centreX = 20f + slot * (i + 0.5f);
                var future = i > today;
                var barHeight = days[i] > 0 ? Mathf.Max(8f, maxBar * days[i] / top) : 6f;
                var bar = Ui.Rounded("Day " + (i + 1), card.transform, Color.white, 8f);
                bar.raycastTarget = false;
                if (i == today && days[i] > 0) bar.gameObject.AddComponent<VerticalGradientEffect>().SetColors(Fig.XpFrom, Fig.XpTo);
                else bar.color = future ? Fig.MainText.WithAlpha(0.07f) : days[i] > 0 ? Fig.XpFrom.WithAlpha(0.6f) : Fig.MainText.WithAlpha(0.14f);
                bar.rectTransform.At(centreX - barWidth / 2f, baseline - barHeight, barWidth, barHeight);

                if (days[i] > 0)
                {
                    var value = Fig.Display(card.transform, days[i].ToString(), 13f, Fig.MainText);
                    value.alignment = TextAlignmentOptions.Center;
                    value.rectTransform.At(centreX - 26f, baseline - barHeight - 18f, 52f, 16f);
                }
                var letter = Fig.Body(card.transform, letters[i], 13f, i == today ? Fig.MainText : Muted, TextWeight.Bold);
                letter.alignment = TextAlignmentOptions.Center;
                letter.rectTransform.At(centreX - 20f, baseline + 8f, 40f, 18f);
            }
            return y + height;
        }

        static float GoalCard(RectTransform content, float y, LocalLeague.Week week)
        {
            var me = week.Player;
            string message;
            float fill;
            var next = week.CanPromote ? LeagueName(week.League + 1) : null;
            if (week.Demotes(me.Rank))
            {
                message = $"You're in the demotion zone. Earn {week.XpToSafety} XP to climb out before Monday!";
                fill = Fraction(me.Xp, week.Rows[week.Rows.Count - LocalLeague.DemoteCount - 1].Xp + 1);
            }
            else if (week.Promotes(me.Rank))
            {
                message = $"You're in the promotion zone! Stay in the top {LocalLeague.PromoteCount} until Monday to reach the {next}.";
                fill = 1f;
            }
            else if (week.CanPromote)
            {
                message = $"Earn {week.XpToPromotion} more XP to reach the promotion zone and move up to the {next}.";
                fill = Fraction(me.Xp, week.Rows[LocalLeague.PromoteCount - 1].Xp);
            }
            else
            {
                message = "You're in the top league. Stay out of the bottom 3 to keep your place!";
                fill = 1f;
            }

            var card = Ui.Rounded("League Goal", content, CardFill, 24f);
            card.raycastTarget = false;
            card.rectTransform.At(Side, y, Width, 124f);
            var title = Fig.Body(card.transform, "LEAGUE GOAL", 13f, Muted, TextWeight.Bold);
            title.Tracking(1.4f);
            title.rectTransform.At(20f, 14f, 300f, 18f);
            var text = Fig.Body(card.transform, message, 16f, Fig.MainText);
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 16f;
            text.rectTransform.At(20f, 36f, Width - 40f, 46f);

            var track = Ui.Rounded("Track", card.transform, Fig.XpTrack.WithAlpha(0.45f), 6f);
            track.raycastTarget = false;
            track.rectTransform.At(20f, 94f, Width - 40f, 12f);
            var bar = Ui.Rounded("Fill", card.transform, Color.white, 6f);
            bar.raycastTarget = false;
            bar.gameObject.AddComponent<HorizontalGradientEffect>().SetColors(Fig.XpFrom, Fig.XpTo);
            bar.rectTransform.At(20f, 94f, Mathf.Max(12f, (Width - 40f) * fill), 12f);
            return y + 124f;
        }

        static float Fraction(int value, int target) => target <= 0 ? 1f : Mathf.Clamp01(value / (float)target);

        // ---------- Server leaderboard: GLOBAL ----------

        static readonly Color StreakOn = Fig.Hex("FF8A3D");

        void BuildServerGlobal(RectTransform root)
        {
            var content = ScrollArea(root, CloseArea + FooterHeight + 10f);
            var y = 4f;
            var rows = board?.Top ?? new List<LeaderboardEntryDto>();
            if (board == null || rows.Count == 0)
            {
                var text = board == null
                    ? (loadFailed ? "Can't load the leaderboard.\nCheck your connection and try again." : "Loading the leaderboard...")
                    : "No hunters on the board yet.\nCatch a meme and be the first!";
                var message = Fig.Body(content, text, 17f, Fig.MainText);
                message.alignment = TextAlignmentOptions.Center;
                message.rectTransform.At(Side, y + 40f, Width, 60f);
                y += 120f;
            }
            else
            {
                y = ServerPodium(content, rows, y) + 12f;
                foreach (var row in rows.Where(r => r.Position > 3))
                    y = ServerRow(content, row, y) + 8f;
            }
            content.sizeDelta = new Vector2(0f, y + 16f);
            ServerFooter(root);
        }

        float ServerPodium(RectTransform content, List<LeaderboardEntryDto> rows, float y)
        {
            const float height = 270f;                  // room for the crown above first place
            // Columns: 2nd (left), 1st (middle), 3rd (right). Block heights make the podium steps.
            var places = new (int index, float centreX, float avatar, float block, Color medal)[]
            {
                (1, 99f, 82f, 56f, Silver),
                (0, 249f, 102f, 82f, MemeHunterUiColors.LegendaryGold),
                (2, 399f, 82f, 40f, Bronze),
            };
            foreach (var place in places)
            {
                if (place.index >= rows.Count) continue;
                var row = rows[place.index];
                var me = IsMe(row);
                var blockTop = y + height - place.block;
                var blockWidth = place.index == 0 ? 136f : 126f;

                var block = Ui.Rounded("Step " + row.Position, content, place.medal.WithAlpha(0.55f), 16f);
                block.raycastTarget = false;
                block.rectTransform.At(place.centreX - blockWidth / 2f, blockTop, blockWidth, place.block);
                var number = Fig.Display(block.transform, row.Position.ToString(), place.index == 0 ? 40f : 32f, Color.white);
                number.alignment = TextAlignmentOptions.Center;
                number.rectTransform.At(0f, 0f, blockWidth, place.block);

                var xp = Fig.Display(content, row.Xp + " XP", 18f, Fig.MainText);
                xp.alignment = TextAlignmentOptions.Center;
                xp.rectTransform.At(place.centreX - 70f, blockTop - 25f, 140f, 22f);

                var name = Fig.Body(content, row.DisplayName, 15f, me ? PlayerBorder : Fig.MainText, TextWeight.Bold);
                name.alignment = TextAlignmentOptions.Center;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Ellipsis;
                name.rectTransform.At(place.centreX - 70f, blockTop - 47f, 140f, 22f);

                var avatarTop = blockTop - 52f - place.avatar;
                var avatar = Avatar(content, AvatarFor(row), place.avatar, place.medal, place.index == 0 ? 5f : 4f);
                avatar.At(place.centreX - place.avatar / 2f, avatarTop, place.avatar, place.avatar);

                // Place chip on the avatar's lower right.
                var chip = Ui.Rounded("Chip", content, place.medal, 14f);
                chip.raycastTarget = false;
                chip.rectTransform.At(place.centreX + place.avatar * 0.5f - 24f, avatarTop + place.avatar - 26f, 28f, 28f);
                var chipText = Fig.Display(chip.transform, row.Position.ToString(), 17f, Color.white);
                chipText.alignment = TextAlignmentOptions.Center;
                chipText.rectTransform.Stretch();

                if (place.index == 0) Crown(content, place.centreX, avatarTop - 6f);
            }
            return y + height;
        }

        float ServerRow(RectTransform content, LeaderboardEntryDto row, float y)
        {
            var me = IsMe(row);
            var rect = Row(content, me, y);

            var position = Fig.Display(rect, row.Position.ToString(), 22f, Fig.MainText);
            position.alignment = TextAlignmentOptions.Center;
            position.rectTransform.At(6f, 0f, 42f, 60f);

            Avatar(rect, AvatarFor(row), 42f, me ? PlayerBorder : Color.white, 3f).At(52f, 9f, 42f, 42f);
            NameAndLine(rect, row.DisplayName, $"{Ranks.Name(row.Rank)} · {CardsText(row.Cards)}{(me ? " · you" : string.Empty)}");

            var xp = Fig.Display(rect, row.Xp + " XP", 21f, Fig.MainText);
            xp.alignment = TextAlignmentOptions.MidlineRight;
            xp.rectTransform.At(300f, 0f, 146f, 60f);
            return y + 60f;
        }

        /// <summary>The player's own place, always visible above the close button, with the XP to the next rank.</summary>
        static void ServerFooter(RectTransform root)
        {
            var me = board?.Me;
            var card = Ui.BorderedCard("Your Rank", root, PlayerFill, PlayerBorder, 20f, 2.5f);
            card.AtBottom(Side, 1080f - CloseArea - FooterHeight, Width, FooterHeight);
            if (me == null)
            {
                var hint = Fig.Body(card, Session.HasPlayer ? "Your place appears after the board loads." : "Catch your first meme to join the board!",
                    15f, Fig.MainText, TextWeight.Bold);
                hint.alignment = TextAlignmentOptions.Center;
                hint.rectTransform.Stretch(12f);
                return;
            }

            var rank = Fig.Display(card, "#" + me.Position, 24f, Fig.MainText);
            rank.alignment = TextAlignmentOptions.Center;
            rank.enableAutoSizing = true;
            rank.fontSizeMin = 16f;
            rank.fontSizeMax = 24f;
            rank.rectTransform.At(6f, 0f, 50f, FooterHeight);
            Avatar(card, AvatarCatalog.SelectedIndex, 44f, PlayerBorder, 3f).At(58f, 11f, 44f, 44f);

            var name = Fig.Body(card, me.DisplayName, 16f, Fig.MainText, TextWeight.Bold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.rectTransform.At(112f, 10f, 200f, 22f);
            var line = Fig.Body(card, NextRankLine(Session.Player, me.Rank), 13f, PromoteInk, TextWeight.Bold);
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.rectTransform.At(112f, 34f, 210f, 20f);

            var xp = Fig.Display(card, me.Xp + " XP", 22f, Fig.MainText);
            xp.alignment = TextAlignmentOptions.MidlineRight;
            xp.rectTransform.At(316f, 0f, 132f, FooterHeight);
        }

        static void NameAndLine(RectTransform rect, string playerName, string detail)
        {
            var name = Fig.Body(rect, playerName, 17f, Fig.MainText, TextWeight.Bold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.rectTransform.At(106f, 9f, 190f, 24f);
            var line = Fig.Body(rect, detail, 13f, Muted);
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.rectTransform.At(106f, 33f, 190f, 18f);
        }

        static bool IsMe(LeaderboardEntryDto row) => board?.Me != null && row.Position == board.Me.Position;

        /// <summary>The server has no avatars: the player's own choice, and a face picked from the name for others.</summary>
        static int AvatarFor(LeaderboardEntryDto row)
        {
            if (IsMe(row)) return AvatarCatalog.SelectedIndex;
            var hash = 17;
            foreach (var c in row.DisplayName ?? string.Empty) hash = hash * 31 + c;
            return hash & 0x7fffffff;
        }

        static string CardsText(int cards) => cards == 1 ? "1 card" : cards + " cards";

        static string NextRankLine(PlayerDto player, string rankId)
        {
            if (player == null) return Ranks.Name(rankId);
            if (player.XpToNextRank == null) return Ranks.Name(player.Rank) + " · top rank";
            return $"{player.XpToNextRank} XP to {Ranks.NameOf(Ranks.Number(player.Rank) + 1)}";
        }

        // ---------- Server leaderboard: ME ----------

        void BuildServerMe(RectTransform root)
        {
            var content = ScrollArea(root, CloseArea);
            var player = Session.Player;
            var me = board?.Me;
            var y = 4f;

            // Player card.
            var card = Ui.Rounded("Player", content, CardFill, 26f);
            card.raycastTarget = false;
            card.rectTransform.At(Side, y, Width, 150f);
            Avatar(card.transform, AvatarCatalog.SelectedIndex, 112f, Fig.AvatarRing, 5f).At(18f, 19f, 112f, 112f);
            var playerName = player != null ? player.DisplayName : me != null ? me.DisplayName : PlayerPrefs.GetString("Nickname", "Hunter");
            var name = Fig.Display(card.transform, playerName, 32f, Fig.MainText);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.enableAutoSizing = true;
            name.fontSizeMin = 20f;
            name.fontSizeMax = 32f;
            name.rectTransform.At(148f, 22f, 300f, 40f);
            var rankId = player != null ? player.Rank : me?.Rank;
            var place = me != null ? "#" + me.Position : "NOT ON THE BOARD YET";
            var rankLine = Fig.Body(card.transform, $"{Ranks.Name(rankId).ToUpperInvariant()} · {place}", 15f, Fig.MainText, TextWeight.Bold);
            rankLine.Tracking(1.2f);
            rankLine.rectTransform.At(148f, 66f, 300f, 22f);
            var next = Fig.Body(card.transform, NextRankLine(player, rankId), 15f, Fig.MainText);
            next.rectTransform.At(148f, 92f, 300f, 22f);
            y += 162f;

            var cards = ServerProgress.Collection != null ? ServerProgress.Collection.Total : me?.Cards ?? 0;
            var xp = player != null ? player.Xp : me?.Xp ?? 0;
            y = Tiles(content, y, ("TOTAL XP", xp.ToString(), Fig.MainText), ("PLACE", me != null ? "#" + me.Position : "—", Fig.MainText),
                ("CARDS", cards.ToString(), Fig.MainText)) + 12f;
            y = RankGoal(content, y, player) + 12f;

            var streak = player != null ? player.CurrentStreak : 0;
            var lit = player != null && player.StreakDoneToday;
            y = Tiles(content, y, ("DAY STREAK", streak.ToString(), lit ? StreakOn : Fig.MainText),
                ("FREEZES", $"{(player != null ? player.StreakFreezeCount : 0)}/{BoostShop.MaxFreezes}", Fig.MainText),
                ("COINS", (player != null ? player.CurrencyBalance : 0).ToString(), Fig.MainText)) + 12f;

            y = Tiles(content, y, ("COMMON", ServerProgress.CountOfRarity("common").ToString(), TierInk(MemeRarity.Common)),
                ("RARE", ServerProgress.CountOfRarity("rare").ToString(), TierInk(MemeRarity.Rare)),
                ("EPIC", ServerProgress.CountOfRarity("epic").ToString(), TierInk(MemeRarity.Uncommon)),
                ("LEGEND", ServerProgress.CountOfRarity("legendary").ToString(), TierInk(MemeRarity.Legendary)));
            content.sizeDelta = new Vector2(0f, y + 20f);
        }

        /// <summary>How far the next rank is (style of the league goal card).</summary>
        static float RankGoal(RectTransform content, float y, PlayerDto player)
        {
            string message;
            float fill;
            if (player == null)
            {
                message = "Catch memes to earn XP and climb the ranks.";
                fill = 0f;
            }
            else if (player.XpToNextRank == null)
            {
                message = $"You reached the top rank, {Ranks.Name(player.Rank)}! Keep hunting to stay on top of the board.";
                fill = 1f;
            }
            else
            {
                var (current, needed) = Ranks.Progress(player);
                message = $"Earn {player.XpToNextRank} more XP to reach {Ranks.NameOf(Ranks.Number(player.Rank) + 1)}.";
                fill = Fraction(current, needed);
            }

            var card = Ui.Rounded("Rank Goal", content, CardFill, 24f);
            card.raycastTarget = false;
            card.rectTransform.At(Side, y, Width, 124f);
            var title = Fig.Body(card.transform, "NEXT RANK", 13f, Muted, TextWeight.Bold);
            title.Tracking(1.4f);
            title.rectTransform.At(20f, 14f, 300f, 18f);
            var text = Fig.Body(card.transform, message, 16f, Fig.MainText);
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 16f;
            text.rectTransform.At(20f, 36f, Width - 40f, 46f);

            var track = Ui.Rounded("Track", card.transform, Fig.XpTrack.WithAlpha(0.45f), 6f);
            track.raycastTarget = false;
            track.rectTransform.At(20f, 94f, Width - 40f, 12f);
            var bar = Ui.Rounded("Fill", card.transform, Color.white, 6f);
            bar.raycastTarget = false;
            bar.gameObject.AddComponent<HorizontalGradientEffect>().SetColors(Fig.XpFrom, Fig.XpTo);
            bar.rectTransform.At(20f, 94f, Mathf.Max(12f, (Width - 40f) * fill), 12f);
            return y + 124f;
        }

        // ---------- Shared pieces ----------

        /// <summary>A vertical scroll area from below the switch down to `bottom` (from the screen's bottom edge).</summary>
        static RectTransform ScrollArea(RectTransform root, float bottom)
        {
            var viewport = Ui.Rect("Viewport", root);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(0f, bottom);
            viewport.offsetMax = new Vector2(0f, -ContentTop);
            viewport.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 24);
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // empty areas can be dragged

            var content = Ui.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            return content;
        }

        /// <summary>Round avatar: ring -> masked light-blue circle -> face (like the Profile avatar).</summary>
        static RectTransform Avatar(Transform parent, int index, float size, Color ring, float ringWidth)
        {
            var outer = Ui.Rounded("Avatar", parent, ring, size / 2f);
            outer.raycastTarget = false;
            var circle = Ui.Rounded("Circle", outer.transform, new Color32(168, 216, 255, 255), size / 2f - ringWidth);
            circle.raycastTarget = false;
            circle.rectTransform.Stretch(ringWidth);
            circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            var all = AvatarCatalog.All;
            var sprite = all.Length > 0 && index >= 0 ? all[index % all.Length] : null;
            var face = Ui.Image("Face", circle.transform, sprite);
            face.rectTransform.Stretch();
            // The Figma avatars have margins: enlarge and lower the face so it fills the circle (as on Profile).
            face.rectTransform.offsetMin = new Vector2(-0.11f * size, -0.14f * size);
            face.rectTransform.offsetMax = new Vector2(0.11f * size, 0.065f * size);
            face.enabled = sprite != null;
            return outer.rectTransform;
        }

        /// <summary>A league badge: the glowing rarity circle from Collections, with the trophy on the current league.</summary>
        static RectTransform LeagueBadge(Transform parent, MemeRarity tier, float size, bool withTrophy)
        {
            var theme = UiTheme.Get();
            var root = Ui.Rect("League " + tier, parent);
            // The rare / legendary sprites include a wider glow: drawn larger around the same centre (as in Collections).
            var glowScale = tier == MemeRarity.Rare ? 1.138f : tier == MemeRarity.Legendary ? 1.276f : 1f;
            var sprite = theme == null ? null : tier switch
            {
                MemeRarity.Uncommon => theme.rarityUncommon,
                MemeRarity.Rare => theme.rarityRare,
                MemeRarity.Legendary => theme.rarityLegendary,
                _ => theme.rarityCommon
            };
            var circle = Ui.Image("Circle", root, sprite);
            circle.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * glowScale);
            if (sprite == null) circle.color = Rarity.ColorOf(tier);
            if (withTrophy && theme != null && theme.trophyGlyph != null)
            {
                var trophy = Ui.Image("Trophy", root, theme.trophyGlyph);
                trophy.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.56f);
                // White reads only on the dark purple (Rare); the grey, green and gold circles get a navy trophy.
                if (tier != MemeRarity.Rare) trophy.color = Fig.MainText.WithAlpha(0.85f);
            }
            return root;
        }

        /// <summary>A small gold crown over first place: a band and three points.</summary>
        static void Crown(RectTransform content, float centreX, float bottomY)
        {
            var gold = MemeHunterUiColors.LegendaryGold;
            var band = Ui.Rounded("Crown", content, gold, 3f);
            band.raycastTarget = false;
            band.rectTransform.At(centreX - 20f, bottomY - 10f, 40f, 10f);
            for (var i = -1; i <= 1; i++)
            {
                var point = Ui.Rect("Point", band.transform).gameObject.AddComponent<TriangleGraphic>();
                point.color = gold;
                point.raycastTarget = false;
                var size = i == 0 ? new Vector2(16f, 20f) : new Vector2(14f, 16f);
                point.rectTransform.anchorMin = point.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                point.rectTransform.pivot = new Vector2(0.5f, 0f);
                point.rectTransform.anchoredPosition = new Vector2(i * 13f, -3f);
                point.rectTransform.sizeDelta = size;
            }
        }

        /// <summary>When a week has ended since the last visit: one popup with the result (only once Leaderboard is on screen).</summary>
        System.Collections.IEnumerator ShowWeekResultWhenShown()
        {
            yield return null;
            if (!isActiveAndEnabled) yield break;
            var result = LocalLeague.TakeUnseenResult();
            if (result == null) yield break;

            var moved = result.To > result.From ? $"You moved up to the {LeagueName(result.To)}!"
                : result.To < result.From ? $"You dropped to the {LeagueName(result.To)}. Catch memes this week to climb back!"
                : $"You stay in the {LeagueName(result.To)}.";
            Popup.Show("Week finished!", $"You finished #{result.Rank} with {result.Xp} XP. {moved}");
        }

        public static string LeagueName(MemeRarity tier) => tier + " League";

        /// <summary>The rarity colour, darkened where it is too light to read on the light cyan background.</summary>
        static Color TierInk(MemeRarity tier) => tier switch
        {
            MemeRarity.Common => Fig.Hex("8A92A8"),
            MemeRarity.Uncommon => Fig.Hex("2FAE4E"),
            MemeRarity.Legendary => Fig.Hex("E2A200"),
            _ => Rarity.ColorOf(tier)
        };

        static string TimeLeft(DateTime end)
        {
            var left = end - DateTime.Now;
            if (left.TotalMinutes < 1) return "1m";
            if (left.TotalDays >= 1) return $"{(int)left.TotalDays}d {left.Hours}h";
            if (left.TotalHours >= 1) return $"{(int)left.TotalHours}h {left.Minutes}m";
            return $"{left.Minutes}m";
        }
    }
}
