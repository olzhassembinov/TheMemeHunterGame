using System;
using MemeHunter.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Avatars
{
    /// <summary>
    /// "Choose your hunter" popup: the 8 avatars in a 4x2 grid of circles, the chosen one ringed in blue.
    /// Confirming with the round check button saves the choice (AvatarCatalog.Select).
    /// Only one picker exists at a time; Close() removes it without saving (e.g. the player left the page).
    /// </summary>
    public static class AvatarPicker
    {
        const float Cell = 84f;
        const float Gap = 12f;

        static readonly Color CircleFill = new Color32(168, 216, 255, 255);   // light blue behind the faces
        static readonly Color RingIdle = new Color32(255, 255, 255, 255);
        static readonly Color RingSelected = MemeHunterUiColors.FormBlue;

        static Popup open;

        public static bool IsOpen => open != null;

        /// <summary>Removes the picker without saving a choice. Safe to call when none is open.</summary>
        public static void Close()
        {
            if (open != null) UnityEngine.Object.Destroy(open.gameObject);
            open = null;
        }

        public static Popup Open(Action onDone = null)
        {
            if (open != null) return open;                 // already on screen: never stack a second one

            var avatars = AvatarCatalog.All;
            if (avatars.Length == 0)
            {
                Debug.LogWarning("No avatars found in Resources/Avatars.");
                onDone?.Invoke();
                return null;
            }

            var rows = Mathf.CeilToInt(avatars.Length / 4f);
            var popup = Popup.Create("Choose your hunter", "Pick a face for your profile. You can change it later.",
                rows * Cell + (rows - 1) * Gap + 8f);

            var grid = popup.Content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(Cell, Cell);
            grid.spacing = new Vector2(Gap, Gap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.MiddleCenter;

            var selected = Mathf.Max(0, AvatarCatalog.SelectedIndex);
            var rings = new RoundedRectGraphic[avatars.Length];

            for (var i = 0; i < avatars.Length; i++)
            {
                var index = i;

                // Ring (outer circle) -> masked light-blue circle -> avatar face.
                var ring = Ui.Rounded("Avatar " + (i + 1), popup.Content, RingIdle, Cell / 2f);
                rings[i] = ring;

                var circle = Ui.Rounded("Circle", ring.transform, CircleFill, (Cell - 10f) / 2f);
                circle.rectTransform.Stretch(5f);
                circle.raycastTarget = false;
                circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                // The Figma avatars have empty margins: enlarge and lower the face so it fills the circle.
                var face = Ui.Image("Face", circle.transform, avatars[i]);
                face.rectTransform.Stretch();
                face.rectTransform.offsetMin = new Vector2(-12f, -16f);
                face.rectTransform.offsetMax = new Vector2(12f, 8f);

                var button = ring.gameObject.AddComponent<Button>();
                button.targetGraphic = ring;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() =>
                {
                    selected = index;
                    Highlight(rings, selected);
                });
            }

            Highlight(rings, selected);
            popup.SetAcceptAction(() =>
            {
                open = null;
                AvatarCatalog.Select(selected);
                onDone?.Invoke();
            });
            open = popup;
            return popup;
        }

        static void Highlight(RoundedRectGraphic[] rings, int selected)
        {
            for (var i = 0; i < rings.Length; i++)
            {
                rings[i].color = i == selected ? RingSelected : RingIdle;
                rings[i].transform.localScale = Vector3.one * (i == selected ? 1.08f : 1f);
            }
        }
    }
}
