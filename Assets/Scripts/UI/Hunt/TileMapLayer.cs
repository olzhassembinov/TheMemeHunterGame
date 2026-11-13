using System;
using System.Collections.Generic;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MemeHunter.UI.Hunt
{
    /// <summary>
    /// Real street map for the Map screen, placed with standard Web Mercator maths. Tiles come from Mapbox when a
    /// real public token (pk.…) is in Resources/Mapbox/MapboxConfiguration.txt, otherwise from OpenStreetMap
    /// (free for light use with attribution and an app User-Agent, per the OSM tile usage policy). Follows the player, zooms in whole
    /// levels (+ / -), pans by dragging, and Recenter snaps back to the player. A light cyan tint matches Figma.
    /// Project(lat, lng) gives the canvas position of any place, so the meme dots line up with the streets.
    /// </summary>
    public sealed class TileMapLayer : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public const int DefaultZoom = 16;                   // ~870 m across the 498-unit-wide screen in Almaty
        const int MinZoom = 12, MaxZoom = 19;
        const float TileSize = 256f;                          // canvas units per tile
        const int MaxCachedTiles = 160;
        const string OsmUrl = "https://tile.openstreetmap.org/{0}/{1}/{2}.png";
        const string MapboxUrl = "https://api.mapbox.com/styles/v1/mapbox/streets-v12/tiles/256/{0}/{1}/{2}@2x?access_token={3}";
        const string UserAgent = "MemeHunter/0.1 (university game project)";   // OSM asks apps to identify themselves

        static string mapboxToken;
        static bool sourceChosen;

        /// <summary>A real Mapbox public token from the (git-ignored) config file, or null.</summary>
        static string MapboxToken
        {
            get
            {
                if (sourceChosen) return mapboxToken;
                sourceChosen = true;
                var config = Resources.Load<TextAsset>("Mapbox/MapboxConfiguration");
                if (config == null) return null;
                try
                {
                    var token = Newtonsoft.Json.Linq.JObject.Parse(config.text).Value<string>("AccessToken");
                    if (!string.IsNullOrEmpty(token) && token.StartsWith("pk.") && token.Length > 80) mapboxToken = token;
                }
                catch (Newtonsoft.Json.JsonException) { }
                return mapboxToken;
            }
        }

        static string Credit => MapboxToken != null ? "© Mapbox © OpenStreetMap contributors" : "© OpenStreetMap contributors";

        // Shared across screens / scene loads: tiles already downloaded are not fetched again.
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        static readonly LinkedList<string> cacheOrder = new LinkedList<string>();
        static readonly HashSet<string> loading = new HashSet<string>();

        readonly Dictionary<string, RawImage> shown = new Dictionary<string, RawImage>();
        readonly Stack<RawImage> pool = new Stack<RawImage>();
        RectTransform tileRoot;
        Canvas canvas;

        int zoom = DefaultZoom;
        double centerLat = 43.2389, centerLng = 76.8897;     // Almaty until the first GPS fix
        bool followPlayer = true;

        public int Zoom => zoom;

        public static TileMapLayer Create(RectTransform parent)
        {
            var root = Ui.Rect("Street Map", parent).Stretch();
            root.SetAsFirstSibling();                         // behind the dots, the player and the buttons
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color32(232, 240, 236, 255);       // shown while tiles load
            var layer = root.gameObject.AddComponent<TileMapLayer>();
            layer.Build(root);
            return layer;
        }

        // ---------- Called by MapHud ----------

        public void SetPlayer(double lat, double lng)
        {
            if (!followPlayer) return;
            centerLat = lat;
            centerLng = lng;
        }

        public void ZoomBy(int levels) => zoom = Mathf.Clamp(zoom + levels, MinZoom, MaxZoom);

        public void Recenter()
        {
            followPlayer = true;
            zoom = DefaultZoom;
        }

        /// <summary>Canvas position (relative to the screen centre) of a place on the current map view.</summary>
        public Vector2 Project(double lat, double lng)
        {
            var p = World(lat, lng, zoom);
            var c = World(centerLat, centerLng, zoom);
            return new Vector2((float)(p.x - c.x), (float)(c.y - p.y));     // world y grows southwards
        }

        // ---------- Dragging ----------

        public void OnBeginDrag(PointerEventData eventData) => followPlayer = false;

        public void OnDrag(PointerEventData eventData)
        {
            var scale = canvas != null ? canvas.scaleFactor : 1f;
            var delta = eventData.delta / scale;
            var c = World(centerLat, centerLng, zoom);
            (centerLat, centerLng) = FromWorld(c.x - delta.x, c.y + delta.y, zoom);
        }

        // ---------- Tiles ----------

        void Update()
        {
            if (tileRoot == null) return;
            var size = tileRoot.rect.size;
            var c = World(centerLat, centerLng, zoom);
            var count = 1 << zoom;

            var minX = (int)Math.Floor((c.x - size.x / 2f) / TileSize);
            var maxX = (int)Math.Floor((c.x + size.x / 2f) / TileSize);
            var minY = Math.Max(0, (int)Math.Floor((c.y - size.y / 2f) / TileSize));
            var maxY = Math.Min(count - 1, (int)Math.Floor((c.y + size.y / 2f) / TileSize));

            var wanted = new HashSet<string>();
            for (var ty = minY; ty <= maxY; ty++)
            for (var tx = minX; tx <= maxX; tx++)
            {
                var wrappedX = ((tx % count) + count) % count;
                var key = zoom + "/" + wrappedX + "/" + ty;
                var slot = key + "@" + tx;                    // the same tile can appear twice when the map wraps
                wanted.Add(slot);

                if (!shown.TryGetValue(slot, out var image))
                {
                    image = pool.Count > 0 ? pool.Pop() : NewTileImage();
                    image.gameObject.SetActive(true);
                    shown[slot] = image;
                }
                var rect = image.rectTransform;
                rect.anchoredPosition = new Vector2((float)(tx * TileSize - c.x + TileSize / 2f), (float)(c.y - ty * TileSize - TileSize / 2f));

                if (cache.TryGetValue(key, out var texture)) { image.texture = texture; image.color = Color.white; }
                else { image.texture = null; image.color = new Color(0, 0, 0, 0); Load(key, zoom, wrappedX, ty); }
            }

            // Tiles that scrolled out of view go back to the pool.
            var gone = new List<string>();
            foreach (var pair in shown)
                if (!wanted.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (var slot in gone)
            {
                var image = shown[slot];
                shown.Remove(slot);
                image.gameObject.SetActive(false);
                pool.Push(image);
            }
        }

        static async void Load(string key, int z, int x, int y)
        {
            if (!Application.isPlaying || loading.Contains(key)) return;
            loading.Add(key);
            var token = MapboxToken;
            var url = token != null ? string.Format(MapboxUrl, z, x, y, token) : string.Format(OsmUrl, z, x, y);
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout = 15;
                if (token == null) request.SetRequestHeader("User-Agent", UserAgent);
                var op = request.SendWebRequest();
                while (!op.isDone) await Awaitable.NextFrameAsync();
                loading.Remove(key);
                if (request.result != UnityWebRequest.Result.Success) return;   // retried when it is needed again

                var texture = DownloadHandlerTexture.GetContent(request);
                texture.wrapMode = TextureWrapMode.Clamp;
                cache[key] = texture;
                cacheOrder.AddLast(key);
                while (cacheOrder.Count > MaxCachedTiles)
                {
                    var oldest = cacheOrder.First.Value;
                    cacheOrder.RemoveFirst();
                    if (cache.TryGetValue(oldest, out var old)) { Destroy(old); cache.Remove(oldest); }
                }
            }
        }

        RawImage NewTileImage()
        {
            var rect = Ui.Rect("Tile", tileRoot);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(TileSize + 0.5f, TileSize + 0.5f);   // +0.5 hides hairline seams
            return image;
        }

        void Build(RectTransform root)
        {
            canvas = GetComponentInParent<Canvas>();
            tileRoot = Ui.Rect("Tiles", root).Stretch();

            // Figma's map is washed with the app's light cyan.
            var tint = Ui.Rounded("Cyan Tint", root, MemeHunterUiColors.LightCyanBackground.WithAlpha(0.38f), 0f);
            tint.rectTransform.Stretch();
            tint.raycastTarget = false;

            // Required credit for the map data and tiles.
            var credit = Fig.Body(root, Credit, 11f, Fig.MainText.WithAlpha(0.8f));
            credit.alignment = TextAlignmentOptions.BottomLeft;
            credit.textWrappingMode = TextWrappingModes.NoWrap;
            credit.rectTransform.anchorMin = credit.rectTransform.anchorMax = credit.rectTransform.pivot = Vector2.zero;
            credit.rectTransform.anchoredPosition = new Vector2(8f, 6f);
            credit.rectTransform.sizeDelta = new Vector2(300f, 16f);
        }

        // ---------- Web Mercator ----------

        static (double x, double y) World(double lat, double lng, int z)
        {
            var scale = TileSize * (1 << z);
            var sin = Math.Sin(Math.Max(-85.05112878, Math.Min(85.05112878, lat)) * Math.PI / 180.0);
            var x = (lng + 180.0) / 360.0 * scale;
            var y = (0.5 - Math.Log((1 + sin) / (1 - sin)) / (4 * Math.PI)) * scale;
            return (x, y);
        }

        static (double lat, double lng) FromWorld(double x, double y, int z)
        {
            var scale = TileSize * (1 << z);
            var lng = x / scale * 360.0 - 180.0;
            var n = Math.PI - 2.0 * Math.PI * y / scale;
            var lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
            return (lat, lng);
        }
    }
}
