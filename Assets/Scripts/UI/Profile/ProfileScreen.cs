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
    /// Figma "Profile" (158:15), without the I AM / MY FRIENDS / GLOBAL tabs (no social features):
    /// avatar circle, nickname, age + city, rank, XP bar, coins, daily streak and streak freezes, the XP booster
    /// with its timer and the BOOSTS shop button, BEST MEME CARDS (2x2), close button.
    /// Rank, XP, coins, streak and booster come from the server profile; the cards from the server collection.
    /// </summary>
    public sealed class ProfileScreen : FigmaScreen
    {
        static readonly Color StreakOn = Fig.Hex("FF8A3D");     // today's photo is done: the flame is lit
        static readonly Color StreakOff = Fig.SubText;          // grey until today's photo
        static readonly Color FreezeFill = Fig.AvatarRing;
        static readonly Color Muted = Fig.MainText.WithAlpha(0.6f);

        bool refreshedThisVisit;                                // one server refresh each time the page is opened
        TextMeshProUGUI boosterText;

        protected override void Build(RectTransform root)
        {
            var player = Session.Player;
            BuildAvatar(root);

            var nickname = Fig.Display(root, Nickname(player), 40f, Fig.MainText);
            nickname.alignment = TextAlignmentOptions.MidlineLeft;
            nickname.enableAutoSizing = true;
            nickname.fontSizeMin = 22f;
            nickname.fontSizeMax = 40f;
            nickname.rectTransform.At(225f, 78f, 258f, 42f);

            var subtitle = Fig.Body(root, Subtitle(), 16f, Fig.MainText, TextWeight.Light);
            subtitle.alignment = TextAlignmentOptions.MidlineLeft;
            subtitle.rectTransform.At(225f, 127f, 258f, 24f);

            // Rank: big Jaro number + the rank name (the server sends rank_1 .. rank_5, names in Ranks).
            var rankId = player != null ? player.Rank : null;
            var level = Fig.Display(root, Ranks.Number(rankId).ToString(), 65f, Fig.MainText);
            level.alignment = TextAlignmentOptions.MidlineLeft;
            level.rectTransform.At(225f, 145f, 60f, 72f);
            var levelLabel = Fig.Body(root, Ranks.Name(rankId).ToUpperInvariant(), 15f, Fig.MainText, TextWeight.Light);
            levelLabel.alignment = TextAlignmentOptions.MidlineLeft;
            levelLabel.enableAutoSizing = true;
            levelLabel.fontSizeMin = 11f;
            levelLabel.fontSizeMax = 15f;
            levelLabel.rectTransform.At(283f, 186f, 100f, 20f);

            // XP bar: teal track 199x9, blue gradient fill. XP inside the current rank.
            var (current, needed) = Ranks.Progress(player);
            var topRank = player != null && player.XpToNextRank == null;
            var track = Ui.Rounded("XP Track", root, Fig.XpTrack, 4.5f);
            track.raycastTarget = false;
            track.rectTransform.At(283f, 162f, 199f, 9f);
            var fill = Ui.Rounded("XP Fill", root, Color.white, 4.5f);
            fill.raycastTarget = false;
            fill.gameObject.AddComponent<HorizontalGradientEffect>().SetColors(Fig.XpFrom, Fig.XpTo);
            fill.rectTransform.At(283f, 162f, Mathf.Max(9f, 199f * current / Mathf.Max(1, needed)), 9f);

            var xpLine = topRank ? $"{player.Xp} XP" : $"{current}<color=#A6AED7>/{needed}</color>";
            var xp = Fig.Body(root, xpLine, 18f, Fig.MainText, TextWeight.Bold);
            xp.alignment = TextAlignmentOptions.MidlineRight;
            xp.enableAutoSizing = true;
            xp.fontSizeMin = 12f;
            xp.fontSizeMax = 18f;
            xp.rectTransform.At(383f, 184f, 99f, 20f);

            // Coins (Score = coins) with the coin icon.
            var coinsText = Fig.Display(root, player != null ? player.CurrencyBalance.ToString() : "0", 18f, Fig.MainText);
            coinsText.alignment = TextAlignmentOptions.MidlineRight;
            coinsText.rectTransform.At(330f, 219f, 118.7f, 20f);
            var theme = UiTheme.Get();
            Ui.Image("Coin", root, theme != null ? theme.coinIcon : null).rectTransform.At(462.2f, 219f, 19.4f, 19.4f);

            BuildStreak(root, player);
            BuildBooster(root, player);

            // BEST MEME CARDS header with its two lines.
            Fig.Line(root, 18f, 321f, 138f, Fig.SubText);
            Fig.Line(root, 342f, 321f, 139f, Fig.SubText);
            var header = Fig.Body(root, "BEST MEME CARDS", 15f, Fig.SubText, TextWeight.Bold);
            header.alignment = TextAlignmentOptions.Center;
            header.Tracking(1.65f);
            header.rectTransform.At(146f, 312f, 206f, 18f);

            // 2x2 grid at (37, 359): columns 42 px apart, rows 35 px apart.
            var best = ProfileSection.CatalogRarestFirst().Where(m => ProfileSection.Count(m) > 0).Take(4).ToList();
            for (var i = 0; i < 4; i++)
            {
                var x = 37f + (i % 2) * (MemeTile.Width + 42f);
                var y = 359f + (i / 2) * (MemeTile.Height + 35f);
                RectTransform slot;
                if (i < best.Count)
                {
                    var meme = best[i];
                    slot = MemeTile.Card(root, meme, true, ProfileSection.Count(meme), () => MemeCardScreen.Open(meme));
                }
                else
                {
                    slot = MemeTile.Empty(root, () => AppNav.OpenScreen(AppNav.Collections));
                }
                slot.At(x, y, MemeTile.Width, MemeTile.Height);
            }

            CloseButton(root);

            if (!Application.isPlaying) return;
            Session.PlayerChanged -= OnPlayerChanged;
            Session.PlayerChanged += OnPlayerChanged;
            ServerProgress.Changed -= OnCollectionChanged;
            ServerProgress.Changed += OnCollectionChanged;
            if (!refreshedThisVisit)
            {
                refreshedThisVisit = true;
                RefreshFromServer();
                if (!AvatarCatalog.HasChoice) StartCoroutine(AskForAvatarWhenShown());
            }
        }

        // ---------- Streak and booster (new: server profile) ----------

        /// <summary>Daily streak pill (orange once today's photo is done, grey until then) and the freezes held.</summary>
        void BuildStreak(RectTransform root, PlayerDto player)
        {
            var streak = player != null ? player.CurrentStreak : 0;
            var done = player != null && player.StreakDoneToday;
            var label = streak > 0 ? $"{streak} DAY STREAK" : "NO STREAK YET";
            var pill = Pill(root, "Streak", done ? StreakOn : StreakOff, label);
            pill.At(225f, 246f, 132f, 28f);

            var freezes = player != null ? player.StreakFreezeCount : 0;
            var freeze = Pill(root, "Freezes", FreezeFill, $"FREEZE {freezes}/{BoostShop.MaxFreezes}");
            freeze.At(363f, 246f, 119f, 28f);
        }

        /// <summary>The active XP booster with its countdown, and the BOOSTS button that opens the shop.</summary>
        void BuildBooster(RectTransform root, PlayerDto player)
        {
            boosterText = Fig.Body(root, BoosterLine(player), 15f, Muted, TextWeight.Bold);
            boosterText.alignment = TextAlignmentOptions.MidlineLeft;
            boosterText.enableAutoSizing = true;
            boosterText.fontSizeMin = 11f;
            boosterText.fontSizeMax = 15f;
            boosterText.rectTransform.At(18f, 281f, 340f, 24f);

            var shop = Ui.PillButton("Boosts", root, "BOOSTS", BoostShop.Open, 17f);
            ((RectTransform)shop.transform).At(370f, 278f, 112f, 30f);
        }

        RectTransform Pill(RectTransform root, string name, Color color, string label)
        {
            var pill = Ui.Rounded(name, root, color, 14f);
            var button = pill.gameObject.AddComponent<Button>();
            button.targetGraphic = pill;
            button.onClick.AddListener(BoostShop.Open);                  // streak and freezes are bought in the shop
            var text = Fig.Body(pill.transform, label, 13f, Color.white, TextWeight.Bold);
            text.alignment = TextAlignmentOptions.Center;
            text.Tracking(0.8f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.Stretch(4f);
            return pill.rectTransform;
        }

        static string BoosterLine(PlayerDto player)
        {
            if (player == null || !player.BoosterActive) return "No XP boost active";
            var left = System.TimeSpan.FromSeconds(player.BoosterSecondsLeft);
            return $"<color=#0099FF>XP BOOST x{player.Booster.Multiplier}</color> · {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00} left";
        }

        void Update()
        {
            // The booster countdown ticks on the phone (from the server's seconds_left).
            if (boosterText != null && Session.Player != null && Session.Player.BoosterActive)
                boosterText.text = BoosterLine(Session.Player);
        }

        // ---------- Server data ----------

        async void RefreshFromServer()
        {
            await ServerProgress.RefreshPlayer();                         // coins, rank, XP, streak, booster
            await ServerProgress.Refresh();                               // the caught cards (BEST MEME CARDS)
        }

        void OnPlayerChanged(PlayerDto _) { if (isActiveAndEnabled) Render(); }
        void OnCollectionChanged() { if (isActiveAndEnabled) Render(); }

        void OnDisable()
        {
            Session.PlayerChanged -= OnPlayerChanged;
            ServerProgress.Changed -= OnCollectionChanged;
            refreshedThisVisit = false;
            AvatarPicker.Close();                                           // leaving Profile: no picker over another page
        }

        /// <summary>
        /// First visit: ask for a face, but only once Profile is really on screen. When the scene loads, every page is
        /// briefly active before the navigator picks one; hiding Profile in that frame stops this coroutine.
        /// </summary>
        System.Collections.IEnumerator AskForAvatarWhenShown()
        {
            yield return null;
            if (isActiveAndEnabled && !AvatarCatalog.HasChoice) AvatarPicker.Open(Rebuild);
        }

        void Rebuild()
        {
            gameObject.SetActive(false);                                    // OnEnable rebuilds with fresh data
            gameObject.SetActive(true);
        }

        void BuildAvatar(RectTransform root)
        {
            // Figma: 183 px circle with a ~5 px #7BB8FF ring (193 px overall) at (17, 86).
            var ring = Ui.Rounded("Avatar Ring", root, Fig.AvatarRing, 96.5f);
            ring.rectTransform.At(12f, 81f, 193f, 193f);
            var circle = Ui.Rounded("Avatar", ring.transform, new Color32(168, 216, 255, 255), 91.5f);
            circle.rectTransform.Stretch(5f);
            circle.raycastTarget = false;
            circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var face = Ui.Image("Face", circle.transform, AvatarCatalog.Selected);
            face.rectTransform.Stretch();
            face.rectTransform.offsetMin = new Vector2(-20f, -26f);       // the Figma avatars have margins: fill the circle
            face.rectTransform.offsetMax = new Vector2(20f, 12f);
            face.enabled = face.sprite != null;

            var button = ring.gameObject.AddComponent<Button>();
            button.targetGraphic = ring;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => AvatarPicker.Open(Rebuild));
        }

        static string Nickname(PlayerDto player) =>
            player != null && !string.IsNullOrEmpty(player.DisplayName) ? player.DisplayName : PlayerPrefs.GetString("Nickname", "Hunter");

        static string Subtitle()
        {
            if (!PlayerPrefs.HasKey("Year")) return PlayerPrefs.GetString("City", string.Empty);
            var today = System.DateTime.Today;
            var age = today.Year - PlayerPrefs.GetInt("Year");
            var month = PlayerPrefs.GetInt("Month", 1);
            if (today.Month < month || (today.Month == month && today.Day < PlayerPrefs.GetInt("Day", 1))) age--;
            return $"Age {Mathf.Max(0, age)}, {PlayerPrefs.GetString("City", string.Empty)}";
        }
    }
}
