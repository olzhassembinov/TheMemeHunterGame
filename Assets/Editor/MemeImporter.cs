using System.IO;
using System.Linq;
using MemeHunter.UI;
using UnityEditor;
using UnityEngine;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Turns the pictures dropped into Assets/MemeInbox into memes. Files are named "001_Name.png" (the number
    /// picks the rarity from the lists below) or "<rarity>_<Name>.png" (rarity = common, rare, epic or legendary).
    /// Each picture is moved to Assets/mst_files/Pictures/Memes,
    /// set up as a sprite, and gets a MemeData in Assets/Resources/Memes. Existing memes are never overwritten.
    /// </summary>
    public static class MemeImporter
    {
        const string InboxFolder = "Assets/MemeInbox";
        const string PicturesFolder = "Assets/mst_files/Pictures/Memes";
        const string DataFolder = "Assets/Resources/Memes";
        const int MaxTextureSize = 1024;        // keeps the app small: a meme card is never shown bigger
        const int DefaultPrice = 35;            // same range as the first memes (34-37)

        [MenuItem("Meme Hunter/Import memes from MemeInbox")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(InboxFolder))
            {
                Debug.LogWarning("[MemeImporter] Folder Assets/MemeInbox does not exist.");
                return;
            }

            var created = 0;
            var skipped = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { InboxFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Import(path)) created++; else skipped++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MemeImporter] Imported {created}, skipped {skipped}. {CountsText()}");
        }

        static bool Import(string path)
        {
            var file = Path.GetFileNameWithoutExtension(path);
            var split = file.IndexOf('_');
            if (split <= 0 || split == file.Length - 1)
                return Skip(path, "name must look like 001_Name.png or rarity_Name.png");

            var prefix = file.Substring(0, split);
            MemeRarity rarity;
            if (int.TryParse(prefix, out var number))
                rarity = RarityOfNumber(number);
            else if (!TryParseRarity(prefix, out rarity))
                return Skip(path, "start the name with a number (001_) or a rarity (common, rare, epic, legendary)");

            var name = file.Substring(split + 1).Replace('_', ' ').Trim();
            var id = MakeId(name);
            if (id.Length == 0)
                return Skip(path, "the name has no letters or digits");

            var dataPath = DataFolder + "/" + id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<MemeData>(dataPath) != null)
                return Skip(path, "a meme called " + id + " already exists");

            var target = PicturesFolder + "/" + Path.GetFileName(path);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(target) != null)
                return Skip(path, "a picture with this file name already exists in " + PicturesFolder);

            var error = AssetDatabase.MoveAsset(path, target);
            if (!string.IsNullOrEmpty(error))
                return Skip(path, error);

            var importer = AssetImporter.GetAtPath(target) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = MaxTextureSize;
                importer.SaveAndReimport();
            }

            var data = ScriptableObject.CreateInstance<MemeData>();
            AssetDatabase.CreateAsset(data, dataPath);
            var serialized = new SerializedObject(data);
            serialized.FindProperty("memeId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = name.ToUpperInvariant();
            serialized.FindProperty("rarity").enumValueIndex = (int)rarity;
            serialized.FindProperty("price").intValue = DefaultPrice;
            serialized.FindProperty("artwork").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(target);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return true;
        }

        static bool Skip(string path, string reason)
        {
            Debug.LogWarning($"[MemeImporter] Skipped {path}: {reason}.");
            return false;
        }

        // Beta set: the numbered pictures 001-030 get their rarity from these lists (12 common, 9 rare, 6 epic,
        // 3 legendary). A number that is in none of the lists is common. Change the lists to change a rarity.
        static readonly int[] LegendaryNumbers = { 1, 3, 6 };
        static readonly int[] EpicNumbers = { 2, 4, 9, 21, 23, 27 };
        static readonly int[] RareNumbers = { 5, 7, 8, 10, 11, 18, 19, 22, 25 };

        static MemeRarity RarityOfNumber(int number)
        {
            if (System.Array.IndexOf(LegendaryNumbers, number) >= 0) return MemeRarity.Legendary;
            if (System.Array.IndexOf(EpicNumbers, number) >= 0) return MemeRarity.Uncommon;   // the game's Uncommon tier is Epic
            if (System.Array.IndexOf(RareNumbers, number) >= 0) return MemeRarity.Rare;
            return MemeRarity.Common;
        }

        // "epic" is the server's name for the game's Uncommon tier.
        static bool TryParseRarity(string text, out MemeRarity rarity)
        {
            switch (text.Trim().ToLowerInvariant())
            {
                case "common": rarity = MemeRarity.Common; return true;
                case "rare": rarity = MemeRarity.Rare; return true;
                case "epic":
                case "uncommon": rarity = MemeRarity.Uncommon; return true;
                case "legendary": rarity = MemeRarity.Legendary; return true;
                default: rarity = MemeRarity.Common; return false;
            }
        }

        /// <summary>"Distracted Boyfriend" becomes "DISTRACTED-BOYFRIEND".</summary>
        static string MakeId(string name)
        {
            var id = new string(name.ToUpperInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
            while (id.Contains("--")) id = id.Replace("--", "-");
            return id.Trim('-');
        }

        static string CountsText()
        {
            var all = Resources.LoadAll<MemeData>("Memes");
            var parts = System.Enum.GetValues(typeof(MemeRarity)).Cast<MemeRarity>()
                .Select(r => Rarity.Name(r) + " " + all.Count(m => m.Rarity == r));
            return "Memes in the game now: " + all.Length + " (" + string.Join(", ", parts) + ").";
        }
    }
}
