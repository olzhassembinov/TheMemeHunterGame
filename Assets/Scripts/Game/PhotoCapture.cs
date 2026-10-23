using System.IO;
using UnityEngine;

namespace MemeHunter.Game
{
    public static class PhotoCapture
    {
        /// <summary>
        /// Opens the phone camera (Android/iOS) or a file picker (Unity Editor).
        /// Returns JPEG bytes, or null if the player cancelled or denied the camera permission.
        /// </summary>
        public static async Awaitable<byte[]> TakeAsync()
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
