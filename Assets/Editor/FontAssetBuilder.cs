using MemeHunter.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Creates dynamic TextMeshPro font assets for Figma's body font (Anuphan Light / Regular / Bold) and puts them
    /// into Resources/UiTheme.asset. Inter is the fallback for glyphs Anuphan lacks (e.g. Cyrillic for RU / KZ).
    /// Command line: -executeMethod MemeHunter.Editor.FontAssetBuilder.Build
    /// </summary>
    public static class FontAssetBuilder
    {
        const string Folder = "Assets/mst_files/Fonts/Anuphan/";
        const string InterPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";

        [MenuItem("Meme Hunter/Fonts/Build Anuphan Font Assets")]
        public static void Build()
        {
            var inter = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(InterPath);
            var light = Create("Anuphan-Light", inter);
            var regular = Create("Anuphan-Regular", inter);
            var bold = Create("Anuphan-Bold", inter);

            var theme = Resources.Load<UiTheme>("UiTheme");
            theme.bodyLight = light;
            theme.bodyFont = regular;
            theme.bodyBold = bold;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("FontAssetBuilder: Anuphan font assets created and assigned to UiTheme.");
        }

        static TMP_FontAsset Create(string name, TMP_FontAsset fallback)
        {
            var path = Folder + name + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(Folder + name + ".ttf");
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = name + " SDF";
            AssetDatabase.CreateAsset(asset, path);

            // The atlas texture and material must be saved inside the font asset, or they are lost on reload.
            asset.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            if (fallback != null)
                asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
