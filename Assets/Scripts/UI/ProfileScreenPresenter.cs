using TMPro;
using Unity.Tutorials.Editor;
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
        [SerializeField] TMP_Text moneyText;
        private MemeCardView[] bestMemeCards = new MemeCardView[4];
        [SerializeField] ProfileTabBarView tabBar;
        [SerializeField] CircularActionButton backButton;
        [SerializeField] string backSceneName = "WelcomeScreen";

        [SerializeField] private GameObject bestMemeCardPrefab;
        [SerializeField] private GameObject emptyBestMemeCardPrefab;

        [SerializeField] private Transform cardContent;

        public UnityEvent BackRequested = new UnityEvent();

        private Vector2[] bestCardsPosition = {new Vector2(-117,0), new Vector2(117,0), new Vector2(-117,-324), new Vector2(117, -324)};

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
            if (moneyText != null)
                moneyText.text = data.Money.ToString();
            for(int i = 0; i<4; i++)
            {
                if(data.BestMemeArtworks[i]!=null || !data.BestMemeTitles[i].IsNullOrEmpty()){
                    GameObject gameObj = Instantiate(bestMemeCardPrefab, cardContent);
                    gameObj.GetComponent<RectTransform>().anchoredPosition = bestCardsPosition[i];
                    bestMemeCards[i] = gameObj.GetComponent<MemeCardView>();
                    bestMemeCards[i].Bind(data.BestMemeArtworks[i], data.BestMemeTitles[i], string.Empty, MemeHunterUiColors.CommonGrey);
                }
                else{
                    GameObject gameObj = Instantiate(emptyBestMemeCardPrefab, cardContent);
                    gameObj.GetComponent<RectTransform>().anchoredPosition = bestCardsPosition[i];
                    bestMemeCards[i] = gameObj.GetComponent<MemeCardView>();
                    bestMemeCards[i].Bind();
                }
            }
        }
    }
}