using System;
using MemeHunter.Game;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Hunt
{
    public enum HuntPhase
    {
        NoHunt,     // nothing claimed: "Start hunt"
        Searching,  // asking the server / waiting for GPS
        Walking,    // a meme is claimed, the player is further than the catch radius
        Near        // within the catch radius: the meme is placed in AR, find it and catch it
    }

    /// <summary>
    /// The AR hunt screen from Figma "Game" (205:426), built from code:
    /// narrative box at the top, distance block, fog that clears as the player gets closer, a lime target frame,
    /// an edge arrow toward the meme, the Catch / Start hunt pill, and the round-icon side menu (MH toggles it).
    /// Catch is only enabled when the meme is inside the frame and the phone is steady (PDF spec).
    /// </summary>
    public sealed class HuntHud : MonoBehaviour
    {
        const float SteadyDegreesPerSecond = 22f;   // slower camera rotation than this counts as "steady"
        const float SteadySeconds = 0.35f;          // ... for at least this long
        const float FrameHalfSize = 0.2f;           // viewport half-size of the target frame (centre ± 0.2)

        public event Action CatchRequested;
        public event Action StartHuntRequested;

        HuntPhase phase = HuntPhase.NoHunt;
        double distanceM = -1;
        string rarity;
        string busyMessage;

        // Built UI
        FogGraphic fog;
        TextMeshProUGUI narrative;
        RectTransform distanceBlock;
        TextMeshProUGUI distanceText;
        RoundedRectGraphic[] frameBars;
        RectTransform frame;
        RectTransform arrow;
        Button actionButton;
        TextMeshProUGUI actionLabel;
        Image actionImage;
        RectTransform sideMenu;
        CompassWidget compass;
        bool hasTarget;
        double targetLat, targetLng;
        bool menuOpen = true;
        bool menuOpenedByPlayer;

        // Steadiness
        Quaternion lastCameraRotation;
        bool hasLastRotation;
        float steadyFor;

        public bool MemeInFrame { get; private set; }
        public bool IsSteady => steadyFor >= SteadySeconds;
        public bool CanCatch => phase == HuntPhase.Near && busyMessage == null && MemeInFrame && IsSteady;

        public static HuntHud Create()
        {
            var canvas = Ui.OverlayCanvas("Hunt HUD", 30);
            var hud = canvas.gameObject.AddComponent<HuntHud>();
            hud.Build(canvas);
            hud.Refresh();
            return hud;
        }

        // ---------- Called by HuntController ----------

        public void SetPhase(HuntPhase value)
        {
            if (phase == value) return;
            phase = value;
            // Figma: the full side menu while idle, only the MH button while hunting (unless the player opened it).
            if (!menuOpenedByPlayer)
                SetMenuOpen(phase == HuntPhase.NoHunt);
            Refresh();
        }

        public void SetDistance(double meters) { distanceM = meters; Refresh(); }
        public void SetRarity(string serverRarity) { rarity = serverRarity; Refresh(); }

        /// <summary>Where the meme is: drives the navigation compass while walking.</summary>
        public void SetTarget(double lat, double lng) { hasTarget = true; targetLat = lat; targetLng = lng; Refresh(); }
        public void ClearTarget() { hasTarget = false; Refresh(); }

        /// <summary>Shows a message in the narrative box and blocks the buttons (e.g. "Sending photo..."); null clears it.</summary>
        public void SetBusy(string message) { busyMessage = message; Refresh(); }

        // ---------- Per frame: steadiness, meme in frame, arrow ----------

        void Update()
        {
            TrackSteadiness();
            TrackMeme();
            Refresh();
        }

        void TrackSteadiness()
        {
            var cam = Camera.main;
            if (cam == null) { steadyFor = SteadySeconds; return; }   // no AR camera (Editor without XR): treat as steady

            var rotation = cam.transform.rotation;
            if (hasLastRotation && Time.deltaTime > 0f)
            {
                var speed = Quaternion.Angle(lastCameraRotation, rotation) / Time.deltaTime;
                steadyFor = speed < SteadyDegreesPerSecond ? steadyFor + Time.deltaTime : 0f;
            }
            lastCameraRotation = rotation;
            hasLastRotation = true;
        }

        void TrackMeme()
        {
            MemeInFrame = false;
            arrow.gameObject.SetActive(false);
            if (phase != HuntPhase.Near) return;

            var cam = Camera.main;
            if (cam == null || !MemeSpawner.TryGetCenter(out var center))
                return;

            var v = cam.WorldToViewportPoint(center);
            var behind = v.z < 0f;
            if (behind) { v.x = 1f - v.x; v.y = 1f - v.y; }          // mirror so the arrow still points the right way

            var offset = new Vector2(v.x - 0.5f, v.y - 0.5f);
            MemeInFrame = !behind && Mathf.Abs(offset.x) <= FrameHalfSize + 0.08f && Mathf.Abs(offset.y) <= FrameHalfSize + 0.08f;
            if (MemeInFrame || offset.sqrMagnitude < 1e-6f) return;

            // Arrow on an oval around the frame, pointing toward the meme.
            var dir = offset.normalized;
            arrow.gameObject.SetActive(true);
            arrow.anchoredPosition = new Vector2(dir.x * 150f, dir.y * 260f);
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }

        // ---------- Visual state ----------

        void Refresh()
        {
            if (narrative == null) return;

            narrative.text = NarrativeText();

            // Walking with a known target: the compass (arrow + distance). Otherwise the plain distance block.
            var showCompass = phase == HuntPhase.Walking && hasTarget;
            if (showCompass) compass.SetTarget(targetLat, targetLng); else compass.ClearTarget();
            var showDistance = !showCompass && phase == HuntPhase.Walking && distanceM >= 0;
            distanceBlock.gameObject.SetActive(showDistance);
            if (showDistance)
                distanceText.text = distanceM >= 1000 ? (distanceM / 1000.0).ToString("0.0") + " km" : Mathf.RoundToInt((float)distanceM) + " m";

            fog.Density = FogDensity();

            var frameColor = phase != HuntPhase.Near ? new Color(1f, 1f, 1f, 0.75f)
                : MemeInFrame ? (IsSteady ? MemeHunterUiColors.LegendaryGold : MemeHunterUiColors.Lime)
                : new Color(1f, 1f, 1f, 0.9f);
            foreach (var bar in frameBars) bar.color = frameColor;
            var pulse = CanCatch ? 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 8f) : 1f;
            frame.localScale = Vector3.one * pulse;

            // The pill is "Start hunt" without a hunt, "Catch" during one. Disabled = faded.
            var idle = phase == HuntPhase.NoHunt;
            actionLabel.text = idle ? "Start hunt" : "Catch";
            var enabled = busyMessage == null && (idle || CanCatch);
            actionButton.interactable = enabled;
            actionImage.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            actionLabel.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            actionButton.gameObject.SetActive(phase != HuntPhase.Searching && phase != HuntPhase.Walking);
        }

        string NarrativeText()
        {
            if (busyMessage != null) return busyMessage;
            var name = string.IsNullOrEmpty(rarity) ? "meme" : rarity.ToLowerInvariant() + " meme";
            switch (phase)
            {
                case HuntPhase.Searching: return "Looking for memes nearby...";
                case HuntPhase.Walking: return $"A {name} is hiding nearby. Get closer!";
                case HuntPhase.Near:
                    if (!MemeInFrame) return "It's here! Turn around and follow the arrow.";
                    if (!IsSteady) return "Hold still...";
                    return "Got it! Tap Catch!";
                default: return "Tap Start hunt to look for memes around you.";
            }
        }

        float FogDensity()
        {
            switch (phase)
            {
                case HuntPhase.Near: return 0f;
                case HuntPhase.Walking:
                    if (distanceM < 0) return 0.8f;
                    // Thick at 150 m and beyond, almost clear at the 30 m catch radius.
                    return Mathf.Lerp(0.15f, 0.85f, Mathf.InverseLerp(30f, 150f, (float)distanceM));
                case HuntPhase.Searching: return 0.6f;
                default: return 0.3f;
            }
        }

        void SetMenuOpen(bool open)
        {
            menuOpen = open;
            for (var i = 0; i < sideMenu.childCount - 1; i++)          // the last child is the MH toggle: always visible
                sideMenu.GetChild(i).gameObject.SetActive(open);
        }

        // ---------- Construction ----------

        void Build(RectTransform canvas)
        {
            var theme = UiTheme.Get();

            fog = Ui.Rect("Fog", canvas).gameObject.AddComponent<FogGraphic>();
            fog.rectTransform.Stretch();
            fog.color = MemeHunterUiColors.LightCyanBackground;
            fog.raycastTarget = false;

            // Narrative box: semi-transparent navy, white text (PDF: "narrative box at the top").
            var box = Ui.Rounded("Narrative Box", canvas, new Color(0.15f, 0.18f, 0.36f, 0.72f), 22f);
            box.raycastTarget = false;
            box.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(440f, 84f));
            narrative = Ui.Text("Narrative", box.transform, string.Empty, 22f, Color.white);
            narrative.rectTransform.Stretch(14f);
            narrative.enableAutoSizing = true;
            narrative.fontSizeMin = 16f;
            narrative.fontSizeMax = 22f;

            // Distance block: big lime Jaro number on a navy pill (design fix "цифро блок с дистанцией").
            var pill = Ui.Rounded("Distance Block", canvas, new Color(0.15f, 0.18f, 0.36f, 0.72f), 30f);
            pill.raycastTarget = false;
            distanceBlock = pill.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(190f, 60f));
            distanceText = Ui.Text("Distance", pill.transform, string.Empty, 40f, MemeHunterUiColors.Lime, display: true);
            distanceText.rectTransform.Stretch();
            distanceText.textWrappingMode = TextWrappingModes.NoWrap;

            compass = CompassWidget.Create(canvas);
            ((RectTransform)compass.transform).Place(new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(150f, 126f));

            // Target frame: four lime corner brackets (Figma Scan Centre), 200x200 in the middle.
            frame = Ui.Rect("Target Frame", canvas).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f));
            frameBars = new RoundedRectGraphic[8];
            var corners = new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
            for (var i = 0; i < 4; i++)
            {
                var c = corners[i];
                var horizontal = Ui.Rounded("Corner H", frame, Color.white, 4f);
                horizontal.raycastTarget = false;
                horizontal.rectTransform.Place(c, Vector2.zero, new Vector2(50f, 8f));
                var vertical = Ui.Rounded("Corner V", frame, Color.white, 4f);
                vertical.raycastTarget = false;
                vertical.rectTransform.Place(c, Vector2.zero, new Vector2(8f, 50f));
                frameBars[i * 2] = horizontal;
                frameBars[i * 2 + 1] = vertical;
            }

            // Edge arrow toward the meme (design fix "стрелка для поворота камеры").
            var arrowGraphic = Ui.Rect("Arrow", canvas).gameObject.AddComponent<TriangleGraphic>();
            arrowGraphic.color = MemeHunterUiColors.Lime;
            arrowGraphic.raycastTarget = false;
            arrow = arrowGraphic.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 60f));
            arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.gameObject.SetActive(false);

            // Catch / Start hunt pill at the bottom centre (Figma 185x58).
            actionButton = Ui.PillButton("Action", canvas, "Catch", OnActionPressed, 30f);
            actionImage = (Image)actionButton.targetGraphic;
            actionLabel = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            ((RectTransform)actionButton.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(210f, 64f));

            // Side menu (right edge, bottom-up): Profile, Map, Notifications, XD (Collections), Donate, MH toggle.
            sideMenu = Ui.Rect("Side Menu", canvas).Place(new Vector2(1f, 0f), new Vector2(-14f, 40f), new Vector2(62f, 6 * 62f + 5 * 10f));
            var column = sideMenu.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 10f;
            column.childAlignment = TextAnchor.LowerCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandHeight = false;

            AddMenuButton("Profile", theme?.profileIcon, () => AppNav.OpenScreen(AppNav.Profile));
            AddMenuButton("Map", theme?.mapIcon, () => AppNav.OpenScreen(AppNav.Map));
            AddMenuButton("Notifications", theme?.notificationsIcon, () => AppNav.OpenScreen(AppNav.Notifications));
            AddMenuButton("Collections", theme?.collectionIcon, () => AppNav.OpenScreen(AppNav.Collections));
            AddMenuButton("Donate", theme?.donateIcon, () => DonateScreen.Open());
            AddMenuButton("Menu Toggle", theme?.menuToggleIcon, () => { menuOpenedByPlayer = true; SetMenuOpen(!menuOpen); });
        }

        void AddMenuButton(string name, Sprite icon, Action onClick)
        {
            var button = Ui.IconButton(name, sideMenu, icon, onClick);
            var size = button.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 62f;
            size.preferredHeight = 62f;
        }

        void OnActionPressed()
        {
            if (phase == HuntPhase.NoHunt) StartHuntRequested?.Invoke();
            else if (CanCatch) CatchRequested?.Invoke();
        }
    }
}
