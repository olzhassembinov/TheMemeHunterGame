using System;
using System.Linq;
using UnityEngine;

namespace MemeHunter.UI.Avatars
{
    /// <summary>
    /// The 8 player avatars from Figma (Resources/Avatars/Avatar1..8) and the player's choice (saved on the device).
    /// </summary>
    public static class AvatarCatalog
    {
        const string ChoiceKey = "AvatarIndex";
        static Sprite[] avatars;

        /// <summary>Fires after the player picks a different avatar.</summary>
        public static event Action<Sprite> Changed;

        public static Sprite[] All
        {
            get
            {
                if (avatars == null)
                {
                    // Sort by the number in the name so the order matches Figma (Avatar1 .. Avatar8).
                    avatars = Resources.LoadAll<Sprite>("Avatars")
                        .OrderBy(s => int.TryParse(s.name.Replace("Avatar", string.Empty), out var n) ? n : 99)
                        .ToArray();
                }
                return avatars;
            }
        }

        public static bool HasChoice => PlayerPrefs.HasKey(ChoiceKey);

        /// <summary>Chosen index, or -1 if the player has not chosen yet.</summary>
        public static int SelectedIndex => PlayerPrefs.GetInt(ChoiceKey, -1);

        /// <summary>The chosen avatar, or null if none was chosen yet.</summary>
        public static Sprite Selected
        {
            get
            {
                var index = SelectedIndex;
                return index >= 0 && index < All.Length ? All[index] : null;
            }
        }

        public static void Select(int index)
        {
            if (index < 0 || index >= All.Length)
                return;
            var changed = index != SelectedIndex;
            PlayerPrefs.SetInt(ChoiceKey, index);
            PlayerPrefs.Save();
            if (changed)
                Changed?.Invoke(All[index]);
        }
    }
}
