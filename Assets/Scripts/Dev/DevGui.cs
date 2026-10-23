using UnityEngine;

namespace MemeHunter.Dev
{
    public static class DevGui
    {
        /// <summary>
        /// OnGUI draws in pixels, which are tiny on a 400 dpi phone. Call this first inside OnGUI:
        /// it scales everything so a button is finger-sized, and returns the scale used.
        /// </summary>
        public static float Scale()
        {
            var s = Mathf.Clamp(Screen.dpi > 0 ? Screen.dpi / 160f : 2f, 1f, 4f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            return s;
        }
    }
}
