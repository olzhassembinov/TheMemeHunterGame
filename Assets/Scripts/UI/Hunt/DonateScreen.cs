using MemeHunter.Net;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;

namespace MemeHunter.UI
{
    /// <summary>
    /// Figma "Donating" section: the Donate screen (363:89) and the Thank-you screen (356:141), full screen,
    /// lavender background, white text, a heart, $1 / $2 / $5 pills and the round close button.
    /// </summary>
    public static class DonateScreen
    {
        static readonly Color Background = MemeHunterUiColors.LightPurple;   // #A6AED7
        static readonly Color PillText = new Color32(17, 17, 17, 255);

        public static void Open()
        {
            var screen = BuildScreen("Donate");
            var canvas = screen.canvas;

            Title(canvas, "Support people with disabilities!", -160f);
            Body(canvas, "Thank you for using this app and contributing to society! By donating, you help people " +
                         "facing disadvantages. Even a small donation supports individuals with impairments.", -270f, 150f);

            var heart = Shapes.Heart("Heart", canvas, new Color(0.69f, 0.66f, 0.85f, 1f), 150f);
            heart.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(150f, 150f));

            var amounts = new[] { 1, 2, 5 };
            for (var i = 0; i < amounts.Length; i++)
            {
                var amount = amounts[i];
                var label = amount == 1 ? "1 dollar" : amount + " dollars";
                var pill = Ui.Rounded("Donate " + amount, canvas, Color.white, 34f);
                pill.gameObject.AddComponent<HorizontalGradientEffect>().SetColors(Color.white, new Color32(200, 222, 255, 255));
                pill.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 380f - i * 100f), new Vector2(320f, 70f));
                var text = Ui.Text("Label", pill.transform, label, 22f, PillText);
                text.rectTransform.Stretch();
                var button = pill.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = pill;
                button.onClick.AddListener(() => Donate(amount, screen.root));
            }
        }

        static void Donate(int amount, GameObject donateScreen)
        {
            if (string.IsNullOrEmpty(ApiConfig.DonateUrl))
            {
                // No payment page yet: say so honestly instead of showing a fake "thank you".
                Popup.Show("Coming soon", "Donations will open soon. Thank you for wanting to help!");
                return;
            }

            Application.OpenURL(ApiConfig.DonateUrl + "?amount=" + amount);
            Object.Destroy(donateScreen);
            OpenThanks();
        }

        public static void OpenThanks()
        {
            var canvas = BuildScreen("Thanks").canvas;
            Title(canvas, "Thank you for donating!", -300f);
            var heart = Shapes.Heart("Heart", canvas, new Color32(255, 48, 82, 255), 160f);
            heart.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(160f, 160f));
            Body(canvas, "This project was made by just two fellows inspired to improve the world for people with " +
                         "disabilities. Your donation supports them and shows there is still hope for humanity. Thank you!",
                -640f, 190f);
        }

        static (GameObject root, RectTransform canvas) BuildScreen(string name)
        {
            var canvas = Ui.OverlayCanvas(name + " Screen", 800);
            var bg = Ui.Rounded("Background", canvas, Background, 0f);   // also blocks taps on what is behind
            bg.rectTransform.Stretch();

            var theme = UiTheme.Get();
            var close = Ui.IconButton("Close", canvas, theme != null ? theme.discardButton : null,
                () => Object.Destroy(canvas.gameObject));
            ((RectTransform)close.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(64f, 64f));
            return (canvas.gameObject, canvas);
        }

        static void Title(RectTransform canvas, string value, float top)
        {
            var title = Ui.Text("Title", canvas, value, 34f, Color.white);
            title.fontStyle = FontStyles.Normal;
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(380f, 100f));
        }

        static void Body(RectTransform canvas, string value, float top, float height)
        {
            var body = Ui.Text("Body", canvas, value, 19f, Color.white);
            body.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(380f, height));
        }
    }
}
