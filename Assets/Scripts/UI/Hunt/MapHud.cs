using System;
using System.Collections.Generic;
using MemeHunter.Game;
using MemeHunter.Net;
using MemeHunter.UI.Kit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemeHunter.UI.Hunt
{
    /// <summary>
    /// Makes the designed Map screen (Figma 208:785) work: nearby memes as rarity-coloured dots around the player,
    /// map tools (zoom +/-, show/hide dots, recenter), the navigation column, the direction bar at the bottom
    /// (arrow, distance and the Hunt button, see DirectionPanel) and the "Meme nearby!" popup.
    /// Added at runtime to the ScreenView with id "Map", so the scene file is not edited.
    /// The street map underneath is TileMapLayer (OpenStreetMap, or Mapbox with a token).
    /// </summary>
    public sealed class MapHud : MonoBehaviour
    {
        const float RefreshSeconds = 15f;
        const double NearbyPopupM = 120.0;      // server positions are rounded to ~100 m, so "nearby" is generous
        const float ButtonSize = 58f;
        const float Gap = 10f;

        TileMapLayer map;
        RectTransform dotLayer;
        RectTransform playerDot;
        DirectionPanel panel;
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

        void Update()
        {
            UpdateDots();
            panel.SetHuntGlow(CameraUnlocked);
            if (!loading && Time.unscaledTime >= nextRefresh)
                RefreshNearby();
        }

        /// <summary>Asks the server for the memes around the player (every 15 s while the Map is open).</summary>
        async void RefreshNearby()
        {
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

        // ---------- Dots, direction bar, nearby popup ----------

        void UpdateDots()
        {
            if (!Geo.TryGetFix(out var fix))
            {
                panel.ClearTarget();
                panel.Show("LOCATING", null, "Finding you...");
                return;
            }
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
            hud.panel.PointFrom(lat, lng);
            hud.panel.SetHuntGlow(hud.CameraUnlocked);
            hud.panel.Pulse(0.4f);
            return hud;
        }

        void Layout(double fixLat, double fixLng, bool allowPopup)
        {
            map.SetPlayer(fixLat, fixLng);
            playerDot.anchoredPosition = map.Project(fixLat, fixLng);      // moves off-centre when the map is dragged

            var hunt = Session.ActiveHunt;
            if (hunt != null && Session.HuntExpired(hunt))                 // the 30 minutes ran out: the card is free again
            {
                Session.ClearHunt();
                hunt = null;
                if (allowPopup) HuntErrors.TimeUp();
            }
            var shown = 0;
            nearestM = -1;
            NearbyCardDto nearest = null;

            foreach (var card in cards)
            {
                var d = Geo.DistanceMeters(fixLat, fixLng, card.ApproxLat, card.ApproxLng);
                if (nearestM < 0 || d < nearestM) { nearestM = d; nearest = card; }
                var dot = DotAt(shown++, Rarity.ColorOf(card.Rarity), 24f);
                dot.anchoredPosition = map.Project(card.ApproxLat, card.ApproxLng);
            }

            if (hunt != null)                                              // the claimed meme: exact pin, gold ring
            {
                var pin = DotAt(shown++, Rarity.ColorOf(hunt.Rarity), 30f, ring: true);
                pin.anchoredPosition = map.Project(hunt.Lat, hunt.Lng);
                nearestM = Geo.DistanceMeters(fixLat, fixLng, hunt.Lat, hunt.Lng);
            }

            for (var i = shown; i < dotLayer.childCount; i++)
                dotLayer.GetChild(i).gameObject.SetActive(false);

            // Direction bar: toward the claimed meme, or else the nearest one.
            if (hunt != null)
            {
                panel.SetTarget(hunt.Lat, hunt.Lng);
                var left = Session.HuntTimeLeft(hunt);
                var what = "YOUR MEME · " + RarityName(hunt.Rarity) + (left.HasValue ? " · " + Session.FormatTimeLeft(left.Value) : string.Empty);
                panel.Show(what, Rarity.ColorOf(hunt.Rarity), FormatDistance(nearestM));
            }
            else if (nearest != null)
            {
                panel.SetTarget(nearest.ApproxLat, nearest.ApproxLng);
                panel.Show("NEAREST MEME · " + RarityName(nearest.Rarity), Rarity.ColorOf(nearest.Rarity), FormatDistance(nearestM));
            }
            else
            {
                panel.ClearTarget();
                panel.Show("NO MEMES NEARBY", null, "Keep exploring");
            }

            // Not over the Terms / Permissions screens (first launch): the popup is drawn above them.
            if (allowPopup && hunt == null && nearest != null && nearestM <= NearbyPopupM && !Onboarding.ConsentFlow.IsOpen
                && announced.Add(nearest.Id))
                ShowNearbyPopup(nearest.Rarity);
        }

        bool CameraUnlocked => Session.ActiveHunt != null || (nearestM >= 0 && nearestM <= NearbyPopupM);

        void ShowNearbyPopup(string rarity)
        {
            Handheld.Vibrate();
            var popup = Popup.Create("Meme nearby!", $"{Rarity.MemeWithArticle(rarity, true)} is hiding close to you. Open the camera and catch it!", 70f);
            var start = Ui.PillButton("Start hunt", popup.Content, "Start hunt", () =>
            {
                popup.Close();
                AppNav.OpenHunt(startHunting: true);
            });
            ((RectTransform)start.transform).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 64f));
        }

        /// <summary>The Hunt button is always available: it opens the AR hunt (and starts one if none is active).</summary>
        void OnHuntPressed() => AppNav.OpenHunt(startHunting: Session.ActiveHunt == null);

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
            var innerDot = dot.transform.GetChild(0).GetComponent<RoundedRectGraphic>();
            innerDot.color = color;
            // A reused dot can change size (pin 30 / dot 24): keep it round. Only when it changes (redraws the mesh).
            if (!Mathf.Approximately(dot.CornerRadius, size / 2f))
            {
                dot.CornerRadius = size / 2f;
                innerDot.CornerRadius = size / 2f - 4f;
            }
            return dot.rectTransform;
        }

        static string RarityName(string serverRarity) => Rarity.Name(Rarity.Parse(serverRarity)).ToUpperInvariant();   // EPIC, not UNCOMMON

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
            map.LiftCredit(DirectionPanel.Height);

            dotLayer = Ui.Rect("Meme Dots", root).Stretch();

            // The player: blue dot with a white ring in the centre.
            var me = Ui.Rounded("Player", root, Color.white, 15f);
            me.raycastTarget = false;
            playerDot = me.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            var meInner = Ui.Rounded("Inner", me.transform, new Color32(64, 160, 255, 255), 11f);
            meInner.raycastTarget = false;
            meInner.rectTransform.Stretch(4f);

            // Map tools (right edge, upper): zoom +, zoom -, show/hide dots, recenter.
            var tools = Column(root, "Map Tools", new Vector2(1f, 1f), new Vector2(-14f, -40f), 4);
            AddButton(tools, "Zoom In", theme?.plusIcon, () => map.ZoomBy(1));
            AddButton(tools, "Zoom Out", theme?.minusIcon, () => map.ZoomBy(-1));
            AddButton(tools, "Show Hide Dots", theme?.showHideIcon, () => dotLayer.gameObject.SetActive(!dotLayer.gameObject.activeSelf));
            AddButton(tools, "Recenter", theme?.mapIcon, () => { map.Recenter(); nextRefresh = 0f; });

            // Navigation (right edge, bottom-up, above the direction bar): the pages, Donate and the MH toggle.
            navGroup = Column(root, "Navigation", new Vector2(1f, 0f), new Vector2(-14f, DirectionPanel.Height + 18f), 6);
            navGroup.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;
            AddButton(navGroup, "Profile", theme?.profileIcon, () => AppNav.OpenScreen(AppNav.Profile));
            if (Features.Notifications)                                    // sample messages only: no server notifications yet
                AddButton(navGroup, "Notifications", theme?.notificationsIcon, () => AppNav.OpenScreen(AppNav.Notifications));
            AddButton(navGroup, "Collections", theme?.collectionIcon, () => AppNav.OpenScreen(AppNav.Collections));
            AddButton(navGroup, "Leaderboard", theme?.leaderboardIcon, () => AppNav.OpenScreen(AppNav.Leaderboard));
            if (Features.Donate)
                AddButton(navGroup, "Donate", theme?.donateIcon, () => DonateScreen.Open());
            AddButton(navGroup, "Menu Toggle", theme?.menuToggleIcon, ToggleNav);

            // Direction bar along the bottom edge (drawn last: on top of the map and the dots).
            panel = DirectionPanel.Create(root, OnHuntPressed);
            panel.Show("LOCATING", null, "Finding you...");
        }

        void ToggleNav()
        {
            navOpen = !navOpen;
            for (var i = 0; i < navGroup.childCount - 1; i++)       // the MH toggle (last) always stays
                navGroup.GetChild(i).gameObject.SetActive(navOpen);
        }

        static RectTransform Column(RectTransform parent, string name, Vector2 anchor, Vector2 offset, int count)
        {
            var column = Ui.Rect(name, parent).Place(anchor, offset, new Vector2(ButtonSize, count * ButtonSize + (count - 1) * Gap));
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = Gap;
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
            size.preferredWidth = ButtonSize;
            size.preferredHeight = ButtonSize;
            return button;
        }

        static void HideChild(Transform parent, string childName)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) t.gameObject.SetActive(false);
        }
    }
}
