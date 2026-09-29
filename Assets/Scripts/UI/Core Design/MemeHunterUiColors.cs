using UnityEngine;

namespace MemeHunter.UI
{
    public static class MemeHunterUiColors
    {
        public static readonly Color DarkNavy = Hex("262E5B");
        public static readonly Color LightCyanBackground = Hex("C3F5FF");
        public static readonly Color Teal = Hex("43E7E5");
        public static readonly Color LightPurple = Hex("A6AED7");
        public static readonly Color BrightGreen = Hex("57FF3D");
        public static readonly Color UncommonGreen = Hex("80EF8D");
        public static readonly Color CommonGrey = Hex("C2C2C2");
        public static readonly Color RarePurple = Hex("662CF8");
        public static readonly Color FormBlue = Hex("6BA6E4");

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }
    }
}