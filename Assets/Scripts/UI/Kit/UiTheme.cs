using TMPro;
using UnityEngine;

namespace MemeHunter.UI.Kit
{
    public enum TextWeight { Light, Regular, Bold }

    /// <summary>
    /// Fonts and sprites from the Figma design that code-built UI (popups, pickers, overlays) needs.
    /// The single instance lives in Resources/UiTheme.asset so runtime code can reach it without scene references.
    /// </summary>
    [CreateAssetMenu(menuName = "Meme Hunter/UI Theme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Fonts")]
        public TMP_FontAsset displayFont;   // Jaro: titles, headers
        public TMP_FontAsset bodyFont;      // Anuphan Regular (Figma body font); Inter until the font step ran
        public TMP_FontAsset bodyLight;     // Anuphan Light
        public TMP_FontAsset bodyBold;      // Anuphan Bold

        [Header("Buttons (Figma 'Logo / Icons')")]
        public Sprite pillButton;           // blue gradient pill, e.g. CONTINUE / Catch
        public Sprite acceptButton;         // round check
        public Sprite discardButton;        // round cross

        [Header("Round icon buttons")]
        public Sprite profileIcon;
        public Sprite mapIcon;              // also used as 'recenter on me' on the map
        public Sprite notificationsIcon;
        public Sprite collectionIcon;       // the 'XD' button
        public Sprite menuToggleIcon;       // the 'MH' button: shows / hides the side menu
        public Sprite donateIcon;           // coffee cup
        public Sprite cameraIcon;           // opens the AR hunt
        public Sprite plusIcon;
        public Sprite minusIcon;
        public Sprite showHideIcon;         // show / hide meme dots on the map

        [Header("Profile section (Figma 158:15 / 196:159 / 203:4)")]
        public Sprite coinIcon;
        public Sprite rarityCommon;
        public Sprite rarityUncommon;
        public Sprite rarityRare;
        public Sprite rarityLegendary;      // recoloured to gold
        public Sprite rarityChosenOutline;  // dashed ring around the chosen rarity filter

        [Header("Onboarding (Figma 'The start')")]
        public Sprite colorfulBackground;   // the neon blob background of Welcome / Registration / Terms / Permissions

        public TMP_FontAsset Body(TextWeight weight) => weight switch
        {
            TextWeight.Light => bodyLight != null ? bodyLight : bodyFont,
            TextWeight.Bold => bodyBold != null ? bodyBold : bodyFont,
            _ => bodyFont
        };

        static UiTheme instance;

        public static UiTheme Get()
        {
            if (instance == null)
                instance = Resources.Load<UiTheme>("UiTheme");
            if (instance == null)
                Debug.LogError("Resources/UiTheme.asset is missing: code-built UI will have no fonts or button sprites.");
            return instance;
        }
    }
}
