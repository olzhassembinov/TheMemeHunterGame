using System;
using MemeHunter.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeScreenStartGame : MonoBehaviour
{
    [SerializeField] private TMP_InputField nickname;
    [SerializeField] private TMP_InputField day;
    [SerializeField] private TMP_InputField month;
    [SerializeField] private TMP_InputField year;
    [SerializeField] private TMP_InputField city;

    public void SubmitInfo()
    {
        // Every problem gets a popup: before, a wrong field only wrote a log line and the button seemed broken.
        var name = nickname.text.Trim();
        var town = city.text.Trim();
        if (name.Length < 3)
        {
            Popup.Show("Pick a nickname", "Your hunter name needs at least 3 letters.");
            return;
        }
        if (!int.TryParse(day.text.Trim(), out var intDay) || !int.TryParse(month.text.Trim(), out var intMonth) ||
            !int.TryParse(year.text.Trim(), out var intYear) || !IsRealBirthday(intDay, intMonth, intYear))
        {
            Popup.Show("Check your birthday", "Please enter a real date of birth: day, month and year, for example 21 / 03 / 2007.");
            return;
        }
        if (town.Length < 3)
        {
            Popup.Show("Where do you hunt?", "Please enter your city (at least 3 letters).");
            return;
        }

        PlayerPrefs.SetString("Nickname", name);
        PlayerPrefs.SetString("City", town);
        PlayerPrefs.SetInt("Day", intDay);
        PlayerPrefs.SetInt("Month", intMonth);
        PlayerPrefs.SetInt("Year", intYear);
        PlayerPrefs.Save();

        // Terms + privacy policy consent and phone permissions, then the game (the Map).
        MemeHunter.UI.Onboarding.ConsentFlow.Run(() => SceneManager.LoadScene("Profile"));
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
