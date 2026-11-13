using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Kit
{
    /// <summary>
    /// The Figma "Pop-up Notifications" card (node 364:118): a dimmed screen, a light-cyan card with a lavender
    /// outline, a Jaro title, Inter body text, optional custom content and a round check button.
    /// Usage: Popup.Show("Meme caught!", "+40 coins"); or build custom content into Popup.Create(...).Content.
    /// </summary>
    public sealed class Popup : MonoBehaviour
    {
        const float CardWidth = 450f;
        const int SortingOrder = 900;               // above scene UI, below the dev toast (999)

        public RectTransform Card { get; private set; }
        /// <summary>Area between the body text and the button for extra elements (grids, cards...).</summary>
        public RectTransform Content { get; private set; }
        public TextMeshProUGUI Title { get; private set; }
        public TextMeshProUGUI Body { get; private set; }
        public Button AcceptButton { get; private set; }

        Action onClosed;

        /// <summary>Simple message popup. <paramref name="onAccept"/> runs after it closes.</summary>
        public static Popup Show(string title, string body, Action onAccept = null)
        {
            return Create(title, body, 0f, onAccept);
        }

        /// <summary>Popup with a custom content area of the given height (0 = none).</summary>
        public static Popup Create(string title, string body, float contentHeight, Action onAccept = null)
        {
            var canvas = Ui.OverlayCanvas("Popup: " + title, SortingOrder);
            var popup = canvas.gameObject.AddComponent<Popup>();
            popup.onClosed = onAccept;

            // Dim layer: also swallows taps on whatever is behind the popup.
            var dim = Ui.Rounded("Dim", canvas, Ui.Dim, 0f);
            dim.rectTransform.Stretch();

            var layout = Ui.Rect("Card Layout", canvas);
            var card = Ui.BorderedCard("Card", layout, Ui.PopupFill, Ui.PopupBorder, 40f, 5f);
            popup.Card = card;

            var column = card.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(32, 32, 34, 30);
            column.spacing = 18f;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            // Ignore the border child (it is stretched by Ui.BorderedCard, not laid out).
            card.GetChild(0).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            popup.Title = Ui.Text("Title", card, title, 34f, Ui.Ink, display: true);
            popup.Body = Ui.Text("Body", card, body, 20f, Ui.Ink);
            popup.Body.gameObject.SetActive(!string.IsNullOrEmpty(body));

            popup.Content = Ui.Rect("Content", card);
            var contentLayout = popup.Content.gameObject.AddComponent<LayoutElement>();
            contentLayout.preferredHeight = contentHeight;
            popup.Content.gameObject.SetActive(contentHeight > 0f);

            var buttonRow = Ui.Rect("Button Row", card);
            buttonRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
            var theme = UiTheme.Get();
            popup.AcceptButton = Ui.IconButton("Accept", buttonRow, theme != null ? theme.acceptButton : null, popup.Close);
            ((RectTransform)popup.AcceptButton.transform).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));

            // The card sizes itself to its content; the layout parent centres it.
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            card.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardWidth, 0f));
            layout.Stretch();

            popup.StartCoroutine(popup.PopIn());
            return popup;
        }

        /// <summary>Changes what the round button does before closing (e.g. "save the chosen avatar").</summary>
        public void SetAcceptAction(Action action) => onClosed = action;

        public void Close()
        {
            var action = onClosed;
            onClosed = null;
            Destroy(gameObject);
            action?.Invoke();
        }

        System.Collections.IEnumerator PopIn()
        {
            // Small overshoot "pop", matching the playful tone; unscaled so it also works while paused.
            const float duration = 0.22f;
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                var k = t / duration;
                var scale = k < 0.7f ? Mathf.Lerp(0.85f, 1.04f, k / 0.7f) : Mathf.Lerp(1.04f, 1f, (k - 0.7f) / 0.3f);
                Card.localScale = Vector3.one * scale;
                yield return null;
            }
            Card.localScale = Vector3.one;
        }
    }
}
