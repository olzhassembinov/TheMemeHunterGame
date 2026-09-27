using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MemeHunter.UI
{
    public sealed class ProfileScreenPresenter : MonoBehaviour
    {
        [SerializeField] ProfilePresentationData profileData;
        [SerializeField] ProfilePhotoView profilePhoto;
        [SerializeField] TMP_Text nicknameText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] XpProgressView xpProgress;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] MemeCardView bestMemeCard;
        [SerializeField] ProfileTabBarView tabBar;
        [SerializeField] CircularActionButton backButton;
        [SerializeField] string backSceneName = "WelcomeScreen";

        public UnityEvent BackRequested = new UnityEvent();

        void Awake()
        {
            if (backButton != null)
                backButton.SetAction(GoBack);
            Bind(profileData);
        }

        public void GoBack()
        {
            BackRequested.Invoke();
            if (!string.IsNullOrWhiteSpace(backSceneName))
                SceneManager.LoadScene(backSceneName);
        }

        public void Bind(ProfilePresentationData data)
        {
            if (data == null)
                return;

            profileData = data;
            if (profilePhoto != null)
                profilePhoto.SetPhoto(data.ProfilePhoto);
            if (nicknameText != null)
                nicknameText.text = data.Nickname;
            if (subtitleText != null)
                subtitleText.text = "Age " + data.Age + ", " + data.City;
            if (levelText != null)
                levelText.text = data.Level.ToString();
            if (xpProgress != null)
                xpProgress.Bind(data.CurrentXp, data.XpForNextLevel);
            if (scoreText != null)
                scoreText.text = data.Score.ToString();
            if (bestMemeCard != null)
                bestMemeCard.Bind(data.BestMemeArtwork, data.BestMemeTitle, string.Empty, MemeHunterUiColors.CommonGrey);
        }
    }
}