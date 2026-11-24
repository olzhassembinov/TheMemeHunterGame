using System.Collections;
using System.Linq;
using System.Reflection;
using MemeHunter.UI;
using MemeHunter.UI.Kit;
using MemeHunter.UI.Onboarding;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemeHunter.Editor
{
    /// <summary>
    /// The first launch: the registration form (wrong nickname, impossible birthday, then valid data), the Terms of use
    /// (CONTINUE only after the tick), the Allow geolocation &amp; camera screen, and the Map.
    /// The Editor's own saved profile and consent are put back afterwards.
    /// Batch: Unity -batchmode -projectPath . -executeMethod MemeHunter.Editor.OnboardingSimulation.Run (env UI_PREVIEW_DIR).
    /// Also in the menu: Meme Hunter / Simulate onboarding.
    /// </summary>
    public static class OnboardingSimulation
    {
        const string Scene = "Assets/Scenes/WelcomeScreen.unity";

        public static void Run() => SimulationRunner.Start<OnboardingDriver>(Scene, quit: true);

        [MenuItem("Meme Hunter/Simulate onboarding")]
        static void RunFromMenu() => SimulationRunner.Start<OnboardingDriver>(Scene, quit: false);
    }

    public sealed class OnboardingDriver : SimulationDriver
    {
        protected override bool AcceptTerms => false;

        int failures;

        protected override IEnumerator Run()
        {
            PlayerPrefs.DeleteKey("ConsentPolicyVersion");              // a new player: terms not accepted yet
            PlayerPrefs.DeleteKey("Nickname");
            PlayerPrefs.Save();
            yield return Wait(1.5f);

            var form = FindFirstObjectByType<WelcomeScreenStartGame>(FindObjectsInactive.Include);
            Check(form != null, "registration form found");
            if (form == null) { Failed = true; yield break; }

            // 1) A two-letter nickname: a popup, nothing saved, still on Welcome.
            Fill(form, "Al", "21", "3", "2007", "Almaty");
            form.SubmitInfo();
            yield return Wait(0.5f);
            Check(PopupTitle() == "Pick a nickname" && !PlayerPrefs.HasKey("Nickname"), "short nickname: popup, nothing saved");
            Shot("onboard_1_short_name");
            ClosePopups();

            // 2) 31 February.
            Fill(form, "TestHunter", "31", "2", "2007", "Almaty");
            form.SubmitInfo();
            yield return Wait(0.5f);
            Check(PopupTitle() == "Check your birthday", "31 February: popup");
            ClosePopups();

            // 3) A birthday in the future.
            Fill(form, "TestHunter", "1", "1", (System.DateTime.Today.Year + 1).ToString(), "Almaty");
            form.SubmitInfo();
            yield return Wait(0.5f);
            Check(PopupTitle() == "Check your birthday", "birthday in the future: popup");
            ClosePopups();

            // 4) Valid data: the Terms of use.
            Fill(form, "  TestHunter ", "21", "3", "2007", "Almaty");
            form.SubmitInfo();
            yield return Wait(0.5f);
            Check(ConsentFlow.IsOpen && GameObject.Find("Terms Screen") != null, "Terms of use shown");
            Check(PlayerPrefs.GetString("Nickname") == "TestHunter", "nickname saved without the spaces");
            Shot("onboard_2_terms");

            var proceed = FindButton("Continue");
            Check(proceed != null && !proceed.interactable, "CONTINUE disabled until the tick");
            BackButton.Handle();                                         // Android Back must not skip the terms
            yield return null;
            Check(GameObject.Find("Terms Screen") != null, "Back does not close the Terms");
            Press("Tick");
            Check(proceed != null && proceed.interactable, "CONTINUE enabled after the tick");
            Press("Continue");
            yield return Wait(0.5f);
            Check(Consent.Accepted && GameObject.Find("Permissions Screen") != null, "terms accepted, Permissions shown");
            Shot("onboard_3_permissions");

            // 5) Permissions (the Editor has nothing to ask) -> the Map.
            Press("Continue");
            yield return Wait(2.5f);
            var visible = FindObjectsByType<ScreenView>(FindObjectsSortMode.None)
                .Where(v => v.gameObject.activeInHierarchy).Select(v => v.ScreenId).ToList();
            Check(SceneManager.GetActiveScene().name == AppNav.MainScene && visible.Count == 1 && visible[0] == AppNav.Map,
                $"the Map after onboarding (scene {SceneManager.GetActiveScene().name}, on screen: {string.Join(", ", visible)})");
            Check(!ConsentFlow.IsOpen, "no consent screen over the Map");
            Shot("onboard_4_map");

            Failed = failures > 0;
            Log(Failed ? $"FAIL: {failures} check(s) failed" : "PASS: registration, terms, permissions, Map");
        }

        static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        void Check(bool ok, string what)
        {
            if (!ok) failures++;
            Log($"{(ok ? "ok" : "FAIL")}: {what}");
        }

        static void Fill(WelcomeScreenStartGame form, string nickname, string day, string month, string year, string city)
        {
            Field(form, "nickname").text = nickname;
            Field(form, "day").text = day;
            Field(form, "month").text = month;
            Field(form, "year").text = year;
            Field(form, "city").text = city;
        }

        static TMP_InputField Field(WelcomeScreenStartGame form, string name) =>
            (TMP_InputField)typeof(WelcomeScreenStartGame).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);

        static string PopupTitle()
        {
            var popup = FindFirstObjectByType<Popup>();
            return popup != null && popup.Title != null ? popup.Title.text : null;
        }

        static void ClosePopups()
        {
            foreach (var popup in FindObjectsByType<Popup>(FindObjectsSortMode.None)) popup.Close();
        }

        static Button FindButton(string name) =>
            FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);

        void Press(string name)
        {
            var button = FindButton(name);
            if (button == null || !button.interactable) { Check(false, $"button '{name}' can be pressed"); return; }
            button.onClick.Invoke();
            Log("pressed " + name);
        }
    }
}
