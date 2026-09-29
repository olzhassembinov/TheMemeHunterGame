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
        if(!int.TryParse(day.text, out int intDay) || !int.TryParse(month.text, out int intMonth) || !int.TryParse(year.text, out int intYear))
        {
            Debug.LogWarning("Parse error!");
            return;
        }
        if (intDay<1 || intDay > 31 || intMonth < 1 || intMonth > 12 || intYear < 1950 || intYear > 2026 || nickname.text.Length < 3 || city.text.Length < 3)
        {
            Debug.LogWarning("Value error!");
            return;
        }

        PlayerPrefs.SetString("Nickname", nickname.text);
        PlayerPrefs.SetString("City", city.text);
        PlayerPrefs.SetInt("Day", intDay);
        PlayerPrefs.SetInt("Month", intMonth);
        PlayerPrefs.SetInt("Year", intYear);
        Debug.Log("It worked!");

        // now teleport the player somewhere. . .
        SceneManager.LoadScene("Profile");
    }

}