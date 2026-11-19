using MemeHunter.Net;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeScreenStartGame : MonoBehaviour
{
    /// <summary>
    /// Beta round 1: only the player name is asked (city and birth date fields are hidden), and a player who
    /// already registered skips the form on the next launch. false = the full form (name, city, birth date).
    /// </summary>
    const bool NameOnly = true;

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
        var playerName = nickname.text.Trim();
        if (playerName.Length < 3)
        {
            Toast.Show("Please enter a name with at least 3 letters.");
            return;
        }

        int intDay = 0, intMonth = 0, intYear = 0;
        if (!NameOnly)
        {
            if (!int.TryParse(day.text, out intDay) || !int.TryParse(month.text, out intMonth) || !int.TryParse(year.text, out intYear))
            {
                Toast.Show("Please enter your birth date as numbers.");
                return;
            }
            if (intDay < 1 || intDay > 31 || intMonth < 1 || intMonth > 12 || intYear < 1950 || intYear > 2026 || city.text.Length < 3)
            {
                Toast.Show("Please check your birth date and city.");
                return;
            }
        }

        PlayerPrefs.SetString("Nickname", playerName);
        if (!NameOnly)
        {
            PlayerPrefs.SetString("City", city.text);
            PlayerPrefs.SetInt("Day", intDay);
            PlayerPrefs.SetInt("Month", intMonth);
            PlayerPrefs.SetInt("Year", intYear);
        }
        PlayerPrefs.Save();

        GoToGame();
    }

    // Terms + privacy policy consent and phone permissions (when switched on), then the game (the Map).
    static void GoToGame()
    {
        MemeHunter.UI.Onboarding.ConsentFlow.Run(() => SceneManager.LoadScene(MemeHunter.UI.AppNav.MainScene));
    }

    void HideNamed(string objectName)
    {
        foreach (var t in transform.root.GetComponentsInChildren<Transform>(true))
            if (t.name == objectName) t.gameObject.SetActive(false);
    }
}
