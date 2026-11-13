using TMPro;
using System;
using System.Linq;
using MemeHunter.Game;
using MemeHunter.Net;
using MemeHunter.UI.Avatars;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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
            BindLive();
            MakePhotoTappable();
            if (tabBar != null)
                tabBar.gameObject.SetActive(false);            // no social features: I AM / FRIENDS / GLOBAL removed
        }

        void OnEnable()
        {
            AvatarCatalog.Changed += ApplyAvatar;
            Session.PlayerChanged += OnPlayerChanged;
            PlayerProgress.Changed += BindLive;
            BindLive();                                         // returning to Profile after a catch
        }

        void OnDisable()
        {
            AvatarCatalog.Changed -= ApplyAvatar;
            Session.PlayerChanged -= OnPlayerChanged;
            PlayerProgress.Changed -= BindLive;
        }

        void OnPlayerChanged(PlayerDto _) => BindLive();

        /// <summary>
        /// Real values over the sample asset: onboarding answers (device), level/XP and catches (PlayerProgress),
        /// coins (server). The sample data stays only where nothing real exists yet.
        /// </summary>
        void BindLive()
        {
            if (nicknameText != null && PlayerPrefs.HasKey("Nickname"))
                nicknameText.text = PlayerPrefs.GetString("Nickname");
            if (subtitleText != null && PlayerPrefs.HasKey("Year"))
                subtitleText.text = "Age " + AgeFromBirthday() + ", " + PlayerPrefs.GetString("City", string.Empty);

            if (levelText != null)
                levelText.text = PlayerProgress.Level.ToString();
            if (xpProgress != null)
            {
                var (current, needed) = PlayerProgress.LevelProgress();
                xpProgress.Bind(current, needed);
            }
            if (scoreText != null)
                scoreText.text = Session.Player != null ? Session.Player.CurrencyBalance.ToString() : "0";   // Score = coins

            if (bestMemeCard != null)
            {
                // The rarest meme the player owns (the newest one if several share that rarity).
                var best = PlayerProgress.Caught
                    .Select(c => MemeCatalog.Find(c.memeId))
                    .Where(m => m != null)
                    .OrderByDescending(m => m.Rarity)
                    .FirstOrDefault();
                bestMemeCard.gameObject.SetActive(best != null);
                if (best != null)
                    bestMemeCard.Bind(best);
            }
        }

        static int AgeFromBirthday()
        {
            var today = DateTime.Today;
            var born = new DateTime(PlayerPrefs.GetInt("Year", today.Year), Mathf.Clamp(PlayerPrefs.GetInt("Month", 1), 1, 12), 1);
            var age = today.Year - born.Year;
            if (today.Month < born.Month || (today.Month == born.Month && today.Day < PlayerPrefs.GetInt("Day", 1)))
                age--;
            return Mathf.Max(0, age);
        }

        async void Start()
        {
            // First visit after onboarding: let the player pick a face right away.
            if (!AvatarCatalog.HasChoice)
                AvatarPicker.Open();

            // Fresh coin balance from the server (Profile is the home screen).
            if (Session.HasPlayer)
            {
                try { Session.SetPlayer(await Api.GetPlayer(Session.PlayerId)); }
                catch (ApiException) { /* offline: keep the last known values */ }
            }
        }

        /// <summary>Tapping the profile photo opens the avatar picker.</summary>
        void MakePhotoTappable()
        {
            if (profilePhoto == null)
                return;
            var graphic = profilePhoto.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;
            // Explicit null check: Unity's fake-null objects do not work with '??'.
            if (!profilePhoto.TryGetComponent<Button>(out var button))
                button = profilePhoto.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => AvatarPicker.Open());
        }

        void ApplyAvatar(Sprite avatar)
        {
            if (profilePhoto != null && avatar != null)
                profilePhoto.SetPhoto(avatar);
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
                profilePhoto.SetPhoto(AvatarCatalog.Selected != null ? AvatarCatalog.Selected : data.ProfilePhoto);
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