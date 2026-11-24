using MemeHunter.UI.Avatars;
using MemeHunter.UI.Kit;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MemeHunter.UI
{
    /// <summary>
    /// Android's Back button (Escape in the Editor). It closes the open popup or overlay first, otherwise it goes
    /// Back like the round close buttons; on the Map (the home screen) and on the Welcome screen it puts the app in
    /// the background, like other Android apps. The project uses the Input System only, which does not handle
    /// Back by itself, so before this the button did nothing anywhere.
    /// </summary>
    public sealed class BackButton : MonoBehaviour
    {
        const string WelcomeScene = "WelcomeScreen";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("Back Button");
            DontDestroyOnLoad(go);
            go.AddComponent<BackButton>();
        }

        void Update()
        {
            if (Pressed()) Handle();
        }

        static bool Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>What one press of Back does (public for the editor simulation).</summary>
        public static void Handle()
        {
            if (AvatarPicker.IsOpen) { AvatarPicker.Close(); return; }          // cancel: Profile asks again next time
            var popup = FindFirstObjectByType<Popup>();
            if (popup != null) { popup.Close(); return; }
            if (DonateScreen.CloseIfOpen()) return;
            if (Onboarding.ConsentFlow.IsOpen) return;                            // the terms are answered with their buttons

            var scene = SceneManager.GetActiveScene().name;
            if (scene == WelcomeScene) { MoveToBackground(); return; }
            if (scene == AppNav.HuntScene) { AppNav.Back(); return; }             // camera -> the Map

            var navigator = AppNav.ActiveNavigator;
            if (navigator != null && navigator.ActiveScreen != null && navigator.ActiveScreen.ScreenId != AppNav.Map)
            {
                AppNav.Back();
                return;
            }
            MoveToBackground();
        }

        static void MoveToBackground()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call<bool>("moveTaskToBack", true);
#endif
        }
    }
}
