using System.Linq;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI.Profile
{
    /// <summary>
    /// Figma "Collections" (203:4): COLLECTIONS header, four rarity filters (the chosen one gets the dashed ring),
    /// and a scrolling 2-column grid of meme tiles. Filtering rebuilds the grid, so the remaining memes move up
    /// and fill the space (no gaps). Uncaught memes are silhouettes; dashed slots fill the last rows.
    /// </summary>
    public sealed class CollectionScreen : FigmaScreen
    {
        const float GridTop = 199f;

        static MemeRarity? filter;                     // null = all rarities (kept while the app runs)

        /// <summary>The chosen rarity filter (null = all). Settable for editor previews.</summary>
        public static MemeRarity? Filter { get => filter; set => filter = value; }

        RectTransform grid;
        Image chosenRing;
        readonly RectTransform[] filterCircles = new RectTransform[4];

        protected override void Build(RectTransform root)
        {
            // Header: COLLECTIONS in Jaro 45 between two navy lines.
            var title = Fig.Display(root, "COLLECTIONS", 45f, Fig.MainText);
            title.alignment = TextAlignmentOptions.Center;
            title.rectTransform.At(98.96f, 22f, 301.17f, 61f);
            Fig.Line(root, 0f, 52f, 108.6f, Fig.MainText);
            Fig.Line(root, 389.4f, 52f, 108.6f, Fig.MainText);

            BuildFilters(root);
            BuildGrid(root);
            CloseButton(root);
            Populate();
        }

        // ---------- Rarity filters ----------

        void BuildFilters(RectTransform root)
        {
            var theme = UiTheme.Get();

            // The dashed ring sits behind the chosen circle (77.8 px around a 58 px circle).
            chosenRing = Ui.Image("Chosen Ring", root, theme != null ? theme.rarityChosenOutline : null);
            chosenRing.rectTransform.sizeDelta = new Vector2(77.8f, 77.8f);

            var items = new (MemeRarity rarity, string label, float x, Sprite sprite, float glowScale, float labelSize)[]
            {
                (MemeRarity.Common, "Common", 26f, theme?.rarityCommon, 1f, 15f),
                (MemeRarity.Uncommon, "Uncommon", 156.9f, theme?.rarityUncommon, 1f, 15f),
                (MemeRarity.Rare, "Rare", 288.8f, theme?.rarityRare, 1.138f, 15f),
                (MemeRarity.Legendary, "Legend", 414f, theme?.rarityLegendary, 1.276f, 20f),
            };

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var color = Rarity.ColorOf(item.rarity);

                // Hit area covers circle + label.
                var hit = Ui.Rounded("Filter " + item.label, root, new Color(0, 0, 0, 0), 0f);
                hit.rectTransform.At(item.x - 8f, 88f, 74f, 92f);
                var button = hit.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                var rarity = item.rarity;
                button.onClick.AddListener(() => { filter = filter == rarity ? (MemeRarity?)null : rarity; Populate(); });

                // Circle (the Figma sprites include the glow, so rare / legendary are drawn larger around the same centre).
                var size = 58f * item.glowScale;
                var circle = Ui.Image("Circle", root, item.sprite);
                circle.rectTransform.At(item.x + 29f - size / 2f, 92f + 29f - size / 2f, size, size);
                filterCircles[i] = Ui.Rect("Centre", root).At(item.x, 92f, 58f, 58f);

                var label = Fig.Display(root, item.label, item.labelSize, color);
                label.alignment = TextAlignmentOptions.Center;
                if (item.rarity >= MemeRarity.Rare) label.Glow(item.rarity == MemeRarity.Rare ? Fig.Hex("A17BFF") : color);
                label.rectTransform.At(item.x - 10f, 152f, 78f, 22f);
            }
        }

        // ---------- Grid ----------

        void BuildGrid(RectTransform root)
        {
            // Viewport from y = 199 down to just above the close button; soft fade at the edges (Figma mask).
            var viewport = Ui.Rect("Viewport", root);
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(0f, 95f);
            viewport.offsetMax = new Vector2(0f, -GridTop);
            var mask = viewport.gameObject.AddComponent<RectMask2D>();
            mask.softness = new Vector2Int(0, 50);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);   // makes empty areas draggable

            grid = Ui.Rect("Memes", viewport);
            grid.anchorMin = new Vector2(0f, 1f);
            grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = Vector2.zero;
            grid.sizeDelta = Vector2.zero;                                     // exactly the viewport width
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(MemeTile.Width, MemeTile.Height);
            layout.spacing = new Vector2(42f, 35f);
            layout.padding = new RectOffset(36, 0, 0, 60);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = grid;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
        }

        void Populate()
        {
            for (var i = grid.childCount - 1; i >= 0; i--)
            {
                var child = grid.GetChild(i).gameObject;
                child.transform.SetParent(null);                               // leave the layout at once (no gap this frame)
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            var memes = ProfileSection.CatalogRarestFirst()
                .Where(m => filter == null || m.Rarity == filter.Value)
                .ToList();
            foreach (var meme in memes)
            {
                var count = ProfileSection.Count(meme);
                MemeTile.Card(grid, meme, count > 0, count, () =>
                {
                    if (count > 0) MemeCardScreen.Open(meme);
                    else Popup.Show("Not caught yet", "Find this meme on the map and catch it to unlock its card.");
                });
            }

            // Dashed empty slots: at least 6 slots in total, and always a full last row.
            var slots = Mathf.Max(6, memes.Count + memes.Count % 2);
            for (var i = memes.Count; i < slots; i++)
                MemeTile.Empty(grid);

            // Dashed ring around the chosen filter (hidden when all rarities are shown).
            chosenRing.gameObject.SetActive(filter != null);
            if (filter != null)
            {
                var centre = filterCircles[(int)filter.Value];
                chosenRing.rectTransform.At(centre.anchoredPosition.x + 29f - 38.9f, -centre.anchoredPosition.y + 29f - 38.9f, 77.8f, 77.8f);
            }
        }
    }
}
