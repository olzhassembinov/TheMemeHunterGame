using UnityEngine;

namespace MemeHunter.Net
{
    /// <summary>
    /// The ONLY place that knows where the backend lives and how long we wait for it.
    /// </summary>
    public static class ApiConfig
    {
        // Inside the Unity Editor the backend runs on the same Mac.
        const string EditorUrl = "http://localhost:8000";

        // Default for phone builds. Until the backend is deployed we override it from the DEV test panel
        // with the Mac's Wi-Fi address (e.g. http://192.168.0.12:8000). After deployment put the real https:// address here.
        const string DeviceUrl = "https://api.example.com";

        // PlayerPrefs key where the DEV panel stores a temporary address.
        const string OverrideKey = "ApiBaseUrl";

        /// <summary>Set to false for the final demo build: hides all developer buttons.</summary>
        public const bool ShowDevTools = true;

        public const int DefaultTimeoutSeconds = 15;  // normal calls
        public const int SubmitTimeoutSeconds = 60;   // photo upload: backend waits for doskaz.kz

        /// <summary>Address without a trailing slash, e.g. http://192.168.0.12:8000</summary>
        public static string BaseUrl
        {
            get
            {
                var custom = PlayerPrefs.GetString(OverrideKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(custom))
                    return custom.Trim().TrimEnd('/');
#if UNITY_EDITOR
                return EditorUrl;
#else
                return DeviceUrl;
#endif
            }
        }

        public static void SetOverride(string url)
        {
            PlayerPrefs.SetString(OverrideKey, url ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
