using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Runs a SimulationDriver in Play Mode, from the menu or in batch mode (where it also keeps Play Mode ticking).
    /// Start&lt;TDriver&gt;(scene, quit) opens the scene, enters Play Mode and creates the driver. When the driver is Done,
    /// Play Mode ends; with quit set, Unity exits with 0 = pass, 1 = fail, 2 = timed out.
    /// </summary>
    [InitializeOnLoad]
    public static class SimulationRunner
    {
        const string DriverKey = "MemeHunter.Simulation.Driver";   // driver type to create once Play Mode runs
        const string QuitKey = "MemeHunter.Simulation.Quit";
        const double TimeoutSeconds = 240;

        static double startedAt;
        static SimulationDriver driver;

        static SimulationRunner()
        {
            if (string.IsNullOrEmpty(SessionState.GetString(DriverKey, string.Empty))) return;
            startedAt = EditorApplication.timeSinceStartup;
            Debug.Log("[Simulation] hooked, playing: " + EditorApplication.isPlaying);
            // Polling (not playModeStateChanged): works the same in batch mode.
            EditorApplication.update += Tick;
        }

        public static void Start<TDriver>(string scenePath, bool quit) where TDriver : SimulationDriver
        {
            UI.Onboarding.Consent.Accept();              // no terms overlay in a simulation
            PlayerPrefs.Save();
            SessionState.SetString(DriverKey, typeof(TDriver).AssemblyQualifiedName);
            SessionState.SetBool(QuitKey, quit);
            EditorSceneManager.OpenScene(scenePath);
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            var typeName = SessionState.GetString(DriverKey, string.Empty);
            if (string.IsNullOrEmpty(typeName)) { EditorApplication.update -= Tick; return; }
            var quit = SessionState.GetBool(QuitKey, false);
            if (EditorApplication.isPlaying && Application.isBatchMode)
                EditorApplication.QueuePlayerLoopUpdate();   // batch mode does not advance Play Mode frames by itself
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;   // Play Mode can start paused (batch mode, Error Pause)

            if (EditorApplication.isPlaying && driver == null)
            {
                var type = Type.GetType(typeName);
                driver = (SimulationDriver)new GameObject("Simulation").AddComponent(type);
            }
            else if (driver != null && driver.Done)
            {
                SessionState.SetString(DriverKey, string.Empty);
                EditorApplication.update -= Tick;
                var failed = driver.Failed;
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                if (quit) EditorApplication.Exit(failed ? 1 : 0);
            }
            else if (quit && EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                Debug.LogError("[Simulation] timed out");
                EditorApplication.Exit(2);
            }
        }
    }

    /// <summary>
    /// A scripted walk through the game. Override Run and set Failed = false once everything passed.
    /// The object survives scene loads, so a walk can cross scenes. Shot(name) saves a screenshot.
    /// </summary>
    public abstract class SimulationDriver : MonoBehaviour
    {
        static readonly Vector2Int Size = new Vector2Int(498 * 2, 1080 * 2);

        public bool Done { get; private set; }
        public bool Failed { get; protected set; } = true;

        protected abstract IEnumerator Run();

        protected void Awake() => DontDestroyOnLoad(gameObject);

        protected IEnumerator Start()
        {
            Log("driver started");
            yield return Run();
            yield return null;
            Done = true;                                 // SimulationRunner leaves Play Mode (and quits in batch)
        }

        protected void Log(string message) => Debug.Log($"[{GetType().Name}] {message}");

        /// <summary>
        /// Renders the camera plus every overlay canvas (moved into camera space for one frame) to a PNG in
        /// UI_PREVIEW_DIR (or Temp/UiPreview). A scene without a camera gets a temporary one.
        /// </summary>
        protected void Shot(string name)
        {
            var cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            var temporary = cam == null;
            if (temporary)
            {
                cam = new GameObject("Shot Camera").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color32(195, 245, 255, 255);
                cam.orthographic = true;
            }

            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var texture = new RenderTexture(Size.x, Size.y, 24);
            var oldTarget = cam.targetTexture;
            cam.targetTexture = texture;
            for (var i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas || modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = cam.nearClipPlane + 0.5f + canvases[i].sortingOrder * -0.0001f;
            }
            Canvas.ForceUpdateCanvases();
            cam.Render();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = texture;
            var image = new Texture2D(Size.x, Size.y, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Size.x, Size.y), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            cam.targetTexture = oldTarget;
            for (var i = 0; i < canvases.Length; i++)
                if (canvases[i] != null) canvases[i].renderMode = modes[i];
            if (temporary) Destroy(cam.gameObject);

            var dir = Environment.GetEnvironmentVariable("UI_PREVIEW_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine("Temp", "UiPreview");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), image.EncodeToPNG());
            Destroy(image);
            texture.Release();
            Log("saved " + name);
        }
    }
}
