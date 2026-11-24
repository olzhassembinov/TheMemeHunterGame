using System;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace MemeHunter.UI.Onboarding
{
    /// <summary>
    /// The player's consent to the terms / privacy policy (stored on the device with the policy version)
    /// and the two phone permissions the game needs: location and camera.
    /// </summary>
    public static class Consent
    {
        /// <summary>Raise this when the policy text changes: everyone is asked to accept again.</summary>
        public const int PolicyVersion = 1;

        const string VersionKey = "ConsentPolicyVersion";
        const string AtKey = "ConsentAcceptedAt";

        public static bool Accepted => PlayerPrefs.GetInt(VersionKey, 0) >= PolicyVersion;

        public static void Accept()
        {
            PlayerPrefs.SetInt(VersionKey, PolicyVersion);
            PlayerPrefs.SetString(AtKey, DateTime.UtcNow.ToString("o"));   // when consent was given (for the record)
            PlayerPrefs.Save();
        }

        public static string PolicyText()
        {
            var asset = Resources.Load<TextAsset>("Legal/PrivacyPolicy_en");
            return asset != null ? asset.text : "Terms of use and privacy policy are missing from this build.";
        }

        // ---------- Permissions ----------

        public static bool HasLocation
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.FineLocation);
#elif UNITY_IOS && !UNITY_EDITOR
                return Input.location.isEnabledByUser && Input.location.status != LocationServiceStatus.Failed;
#else
                return true;                                      // Editor / desktop: nothing to ask
#endif
            }
        }

        public static bool HasCamera
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.Camera);
#elif UNITY_IOS && !UNITY_EDITOR
                return Application.HasUserAuthorization(UserAuthorization.WebCam);
#else
                return true;
#endif
            }
        }

        /// <summary>Shows the system dialog for location; calls back with the result.</summary>
        public static void RequestLocation(Action<bool> done)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Request(Permission.FineLocation, done);
#elif UNITY_IOS && !UNITY_EDITOR
            Input.location.Start();                               // iOS shows its dialog when location starts
            done?.Invoke(Input.location.isEnabledByUser);
#else
            done?.Invoke(true);
#endif
        }

        public static void RequestCamera(Action<bool> done)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Request(Permission.Camera, done);
#elif UNITY_IOS && !UNITY_EDITOR
            var op = Application.RequestUserAuthorization(UserAuthorization.WebCam);
            op.completed += _ => done?.Invoke(Application.HasUserAuthorization(UserAuthorization.WebCam));
#else
            done?.Invoke(true);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static void Request(string permission, Action<bool> done)
        {
            if (Permission.HasUserAuthorizedPermission(permission)) { done?.Invoke(true); return; }
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => done?.Invoke(true);
            callbacks.PermissionDenied += _ => done?.Invoke(false);
            callbacks.PermissionDeniedAndDontAskAgain += _ => done?.Invoke(false);
            Permission.RequestUserPermission(permission, callbacks);
        }
#endif
    }
}
