using System;
using MemeHunter.Net;
using MemeHunter.UI.Hunt;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Profile
{
    /// <summary>
    /// The BOOSTS popup (Figma popup card style): spend coins on an XP booster (x2 / x5 for 24 hours) or a streak
    /// freeze. The server checks the coins and answers with the updated profile; the prices below are shown only
    /// and must match app/progression.py and app/streak.py on the backend.
    /// </summary>
    public static class BoostShop
    {
        public const int BoosterX2Price = 50;
        public const int BoosterX5Price = 150;
        public const int FreezePrice = 40;
        public const int MaxFreezes = 2;

        const float RowWidth = 386f;          // popup card 450 minus its padding
        const float RowHeight = 72f;
        const float RowGap = 10f;

        static Popup open;
        static bool buying;

        public static void Open()
        {
            if (open != null) return;
            var player = Session.Player;
            var coins = player != null ? player.CurrencyBalance : 0;
            var body = $"You have {coins} coins.";
            if (player != null && player.BoosterActive)
                body += $"\nXP x{player.Booster.Multiplier} is on: a new boost replaces it.";

            open = Popup.Create("Boosts", body, 3 * RowHeight + 2 * RowGap, () => open = null);
            var content = open.Content;
            var freezes = player != null ? player.StreakFreezeCount : 0;

            Offer(content, 0, "XP BOOST x2", "Double XP for 24 hours", BoosterX2Price, true, () => BuyBooster(2, BoosterX2Price));
            Offer(content, 1, "XP BOOST x5", "Five times the XP for 24 hours", BoosterX5Price, true, () => BuyBooster(5, BoosterX5Price));
            Offer(content, 2, "STREAK FREEZE", $"Saves your streak on a missed day · {freezes}/{MaxFreezes}", FreezePrice,
                freezes < MaxFreezes, BuyFreeze);
        }

        static void Offer(RectTransform content, int index, string title, string subtitle, int price, bool available, Action buy)
        {
            var row = Ui.BorderedCard(title, content, new Color(1f, 1f, 1f, 0.7f), Ui.PopupBorder, 20f, 2f);
            row.Place(new Vector2(0.5f, 1f), new Vector2(0f, -index * (RowHeight + RowGap)), new Vector2(RowWidth, RowHeight));

            var name = Fig.Display(row, title, 20f, Fig.MainText);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.rectTransform.Place(new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(236f, 26f));
            var line = Fig.Body(row, subtitle, 13f, Fig.MainText.WithAlpha(0.7f));
            line.alignment = TextAlignmentOptions.MidlineLeft;
            line.enableAutoSizing = true;
            line.fontSizeMin = 10f;
            line.fontSizeMax = 13f;
            line.rectTransform.Place(new Vector2(0f, 1f), new Vector2(16f, -38f), new Vector2(236f, 24f));

            var button = Ui.PillButton("Buy", row, available ? $"{price} COINS" : "MAX", () => buy(), 16f);
            ((RectTransform)button.transform).Place(new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(118f, 46f));
            button.interactable = available;
            if (!available) button.targetGraphic.color = new Color(1f, 1f, 1f, 0.45f);
        }

        static async void BuyBooster(int multiplier, int price)
        {
            if (!CanBuy(price)) return;
            await Buy(() => Api.BuyXpBooster(Session.PlayerId, multiplier),
                $"XP x{multiplier} is on!", $"Every meme you catch in the next 24 hours gives {multiplier} times the XP.");
        }

        static async void BuyFreeze()
        {
            if (!CanBuy(FreezePrice)) return;
            await Buy(() => Api.BuyStreakFreeze(Session.PlayerId),
                "Streak freeze ready", "If you miss a day, a freeze is used and your streak keeps going.");
        }

        /// <summary>Quick check on the phone so a poor player gets the answer without waiting for the server.</summary>
        static bool CanBuy(int price)
        {
            if (buying) return false;
            if (!Session.HasPlayer)
            {
                Popup.Show("Catch a meme first", "Your hunter profile is created with your first hunt.");
                return false;
            }
            if (Session.Player != null && Session.Player.CurrencyBalance < price)
            {
                NotEnoughCoins();
                return false;
            }
            return true;
        }

        static async Awaitable Buy(Func<Awaitable<PlayerDto>> call, string doneTitle, string doneText)
        {
            buying = true;
            try
            {
                var updated = await call();
                Session.SetPlayer(updated);                              // the profile page redraws itself
                Close();
                Popup.Show(doneTitle, doneText);
            }
            catch (ApiException e)
            {
                if (e.Message.IndexOf("Not enough coins", StringComparison.OrdinalIgnoreCase) >= 0) NotEnoughCoins();
                else if (e.Message.IndexOf("Freeze limit", StringComparison.OrdinalIgnoreCase) >= 0)
                    Popup.Show("Freezes are full", $"You can hold {MaxFreezes} streak freezes at a time.");
                else HuntErrors.From(e);
            }
            finally
            {
                buying = false;
            }
        }

        static void NotEnoughCoins() =>
            Popup.Show("Not enough coins", "Catch more memes to earn coins, then come back for a boost.");

        static void Close()
        {
            if (open != null) open.Close();
            open = null;
        }
    }
}
