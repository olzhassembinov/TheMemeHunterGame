using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    public sealed class CollectionScreenPresenter : MonoBehaviour
    {
        [SerializeField] MemeCollectionData collectionData;
        [SerializeField] MemeCardView[] memeCards = Array.Empty<MemeCardView>();
        [SerializeField] GameObject[] emptySlots = Array.Empty<GameObject>();
        [SerializeField] Button[] rarityButtons = Array.Empty<Button>();
        [SerializeField] MemeRarity[] buttonRarities = Array.Empty<MemeRarity>();
        [SerializeField] ScreenNavigator navigator;
        [SerializeField] MemeDetailPresenter detailPresenter;
        [SerializeField] Color selectedColor = MemeHunterUiColors.DarkNavy;
        [SerializeField] Color unselectedColor = MemeHunterUiColors.LightPurple;

        MemeRarity? selectedRarity;

        void Awake()
        {
            for (var index = 0; index < rarityButtons.Length; index++)
            {
                var capturedIndex = index;
                if (rarityButtons[index] != null)
                    rarityButtons[index].onClick.AddListener(() => SelectRarity(capturedIndex));
            }

            for (var index = 0; index < memeCards.Length; index++)
            {
                var capturedIndex = index;
                var button = memeCards[index] != null ? memeCards[index].GetComponent<Button>() : null;
                if (button != null)
                    button.onClick.AddListener(() => OpenMeme(capturedIndex));
            }
        }

        void Start()
        {
            Bind(collectionData);
            ApplyFilter();
        }

        public void Bind(MemeCollectionData data)
        {
            collectionData = data;
            if (collectionData == null || collectionData.Memes == null)
                return;

            var memes = collectionData.Memes;
            for (var index = 0; index < memeCards.Length; index++)
            {
                var hasData = index < memes.Length && memes[index] != null;
                if (memeCards[index] != null)
                {
                    memeCards[index].gameObject.SetActive(hasData);
                    if (hasData)
                        memeCards[index].Bind(memes[index]);
                }
            }

            ApplyFilter();
        }

        public void SelectRarity(int index)
        {
            if (index < 0 || index >= rarityButtons.Length || index >= buttonRarities.Length)
                return;

            selectedRarity = selectedRarity == buttonRarities[index] ? null : buttonRarities[index];
            ApplyFilter();
        }

        public void ClearFilter()
        {
            selectedRarity = null;
            ApplyFilter();
        }

        public void GoBack()
        {
            if (navigator != null)
                navigator.Show("Profile");
        }

        void OpenMeme(int index)
        {
            if (collectionData == null || collectionData.Memes == null || index < 0 || index >= collectionData.Memes.Length || collectionData.Memes[index] == null)
                return;

            if (detailPresenter != null)
                detailPresenter.Bind(collectionData.Memes[index]);
            if (navigator != null)
                navigator.Show("MemeCard");
        }

        void ApplyFilter()
        {
            var memes = collectionData != null ? collectionData.Memes : Array.Empty<MemeData>();
            for (var index = 0; index < memeCards.Length; index++)
            {
                var hasMeme = index < memes.Length && memes[index] != null;
                var visible = hasMeme && (!selectedRarity.HasValue || memes[index].Rarity == selectedRarity.Value);
                if (memeCards[index] != null)
                    memeCards[index].gameObject.SetActive(visible);
            }

            for (var index = 0; index < emptySlots.Length; index++)
            {
                if (emptySlots[index] != null)
                    emptySlots[index].SetActive(!selectedRarity.HasValue);
            }

            for (var index = 0; index < rarityButtons.Length; index++)
            {
                var isSelected = index < buttonRarities.Length && selectedRarity == buttonRarities[index];
                var label = rarityButtons[index] != null ? rarityButtons[index].GetComponentInChildren<TMP_Text>() : null;
                if (label != null)
                    label.color = isSelected ? selectedColor : MemeHunterUiColors.DarkNavy;
                if (rarityButtons[index] != null && rarityButtons[index].targetGraphic != null)
                    rarityButtons[index].targetGraphic.color = isSelected ? new Color(MemeHunterUiColors.Teal.r, MemeHunterUiColors.Teal.g, MemeHunterUiColors.Teal.b, 0.24f) : new Color(1f, 1f, 1f, 0f);
            }
        }
    }
}