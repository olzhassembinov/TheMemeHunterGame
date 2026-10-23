using MemeHunter.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemeHunter.Dev
{
    /// <summary>
    /// Floating "DEV" button in the top-right corner: jump to a scene, open the net/camera test panel,
    /// or wipe saved data. Created from code at app start, so no scene is edited.
    /// </summary>
    public sealed class DevOverlay : MonoBehaviour
    {
        // Scene names exactly as in Build Settings.
        static readonly string[] Scenes = { "WelcomeScreen", "Profile", "SampleScene" };

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
            var s = DevGui.Scale();
            var w = Screen.width / s;

            if (GUI.Button(new Rect(w - 62, 6, 56, 28), open ? "X" : "DEV"))
                open = !open;
            if (!open) return;

            GUILayout.BeginArea(new Rect(w - 190, 38, 184, 260));
            if (GUILayout.Button(testPanel.Visible ? "Hide net/camera test" : "Net / camera test", GUILayout.Height(30)))
            {
                testPanel.Visible = !testPanel.Visible;
                open = false;
            }
            foreach (var sceneName in Scenes)
            {
                if (GUILayout.Button(sceneName, GUILayout.Height(30)))
                {
                    open = false;
                    testPanel.Visible = false;
                    SceneManager.LoadScene(sceneName);
                }
            }
            if (GUILayout.Button("Clear saved data", GUILayout.Height(30)))
            {
                PlayerPrefs.DeleteAll();               // forget player, profile, server address override
                PlayerPrefs.Save();
                Session.Clear();
                Toast.Show("Saved data cleared");
            }
            GUILayout.EndArea();
        }
    }
}
