using System;
using System.IO;
using MemeHunter.Game;
using MemeHunter.UI;
using MemeHunter.UI.Avatars;
using MemeHunter.UI.Hunt;
using MemeHunter.UI.Profile;
using MemeHunter.UI.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Renders code-built UI (popups, pickers, overlays) to PNG files without entering Play Mode.
    /// Command line: Unity -batchmode -projectPath . -executeMethod MemeHunter.Editor.UiPreview.CaptureAll -quit
    /// Output folder: environment variable UI_PREVIEW_DIR, or Temp/UiPreview.
    /// </summary>
    public static class UiPreview
    {
        static readonly Vector2Int Size = new Vector2Int(498 * 2, 1080 * 2);

        [MenuItem("Meme Hunter/Preview/Capture Code-Built UI")]
        public static void CaptureAll()
        {
            Capture("avatar_picker", () => AvatarPicker.Open());
            Capture("popup_no_internet", () => HuntErrors.NoInternet());
            Capture("hud_idle", () => HuntHud.Create());
            Capture("hud_walking", () => { var h = HuntHud.Create(); h.SetRarity("rare"); h.SetPhase(HuntPhase.Walking); h.SetDistance(124); h.SetTarget(43.24, 76.89); });
            Capture("hud_near", () => { var h = HuntHud.Create(); h.SetRarity("legendary"); h.SetPhase(HuntPhase.Near); });
            Capture("catch_reveal", () =>
            {
                var meme = MemeCatalog.ForCard("preview", "legendary");
                CatchReveal.Show(new PlayerProgress.CatchResult(meme, true, 100, true), "legendary", 40);
                ShowRevealFront();
            });
            CaptureMap();
            PlayerProgress.UsePreviewData(320, ("B-HOPPING", 2), ("DOGE", 1), ("GIGACHAD", 1));
            CaptureScreen<ProfileScreen>("profile");
            CollectionScreen.Filter = null;
            CaptureScreen<CollectionScreen>("collections");
            CollectionScreen.Filter = MemeRarity.Rare;
            CaptureScreen<CollectionScreen>("collections_rare");
            CollectionScreen.Filter = null;
            MemeCardScreen.Select(MemeCatalog.Find("B-HOPPING"));
            CaptureScreen<MemeCardScreen>("meme_card");
            Capture("terms", () => MemeHunter.UI.Onboarding.ConsentFlow.ShowTerms(null));
            Capture("permissions", () => MemeHunter.UI.Onboarding.ConsentFlow.ShowPermissions(null));
            Capture("donate", () => DonateScreen.Open());
            Capture("donate_thanks", () => DonateScreen.OpenThanks());
        }

        static void CaptureScreen<T>(string file) where T : FigmaScreen
        {
            Capture(file, () =>
            {
                var canvas = Ui.OverlayCanvas("Preview " + file, 10);
                Ui.Rect(typeof(T).Name, canvas).Stretch().gameObject.AddComponent<T>().Render();
            });
        }

        /// <summary>The designed Map screen from the Profile scene with the working HUD and three fake memes.</summary>
        static void CaptureMap()
        {
            Capture("map", () =>
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Profile.unity", OpenSceneMode.Additive);
                foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "Persistent UI") t.gameObject.SetActive(false);      // hidden at runtime by ProfileSection
                foreach (var view in UnityEngine.Object.FindObjectsByType<ScreenView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    view.gameObject.SetActive(view.ScreenId == AppNav.Map);
                    if (view.ScreenId != AppNav.Map) continue;
                    const double lat = 43.2389, lng = 76.8897;
                    MapHud.PreviewOn(view, lat, lng, new[]
                    {
                        new MemeHunter.Net.NearbyCardDto { Id = "a", Rarity = "legendary", ApproxLat = lat + 0.0006, ApproxLng = lng + 0.0004 },
                        new MemeHunter.Net.NearbyCardDto { Id = "b", Rarity = "rare", ApproxLat = lat - 0.0011, ApproxLng = lng - 0.0013 },
                        new MemeHunter.Net.NearbyCardDto { Id = "c", Rarity = "common", ApproxLat = lat + 0.0016, ApproxLng = lng - 0.0018 },
                    });
                }
            });
        }

        /// <summary>The flip animation does not run in edit mode: show the card front and the rewards directly.</summary>
        static void ShowRevealFront()
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "Front") t.gameObject.SetActive(true);
                if (t.name == "Back") t.gameObject.SetActive(false);
                if (t.name == "Stage") t.localScale = Vector3.one;
            }
            foreach (var popup in UnityEngine.Object.FindObjectsByType<Popup>(FindObjectsSortMode.None))
                popup.Body.alpha = 1f;
        }

        public static void Capture(string fileName, Action build)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(195, 245, 255, 255);   // the light cyan screen behind popups
            camera.orthographic = true;

            build();

            // Overlay canvases do not render into cameras: switch them to camera space for the capture.
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
            }
            foreach (var popup in UnityEngine.Object.FindObjectsByType<Popup>(FindObjectsSortMode.None))
                popup.Card.localScale = Vector3.one;                     // the pop-in animation does not run in edit mode

            var texture = new RenderTexture(Size.x, Size.y, 24);
            camera.targetTexture = texture;
            camera.aspect = (float)Size.x / Size.y;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Canvas.ForceUpdateCanvases();                                 // second pass: layout groups settle after one update
            camera.Render();

            RenderTexture.active = texture;
            var image = new Texture2D(Size.x, Size.y, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Size.x, Size.y), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            var dir = Environment.GetEnvironmentVariable("UI_PREVIEW_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine("Temp", "UiPreview");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName + ".png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log("UiPreview saved " + Path.GetFullPath(path));

            UnityEngine.Object.DestroyImmediate(image);
            camera.targetTexture = null;
            texture.Release();
        }
    }
}
