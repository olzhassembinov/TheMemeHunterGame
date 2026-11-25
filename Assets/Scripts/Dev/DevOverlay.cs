using MemeHunter.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemeHunter.Dev
{
    /// <summary>
    /// Floating "DEV" button in the top-right corner: jump to a scene or page, open the net/camera test panel,
    /// or wipe saved data. Created from code at app start, so no scene is edited.
    /// </summary>
    public sealed class DevOverlay : MonoBehaviour
    {
        bool open;
        NetTestPanel testPanel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!ApiConfig.ShowDevTools) return;       // final build: nothing is created at all
            var go = new GameObject("DevOverlay");
            DontDestroyOnLoad(go);                      // stays visible when scenes change
            go.AddComponent<DevOverlay>();
        }

        void Awake()
        {
            testPanel = gameObject.AddComponent<NetTestPanel>();
        }

        void OnGUI()
        {
            if (DevGui.Hidden) return;

            var s = DevGui.Scale();
            var w = Screen.width / s;

            if (GUI.Button(new Rect(w - 62, 6, 56, 28), open ? "X" : "DEV"))
                open = !open;
            if (!open) return;

            GUILayout.BeginArea(new Rect(w - 190, 38, 184, 330));
            if (GUILayout.Button(testPanel.Visible ? "Hide net/camera test" : "Net / camera test", GUILayout.Height(30)))
            {
                testPanel.Visible = !testPanel.Visible;
                open = false;
            }
            // Scenes and pages. "Profile" and "Map" are pages of the Profile scene (it opens on the Map by default).
            if (Jump("WelcomeScreen")) SceneManager.LoadScene("WelcomeScreen");
            if (Jump("Profile")) UI.AppNav.OpenScreen(UI.AppNav.Profile);
            if (Jump("Map")) UI.AppNav.OpenScreen(UI.AppNav.Map);
            if (Jump("Leaderboard")) UI.AppNav.OpenScreen(UI.AppNav.Leaderboard);
            if (Jump("SampleScene (camera)")) UI.AppNav.OpenHunt(startHunting: false);
            if (GUILayout.Button("Clear saved data", GUILayout.Height(30)))
            {
                PlayerPrefs.DeleteAll();               // forget player, profile, server address override
                PlayerPrefs.Save();
                Session.Clear();
                Game.PlayerProgress.Reload();          // catches / XP and the league are cached in memory too
                Game.LocalLeague.Reset();
                Toast.Show("Saved data cleared");
            }
            GUILayout.EndArea();
        }

        /// <summary>A jump button: closes the menu, the test panel and the avatar picker when pressed.</summary>
        bool Jump(string label)
        {
            if (!GUILayout.Button(label, GUILayout.Height(30))) return false;
            open = false;
            testPanel.Visible = false;
            UI.Avatars.AvatarPicker.Close();
            return true;
        }
    }
}
