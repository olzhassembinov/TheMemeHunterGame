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
    /// avatar circle, nickname, age + city, level, XP bar, coins, BEST MEME CARDS (2x2), close button.
    /// </summary>
    public sealed class ProfileScreen : FigmaScreen
    {
        TextMeshProUGUI coinsText;

        protected override void Build(RectTransform root)
        {
            BuildAvatar(root);

            var nickname = Fig.Display(root, PlayerPrefs.GetString("Nickname", "Hunter"), 40f, Fig.MainText);
            nickname.alignment = TextAlignmentOptions.MidlineLeft;
            nickname.enableAutoSizing = true;
            nickname.fontSizeMin = 22f;
            nickname.fontSizeMax = 40f;
            nickname.rectTransform.At(225f, 78f, 258f, 42f);

            var subtitle = Fig.Body(root, Subtitle(), 16f, Fig.MainText, TextWeight.Light);
            subtitle.alignment = TextAlignmentOptions.MidlineLeft;
            subtitle.rectTransform.At(225f, 127f, 258f, 24f);

            // Level: big Jaro number + "LEVEL".
            var level = Fig.Display(root, PlayerProgress.Level.ToString(), 65f, Fig.MainText);
            level.alignment = TextAlignmentOptions.MidlineLeft;
            level.rectTransform.At(225f, 145f, 60f, 72f);
            var levelLabel = Fig.Body(root, "LEVEL", 15f, Fig.MainText, TextWeight.Light);
            levelLabel.alignment = TextAlignmentOptions.Center;
            levelLabel.rectTransform.At(283f, 186f, 49f, 20f);

            // XP bar: teal track 199x9, blue gradient fill.
            var (current, needed) = PlayerProgress.LevelProgress();
            var track = Ui.Rounded("XP Track", root, Fig.XpTrack, 4.5f);
            track.raycastTarget = false;
            track.rectTransform.At(283f, 162f, 199f, 9f);
            var fill = Ui.Rounded("XP Fill", root, Color.white, 4.5f);
            fill.raycastTarget = false;
            fill.gameObject.AddComponent<HorizontalGradientEffect>().SetColors(Fig.XpFrom, Fig.XpTo);
            fill.rectTransform.At(283f, 162f, Mathf.Max(9f, 199f * current / Mathf.Max(1, needed)), 9f);

            var xp = Fig.Body(root, $"{current}<color=#A6AED7>/{needed}</color>", 18f, Fig.MainText, TextWeight.Bold);
            xp.alignment = TextAlignmentOptions.MidlineRight;
            xp.rectTransform.At(331f, 184f, 150f, 20f);

            // Coins (Score = coins) with the coin icon.
            coinsText = Fig.Display(root, Coins(), 18f, Fig.MainText);
            coinsText.alignment = TextAlignmentOptions.MidlineRight;
            coinsText.rectTransform.At(330f, 219f, 118.7f, 20f);
            var theme = UiTheme.Get();
            Ui.Image("Coin", root, theme != null ? theme.coinIcon : null).rectTransform.At(462.2f, 219f, 19.4f, 19.4f);

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

            Session.PlayerChanged -= OnPlayerChanged;
            Session.PlayerChanged += OnPlayerChanged;
            if (Application.isPlaying) RefreshCoins();
            if (Application.isPlaying && !AvatarCatalog.HasChoice) AvatarPicker.Open(() => { if (this != null && isActiveAndEnabled) Rebuild(); });
        }

        void OnDisable() => Session.PlayerChanged -= OnPlayerChanged;
        void OnPlayerChanged(PlayerDto _) { if (coinsText != null) coinsText.text = Coins(); }

        void Rebuild()
        {
            gameObject.SetActive(false);                                    // OnEnable rebuilds with fresh data
            gameObject.SetActive(true);
        }

        async void RefreshCoins()
        {
            if (!Session.HasPlayer) return;
            try { Session.SetPlayer(await Api.GetPlayer(Session.PlayerId)); }
            catch (ApiException) { /* offline: keep the last known balance */ }
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

        static string Subtitle()
        {
            if (!PlayerPrefs.HasKey("Year")) return PlayerPrefs.GetString("City", string.Empty);
            var today = System.DateTime.Today;
            var age = today.Year - PlayerPrefs.GetInt("Year");
            var month = PlayerPrefs.GetInt("Month", 1);
            if (today.Month < month || (today.Month == month && today.Day < PlayerPrefs.GetInt("Day", 1))) age--;
            return $"Age {Mathf.Max(0, age)}, {PlayerPrefs.GetString("City", string.Empty)}";
        }

        static string Coins() => Session.Player != null ? Session.Player.CurrencyBalance.ToString() : "0";
    }
}
