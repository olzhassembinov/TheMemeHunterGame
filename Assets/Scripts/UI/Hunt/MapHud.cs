using System;
using System.Collections.Generic;
using MemeHunter.Game;
using MemeHunter.Net;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemeHunter.UI.Hunt
{
    /// <summary>
    /// Makes the designed Map screen (Figma 208:785) work: nearby memes as rarity-coloured dots around the player,
    /// map tools (zoom +/-, show/hide dots, recenter), the navigation buttons incl. Camera, a distance block and the
    /// "Meme nearby!" popup. Added at runtime to the ScreenView with id "Map", so the scene file is not edited.
    /// The street map underneath is TileMapLayer (OpenStreetMap, or Mapbox with a token).
    /// </summary>
    public sealed class MapHud : MonoBehaviour
    {
        const float RefreshSeconds = 15f;
        const double NearbyPopupM = 120.0;      // server positions are rounded to ~100 m, so "nearby" is generous

        TileMapLayer map;
        RectTransform dotLayer;
        RectTransform playerDot;
        TextMeshProUGUI distanceText;
        Button cameraButton;
        CompassWidget compass;
        RoundedRectGraphic cameraHalo;
        RectTransform navGroup;
        bool navOpen = true;

        readonly List<NearbyCardDto> cards = new List<NearbyCardDto>();
        readonly HashSet<string> announced = new HashSet<string>();   // cards that already showed the popup
        float nextRefresh;
        double nearestM = -1;
        bool loading;

        // ---------- Attach to the Map screen when the main scene loads ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Hook() => SceneManager.sceneLoaded += (_, _) => Attach();

        static void Attach()
        {
            foreach (var view in FindObjectsByType<ScreenView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (view.ScreenId != AppNav.Map || view.GetComponentInChildren<MapHud>(true) != null) continue;
                var root = Ui.Rect("Map HUD", view.transform).Stretch();
                root.gameObject.AddComponent<MapHud>().Build(view.transform, root);
            }
        }

        // ---------- Lifecycle (runs only while the Map screen is visible) ----------

        void OnEnable() { nextRefresh = 0f; }

        async void Update()
        {
            UpdateDots();
            PulseCamera();

            if (loading || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            loading = true;
            try
            {
                if (!await Geo.StartAsync() || !Geo.TryGetFix(out var fix)) return;
                var nearby = await Api.GetNearby(fix.Lat, fix.Lng);
                if (this == null) return;                                  // screen closed while waiting
                cards.Clear();
                cards.AddRange(nearby);
            }
            catch (ApiException)
            {
                // Quiet on the map: it retries every 15 s. Errors are shown when the player starts a hunt.
            }
            finally
            {
                loading = false;
            }
        }

        // ---------- Dots, distance, nearby popup ----------

        void UpdateDots()
        {
            if (!Geo.TryGetFix(out var fix)) { distanceText.text = "Finding you..."; return; }
            Layout(fix.Lat, fix.Lng, allowPopup: true);
        }

        /// <summary>Editor preview: lays out the given cards around a fixed position (no GPS, no server).</summary>
        public static MapHud PreviewOn(ScreenView mapScreen, double lat, double lng, IEnumerable<NearbyCardDto> fakeCards)
        {
            var root = Ui.Rect("Map HUD", mapScreen.transform).Stretch();
            var hud = root.gameObject.AddComponent<MapHud>();
            hud.Build(mapScreen.transform, root);
            hud.cards.AddRange(fakeCards);
            hud.Layout(lat, lng, allowPopup: false);
            Canvas.ForceUpdateCanvases();                                   // let the button columns lay out first
            hud.PulseCamera();
            return hud;
        }

        void Layout(double fixLat, double fixLng, bool allowPopup)
        {
            var fix = (Lat: fixLat, Lng: fixLng);
            map.SetPlayer(fix.Lat, fix.Lng);
            playerDot.anchoredPosition = map.Project(fix.Lat, fix.Lng);   // moves off-centre when the map is dragged

            var hunt = Session.ActiveHunt;
            var shown = 0;
            nearestM = -1;
            NearbyCardDto nearest = null;

            foreach (var card in cards)
            {
                var d = Geo.DistanceMeters(fix.Lat, fix.Lng, card.ApproxLat, card.ApproxLng);
                if (nearestM < 0 || d < nearestM) { nearestM = d; nearest = card; }
                var dot = DotAt(shown++, Rarity.ColorOf(card.Rarity), 24f);
                dot.anchoredPosition = map.Project(card.ApproxLat, card.ApproxLng);
            }

            if (hunt != null)                                              // the claimed meme: exact pin, gold ring
            {
                var pin = DotAt(shown++, Rarity.ColorOf(hunt.Rarity), 30f, ring: true);
                pin.anchoredPosition = map.Project(hunt.Lat, hunt.Lng);
                nearestM = Geo.DistanceMeters(fix.Lat, fix.Lng, hunt.Lat, hunt.Lng);
            }

            for (var i = shown; i < dotLayer.childCount; i++)
                dotLayer.GetChild(i).gameObject.SetActive(false);

            // Compass toward the claimed meme, or else the nearest one.
            if (hunt != null) compass.SetTarget(hunt.Lat, hunt.Lng);
            else if (nearest != null) compass.SetTarget(nearest.ApproxLat, nearest.ApproxLng);
            else compass.ClearTarget();

            distanceText.text = nearestM < 0 ? "No memes nearby"
                : hunt != null ? "Your meme: " + FormatDistance(nearestM)
                : "Nearest meme: " + FormatDistance(nearestM);

            if (allowPopup && hunt == null && nearest != null && nearestM <= NearbyPopupM && announced.Add(nearest.Id))
                ShowNearbyPopup(nearest.Rarity);
        }

        bool CameraUnlocked => Session.ActiveHunt != null || (nearestM >= 0 && nearestM <= NearbyPopupM);

        void ShowNearbyPopup(string rarity)
        {
            Handheld.Vibrate();
            var popup = Popup.Create("Meme nearby!", $"A {rarity.ToLowerInvariant()} meme is hiding close to you. Open the camera and catch it!", 70f);
            var start = Ui.PillButton("Start hunt", popup.Content, "Start hunt", () =>
            {
                popup.Close();
                AppNav.OpenHunt(startHunting: true);
            });
            ((RectTransform)start.transform).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 64f));
        }

        /// <summary>The camera is always available: it opens the AR hunt (and starts one if none is active).</summary>
        void OnCameraPressed() => AppNav.OpenHunt(startHunting: Session.ActiveHunt == null);

        void PulseCamera()
        {
            var on = CameraUnlocked;
            cameraHalo.gameObject.SetActive(on);
            if (on)
            {
                var target = (RectTransform)cameraButton.transform;
                var halo = cameraHalo.rectTransform;
                halo.anchorMin = target.anchorMin;
                halo.anchorMax = target.anchorMax;
                halo.pivot = target.pivot;
                halo.anchoredPosition = target.anchoredPosition;
                halo.sizeDelta = target.sizeDelta + new Vector2(12f, 12f);
                var k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                cameraHalo.color = new Color(1f, 0.77f, 0.15f, 0.35f + 0.4f * k);
                cameraHalo.transform.localScale = Vector3.one * (1.05f + 0.1f * k);
            }
        }

        RectTransform DotAt(int index, Color color, float size, bool ring = false)
        {
            RoundedRectGraphic dot;
            if (index < dotLayer.childCount)
            {
                dot = dotLayer.GetChild(index).GetComponent<RoundedRectGraphic>();
                dot.gameObject.SetActive(true);
            }
            else
            {
                dot = Ui.Rounded("Dot", dotLayer, color, size / 2f);
                dot.raycastTarget = false;
                var inner = Ui.Rounded("Inner", dot.transform, color, size / 2f - 4f);
                inner.raycastTarget = false;
                inner.rectTransform.Stretch(4f);
            }
            dot.rectTransform.Place(new Vector2(0.5f, 0.5f), dot.rectTransform.anchoredPosition, new Vector2(size, size));
            dot.color = ring ? MemeHunterUiColors.LegendaryGold : Color.white;         // white outline, gold for the claimed one
            dot.transform.GetChild(0).GetComponent<RoundedRectGraphic>().color = color;
            return dot.rectTransform;
        }

        static string FormatDistance(double m) =>
            m >= 1000 ? (m / 1000.0).ToString("0.0") + " km" : Mathf.RoundToInt((float)m) + " m";

        // ---------- Construction ----------

        void Build(Transform screen, RectTransform root)
        {
            var theme = UiTheme.Get();

            // The designed sidebar and sample dots were static pictures: replaced by working ones.
            HideChild(screen, "Map Tool Sidebar");
            var presenter = screen.GetComponentInChildren<MapScreenPresenter>(true);
            if (presenter != null) presenter.enabled = false;
            foreach (var sample in screen.GetComponentsInChildren<MemeLocationDot>(true))
                sample.gameObject.SetActive(false);
            // Old close / back buttons of the designed map: the Map is the home screen now, it has no close button.
            foreach (var old in screen.GetComponentsInChildren<CircularActionButton>(true))
                old.gameObject.SetActive(false);

            // Real street map instead of the placeholder picture.
            HideChild(screen, "Map Placeholder Base");
            HideChild(screen, "Placeholder Map Artwork");
            HideChild(screen, "Map Placeholder Label");
            map = TileMapLayer.Create(root);

            dotLayer = Ui.Rect("Meme Dots", root).Stretch();

            // The player: blue dot with a white ring in the centre.
            var me = Ui.Rounded("Player", root, Color.white, 15f);
            me.raycastTarget = false;
            playerDot = me.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            var meInner = Ui.Rounded("Inner", me.transform, new Color32(64, 160, 255, 255), 11f);
            meInner.raycastTarget = false;
            meInner.rectTransform.Stretch(4f);

            // Distance block (top centre, navy pill, lime Jaro).
            var pill = Ui.Rounded("Distance Block", root, new Color(0.15f, 0.18f, 0.36f, 0.8f), 24f);
            pill.raycastTarget = false;
            pill.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(280f, 52f));
            distanceText = Ui.Text("Distance", pill.transform, "Finding you...", 24f, MemeHunterUiColors.Lime, display: true);
            distanceText.rectTransform.Stretch(8f);
            distanceText.textWrappingMode = TextWrappingModes.NoWrap;
            distanceText.enableAutoSizing = true;
            distanceText.fontSizeMin = 14f;
            distanceText.fontSizeMax = 24f;

            compass = CompassWidget.Create(root);
            ((RectTransform)compass.transform).Place(new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(150f, 126f));

            // Map tools (right edge, upper): zoom +, zoom -, show/hide dots, recenter.
            var tools = Column(root, "Map Tools", new Vector2(1f, 1f), new Vector2(-14f, -110f), 4);
            AddButton(tools, "Zoom In", theme?.plusIcon, () => map.ZoomBy(1));
            AddButton(tools, "Zoom Out", theme?.minusIcon, () => map.ZoomBy(-1));
            AddButton(tools, "Show Hide Dots", theme?.showHideIcon, () => dotLayer.gameObject.SetActive(!dotLayer.gameObject.activeSelf));
            AddButton(tools, "Recenter", theme?.mapIcon, () => { map.Recenter(); nextRefresh = 0f; });

            // Navigation (right edge, bottom-up): Camera, Profile, Notifications, XD, Donate, MH toggle.
            navGroup = Column(root, "Navigation", new Vector2(1f, 0f), new Vector2(-14f, 40f), 6);
            navGroup.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;
            cameraButton = AddButton(navGroup, "Camera", theme?.cameraIcon, OnCameraPressed);
            // Halo behind the Camera button (a sibling drawn first, kept on top of the button's position).
            cameraHalo = Ui.Rounded("Camera Halo", navGroup, MemeHunterUiColors.LegendaryGold, 35f);
            cameraHalo.raycastTarget = false;
            cameraHalo.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            cameraHalo.transform.SetAsFirstSibling();
            AddButton(navGroup, "Profile", theme?.profileIcon, () => AppNav.OpenScreen(AppNav.Profile));
            AddButton(navGroup, "Notifications", theme?.notificationsIcon, () => AppNav.OpenScreen(AppNav.Notifications));
            AddButton(navGroup, "Collections", theme?.collectionIcon, () => AppNav.OpenScreen(AppNav.Collections));
            AddButton(navGroup, "Donate", theme?.donateIcon, () => DonateScreen.Open());
            AddButton(navGroup, "Menu Toggle", theme?.menuToggleIcon, ToggleNav);
        }

        void ToggleNav()
        {
            navOpen = !navOpen;
            for (var i = 2; i < navGroup.childCount - 1; i++)       // halo (0), Camera (1) and MH (last) always stay
                navGroup.GetChild(i).gameObject.SetActive(navOpen);
        }

        static RectTransform Column(RectTransform parent, string name, Vector2 anchor, Vector2 offset, int count)
        {
            var column = Ui.Rect(name, parent).Place(anchor, offset, new Vector2(58f, count * 58f + (count - 1) * 10f));
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        static Button AddButton(RectTransform column, string name, Sprite icon, Action onClick)
        {
            var button = Ui.IconButton(name, column, icon, onClick);
            var size = button.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 58f;
            size.preferredHeight = 58f;
            return button;
        }

        static void HideChild(Transform parent, string childName)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) t.gameObject.SetActive(false);
        }
    }
}
