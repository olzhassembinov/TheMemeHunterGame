using System;
using MemeHunter.Net;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeScreenStartGame : MonoBehaviour
{
    /// <summary>
    /// Beta build: only the player name is asked (city and birth date fields are hidden), and a player who
    /// already registered skips the form on the next launch. Elsewhere (Editor, development build) the full form:
    /// name, city, birth date.
    /// </summary>
    static bool NameOnly => ApiConfig.BetaRelease;

    static bool autoSkipDone;     // the form is skipped once per app launch (so Back from Profile can still show it)

    [SerializeField] private TMP_InputField nickname;
    [SerializeField] private TMP_InputField day;
    [SerializeField] private TMP_InputField month;
    [SerializeField] private TMP_InputField year;
    [SerializeField] private TMP_InputField city;

    void Start()
    {
        if (!NameOnly) return;

        foreach (var field in new[] { day, month, year, city })
            if (field != null) field.gameObject.SetActive(false);
        HideNamed("Have Account Button");      // no sign-in exists yet: the button did nothing

        if (!autoSkipDone && PlayerPrefs.HasKey("Nickname"))
        {
            autoSkipDone = true;
            GoToGame();
        }
    }

    public void SubmitInfo()
    {
        // Every problem gets a popup: before, a wrong field only wrote a log line and the button seemed broken.
        var name = nickname.text.Trim();
        if (name.Length < 3)
        {
            Popup.Show("Pick a nickname", "Your hunter name needs at least 3 letters.");
            return;
        }

        int intDay = 0, intMonth = 0, intYear = 0;
        var town = string.Empty;
        if (!NameOnly)
        {
            town = city.text.Trim();
            if (!int.TryParse(day.text.Trim(), out intDay) || !int.TryParse(month.text.Trim(), out intMonth) ||
                !int.TryParse(year.text.Trim(), out intYear) || !IsRealBirthday(intDay, intMonth, intYear))
            {
                Popup.Show("Check your birthday", "Please enter a real date of birth: day, month and year, for example 21 / 03 / 2007.");
                return;
            }
            if (town.Length < 3)
            {
                Popup.Show("Where do you hunt?", "Please enter your city (at least 3 letters).");
                return;
            }
        }

        PlayerPrefs.SetString("Nickname", name);
        if (!NameOnly)
        {
            PlayerPrefs.SetString("City", town);
            PlayerPrefs.SetInt("Day", intDay);
            PlayerPrefs.SetInt("Month", intMonth);
            PlayerPrefs.SetInt("Year", intYear);
        }
        PlayerPrefs.Save();

        GoToGame();
    }

    // Terms + privacy policy consent and phone permissions (when switched on), then the game (AppNav.MainScene).
    static void GoToGame()
    {
        MemeHunter.UI.Onboarding.ConsentFlow.Run(() => SceneManager.LoadScene(MemeHunter.UI.AppNav.MainScene));
    }

    void HideNamed(string objectName)
    {
        foreach (var t in transform.root.GetComponentsInChildren<Transform>(true))
            if (t.name == objectName) t.gameObject.SetActive(false);
    }

    /// <summary>A date that exists (no 31 February) and is not in the future.</summary>
    public static bool IsRealBirthday(int day, int month, int year)
    {
        var today = DateTime.Today;
        if (year < 1920 || year > today.Year || month < 1 || month > 12) return false;
        if (day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        return new DateTime(year, month, day) <= today;
    }
}
