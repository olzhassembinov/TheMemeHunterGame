using System;
using System.Collections;
using MemeHunter.Game;
using MemeHunter.Net;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;

namespace MemeHunter.UI.Hunt
{
    /// <summary>
    /// "Meme caught!" popup with a card-flip reveal: the card back (navy, MH) turns over to the meme card
    /// (art, name, rarity in its colour, glow in the rarity colour), then the coin / XP rewards.
    /// Rewards are instant: never "pending". Real catches show what the server credited (ShowServer):
    /// coins, XP with the booster, the daily streak and a rank-up.
    /// </summary>
    public static class CatchReveal
    {
        const float CardWidth = 220f;
        const float CardHeight = 300f;

        /// <summary>A real catch: the rewards come from the submission answer; `before` is the profile before the catch.</summary>
        public static Popup ShowServer(string cardId, string serverRarity, SubmissionResultDto result, PlayerDto before, Action onClosed = null)
        {
            var rewards = $"+{result.Reward} coins   +{result.Xp} XP";
            if (before != null && before.BoosterActive) rewards += $" (x{before.Booster.Multiplier} boost)";
            if (result.Streak > 0)
            {
                var days = result.Streak == 1 ? "1 day" : result.Streak + " days";
                rewards += before != null && !before.StreakDoneToday ? $"\nStreak: {days}! Come back tomorrow." : $"\nStreak: {days}";
            }
            if (before != null)
            {
                var newRank = Ranks.NumberForXp(before.Xp + result.Xp);
                if (newRank > Ranks.Number(before.Rank)) rewards += $"\nRank up! New rank: {Ranks.NameOf(newRank)}.";
            }
            return Build(MemeCatalog.ForCard(cardId, serverRarity), serverRarity, rewards, onClosed);
        }

        /// <summary>The DEV test meme and the editor previews: rewards counted on the phone.</summary>
        public static Popup Show(PlayerProgress.CatchResult result, string serverRarity, int coins, Action onClosed = null)
        {
            var rewards = $"+{coins} coins   +{result.XpGained} XP";
            if (result.IsDuplicate) rewards += "\nYou already had it: turned into coins!";
            if (result.LeveledUp) rewards += $"\nLevel up! You are now level {PlayerProgress.Level}.";
            return Build(result.Meme, serverRarity, rewards, onClosed);
        }

        static Popup Build(MemeData meme, string serverRarity, string rewards, Action onClosed)
        {
            var rarity = Rarity.Parse(serverRarity);
            var color = Rarity.ColorOf(rarity);

            var popup = Popup.Create("Meme caught!", rewards, CardHeight + 30f, onClosed);
            popup.Content.SetSiblingIndex(popup.Body.transform.GetSiblingIndex());   // the card first, rewards under it

            // Glow (rarity colour) behind the card.
            var stage = Ui.Rect("Stage", popup.Content).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardWidth, CardHeight));
            var glow = Ui.Rounded("Glow", stage, new Color(color.r, color.g, color.b, 0.55f), 34f);
            glow.raycastTarget = false;
            glow.rectTransform.Stretch(-10f);

            // Front: the meme card (Figma collection card style).
            var front = Ui.BorderedCard("Front", stage, new Color32(168, 216, 255, 255), color, 26f, 4f);
            front.Stretch();
            var art = Ui.CroppedPicture("Art", front, meme != null ? meme.Artwork : null, 16f);
            art.Place(new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(CardWidth - 28f, CardHeight * 0.58f));
            var title = Ui.Text("Name", front, meme != null ? meme.DisplayName : "MEME", 26f,
                MemeHunterUiColors.DarkNavy, display: true);
            title.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(CardWidth - 20f, 40f));
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 16f;
            title.fontSizeMax = 26f;
            var rarityText = Ui.Text("Rarity", front, Rarity.Name(rarity).ToUpperInvariant(), 18f, color, display: true);
            rarityText.characterSpacing = 18f;
            rarityText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(CardWidth - 20f, 30f));

            // Back: navy with the MH logo text, shown first.
            var back = Ui.BorderedCard("Back", stage, MemeHunterUiColors.DarkNavy, color, 26f, 4f);
            back.Stretch();
            var logo = Ui.Text("Logo", back, "MH", 72f, MemeHunterUiColors.Lime, display: true);
            logo.rectTransform.Stretch();

            popup.Body.alpha = 0f;                                   // rewards fade in after the flip
            popup.StartCoroutine(Flip(stage, front.gameObject, back.gameObject, popup.Body, glow));
            return popup;
        }

        static IEnumerator Flip(RectTransform stage, GameObject front, GameObject back, TextMeshProUGUI rewards, RoundedRectGraphic glow)
        {
            front.SetActive(false);
            back.SetActive(true);
            yield return new WaitForSecondsRealtime(0.35f);

            const float half = 0.22f;
            for (var t = 0f; t < half; t += Time.unscaledDeltaTime)          // back turns away
            {
                stage.localScale = new Vector3(Mathf.Lerp(1f, 0f, t / half), 1f, 1f);
                yield return null;
            }
            back.SetActive(false);
            front.SetActive(true);
            for (var t = 0f; t < half; t += Time.unscaledDeltaTime)          // front turns in with a small overshoot
            {
                var k = t / half;
                stage.localScale = new Vector3(Mathf.Lerp(0f, 1.06f, k), 1f + 0.06f * Mathf.Sin(k * Mathf.PI), 1f);
                yield return null;
            }
            stage.localScale = Vector3.one;

            for (var t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                rewards.alpha = t / 0.3f;
                yield return null;
            }
            rewards.alpha = 1f;

            // Gentle glow pulse while the popup is open.
            var baseColor = glow.color;
            while (glow != null)
            {
                var a = 0.4f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
                glow.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
        }
    }
}
