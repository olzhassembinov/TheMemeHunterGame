using System.Collections.Generic;
using System.IO;
using MemeHunter.Dev;
using MemeHunter.Net;
using UnityEngine;

namespace MemeHunter.Game
{
    public static class PhotoCapture
    {
        const int JpegQuality = 85;

        /// <summary>
        /// The photo for the hunt.
        /// Phone: the live AR view is the viewfinder: all UI is hidden for one frame and the screen is captured.
        /// Editor: a file picker (the Editor has no camera).
        /// Set ApiConfig.UseNativeCamera = true to open the phone's camera app instead.
        /// Returns JPEG bytes, or null if nothing was captured.
        /// </summary>
        public static async Awaitable<byte[]> TakeAsync()
        {
#if UNITY_EDITOR
            return await TakeNativeAsync();
#else
            if (ApiConfig.UseNativeCamera)
                return await TakeNativeAsync();
            return await CaptureViewfinderAsync();
#endif
        }

        /// <summary>
        /// In-app shutter: hides every Canvas and the DEV panels for one frame, takes a screenshot of the
        /// camera image, shows them again and plays a short flash.
        /// </summary>
        static async Awaitable<byte[]> CaptureViewfinderAsync()
        {
            var hidden = new List<Canvas>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!canvas.enabled) continue;
                canvas.enabled = false;
                hidden.Add(canvas);
            }
            DevGui.Hidden = true;
            MemeSpawner.SetPhotoMode(true);          // the virtual meme must not be in the real photo

            Texture2D shot = null;
            try
            {
                await Awaitable.NextFrameAsync();      // one frame is drawn without any UI
                await Awaitable.EndOfFrameAsync();     // wait until that frame is completely rendered
                shot = ScreenCapture.CaptureScreenshotAsTexture();
            }
            finally
            {
                foreach (var canvas in hidden)
                    if (canvas != null) canvas.enabled = true;
                DevGui.Hidden = false;
                MemeSpawner.SetPhotoMode(false);
            }

            if (shot == null)
                return null;

            ShutterFlash.Play();
            var jpeg = shot.EncodeToJPG(JpegQuality);
            Object.Destroy(shot);
            return jpeg;
        }

        /// <summary>
        /// Opens the phone's camera app (Android/iOS) or a file picker (Unity Editor).
        /// Returns JPEG bytes, or null if the player cancelled or denied the camera permission.
        /// </summary>
        public static async Awaitable<byte[]> TakeNativeAsync()
        {
#if UNITY_EDITOR
            // The Editor has no camera: pick any .jpg from disk instead.
            await Awaitable.NextFrameAsync();
            var picked = UnityEditor.EditorUtility.OpenFilePanel("Pick a test photo (jpg)", "", "jpg");
            return string.IsNullOrEmpty(picked) ? null : File.ReadAllBytes(picked);
#else
            string path = null;
            var done = false;

            // NativeCamera asks for the camera permission itself, opens the camera app and calls us back
            // on the main thread. path is null when the player cancelled or denied the permission.
            // maxSize 1600: shrink to at most 1600 px so the upload is a few hundred KB, not several MB.
            NativeCamera.TakePicture(p => { path = p; done = true; }, 1600);

            while (!done)
                await Awaitable.NextFrameAsync();

            if (string.IsNullOrEmpty(path))
                return null;

            var bytes = File.ReadAllBytes(path);
            try { File.Delete(path); } catch { /* temp file, ignore */ }
            return bytes;
#endif
        }
    }
}
