using System;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Onboarding
{
    /// <summary>
    /// Figma "The start" screens 3 and 4: Terms of use (364:254) and Allow geolocation &amp; camera access (364:288).
    /// The player must tick "I accept" to continue; the permission buttons open the real system dialogs.
    /// Run(onDone) shows them as a full-screen overlay and calls onDone after the last CONTINUE.
    /// </summary>
    public static class ConsentFlow
    {
        static readonly Color PillFrom = Fig.Hex("8FBCFF");
        static readonly Color PillTo = Fig.Hex("3A98E4");
        static readonly Color WhitePillTo = Fig.Hex("C5DCFF");
        static readonly Color TickBlue = Fig.Hex("3A98E4");

        static GameObject open;

        public static void Run(Action onDone)
        {
            if (open != null) return;
            if (Consent.Accepted) { ShowPermissions(onDone); return; }
            ShowTerms(() => ShowPermissions(onDone));
        }

        // ---------- Terms of use ----------

        public static RectTransform ShowTerms(Action next)
        {
            var root = NewScreen("Terms Screen");
            Title(root, "Terms of use", 161f, 78f);

            // White 75 % box (392x578 at 58, 305) with the policy text, scrollable.
            var box = Ui.Rounded("Policy Box", root, new Color(1f, 1f, 1f, 0.75f), 0f);
            box.rectTransform.At(58f, 305f, 392f, 578f);
            box.gameObject.AddComponent<RectMask2D>();
            var text = Fig.Body(box.transform, Consent.PolicyText(), 20f, Color.black);
            text.alignment = TextAlignmentOptions.TopLeft;           // long legal text reads better left-aligned
            text.margin = new Vector4(14f, 12f, 14f, 12f);
            var content = text.rectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = box.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = box.rectTransform;
            scroll.horizontal = false;

            // "I accept" tick: white 25 px circle, blue 13 px dot when ticked.
            var accepted = false;
            var circle = Ui.Rounded("Tick", root, Color.white, 12.5f);
            circle.rectTransform.AtBottom(68f, 902f, 25f, 25f);
            var dot = Ui.Rounded("Dot", circle.transform, TickBlue, 6.5f);
            dot.raycastTarget = false;
            dot.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(13f, 13f));
            dot.gameObject.SetActive(false);

            var label = Fig.Body(root, "I am 16+ and accept the terms of use and privacy policy", 20f, Color.white);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 20f;
            label.raycastTarget = true;
            label.rectTransform.AtBottom(104f, 893f, 346f, 44f);

            Button continueButton = null;
            void Toggle()
            {
                accepted = !accepted;
                dot.gameObject.SetActive(accepted);
                SetEnabled(continueButton, accepted);
            }
            AddClick(circle.gameObject, circle, Toggle);
            AddClick(label.gameObject, label, Toggle);

            continueButton = ContinueButton(root, () =>
            {
                if (!accepted) return;
                Consent.Accept();
                Close(root);
                next?.Invoke();
            });
            SetEnabled(continueButton, false);
            return root;
        }

        // ---------- Allow geolocation & camera access ----------

        public static RectTransform ShowPermissions(Action done)
        {
            var root = NewScreen("Permissions Screen");
            Title(root, "Allow geolocation\n& camera access", 140f, 120f);

            var info = Ui.Rounded("Info Box", root, new Color(1f, 1f, 1f, 0.5f), 37f);
            info.raycastTarget = false;
            info.rectTransform.At(38f, 362f, 423f, 253f);
            var text = Fig.Body(info.transform,
                "This project was made in order to support people with disabilities. In this game, the player captures " +
                "places that include accessibility features. Geolocation shows where to capture; the camera takes the photo.",
                25f, Fig.MainText);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 25f;
            text.rectTransform.Stretch(18f);

            PermissionButton(root, 724f, "Allow geolocation", "Geolocation allowed!", () => Consent.HasLocation, Consent.RequestLocation);
            PermissionButton(root, 827f, "Allow camera", "Camera allowed!", () => Consent.HasCamera, Consent.RequestCamera);

            ContinueButton(root, () =>
            {
                Close(root);
                if (Consent.HasLocation && Consent.HasCamera) { done?.Invoke(); return; }
                Popup.Show("Permissions needed",
                    "Without location and camera you can't find or catch memes. You can allow them later in the phone settings.",
                    done);
            });
            return root;
        }

        static void PermissionButton(RectTransform root, float y, string ask, string granted, Func<bool> has, Action<Action<bool>> request)
        {
            var pill = Ui.Rounded(ask, root, Color.white, 32.5f);
            pill.gameObject.AddComponent<HorizontalGradientEffect>().SetColors(Color.white, WhitePillTo);
            pill.rectTransform.AtBottom(77f, y, 350f, 65f);
            var label = Fig.Body(pill.transform, ask, 30f, Color.black);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.Stretch();

            void Refresh()
            {
                var ok = has();
                label.text = ok ? granted : ask;
                if (!pill.TryGetComponent<CanvasGroup>(out var group)) group = pill.gameObject.AddComponent<CanvasGroup>();
                group.alpha = ok ? 0.5f : 1f;                     // Figma: the granted state is faded to 50 %
            }
            AddClick(pill.gameObject, pill, () => { if (!has()) request(_ => Refresh()); });
            Refresh();
        }

        // ---------- Shared pieces ----------

        static RectTransform NewScreen(string name)
        {
            var canvas = Ui.OverlayCanvas(name, 850);
            open = canvas.gameObject;

            // Colorful background (Figma: 538 x 1113.6 from the top-left), cropped to cover any screen.
            var theme = UiTheme.Get();
            var bgBox = Ui.Rect("Background", canvas).Stretch();
            bgBox.gameObject.AddComponent<RectMask2D>();
            var bg = Ui.Image("Colorful Background", bgBox, theme != null ? theme.colorfulBackground : null, false);
            bg.raycastTarget = true;                              // blocks taps on whatever is behind
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 538f / 1113.556f;
            return canvas;
        }

        static void Title(RectTransform root, string value, float y, float h)
        {
            var title = Fig.Display(root, value, 45f, Color.white);
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.rectTransform.At(31f, y, 436f, h);
        }

        /// <summary>Figma CONTINUE: 266x58 pill, #8FBCFF -> #3A98E4, Anuphan 35 white, 63 px above the bottom.</summary>
        static Button ContinueButton(RectTransform root, Action onClick)
        {
            var pill = Ui.Rounded("Continue", root, Color.white, 29f);
            pill.gameObject.AddComponent<HorizontalGradientEffect>().SetColors(PillFrom, PillTo);
            pill.rectTransform.AtBottom(116f, 959f, 266f, 58f);
            var label = Fig.Body(pill.transform, "CONTINUE", 35f, Color.white);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.Stretch();
            return AddClick(pill.gameObject, pill, onClick);
        }

        static void SetEnabled(Button button, bool enabled)
        {
            if (button == null) return;
            button.interactable = enabled;
            if (!button.TryGetComponent<CanvasGroup>(out var group)) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = enabled ? 1f : 0.45f;
        }

        static Button AddClick(GameObject go, Graphic target, Action onClick)
        {
            if (!go.TryGetComponent<Button>(out var button)) button = go.AddComponent<Button>();
            button.targetGraphic = target;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick());
            return button;
        }

        static void Close(RectTransform root)
        {
            if (open == root.gameObject) open = null;
            if (Application.isPlaying) UnityEngine.Object.Destroy(root.gameObject);
            else UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
    }
}
