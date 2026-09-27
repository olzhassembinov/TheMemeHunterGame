using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoundedRectGraphic), typeof(Button))]
    public sealed class MemeLocationDot : MonoBehaviour
    {
        [SerializeField] Graphic dotGraphic;
        [SerializeField] Button button;
        [SerializeField] MemeData memeData;

        public UnityEvent<MemeData> Selected = new UnityEvent<MemeData>();
        public MemeData Meme => memeData;

        void Awake()
        {
            if (dotGraphic == null)
                dotGraphic = GetComponent<Graphic>();
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
            {
                button.targetGraphic = dotGraphic;
                button.onClick.AddListener(NotifySelected);
            }
        }

        public void Bind(MemeData data)
        {
            memeData = data;
            if (dotGraphic == null)
                dotGraphic = GetComponent<Graphic>();
            if (dotGraphic != null && data != null)
                dotGraphic.color = RarityColor(data.Rarity);
        }

        public void NotifySelected()
        {
            if (memeData != null)
                Selected.Invoke(memeData);
        }

        public static Color RarityColor(MemeRarity rarity)
        {
            return rarity switch
            {
                MemeRarity.Common => MemeHunterUiColors.CommonGrey,
                MemeRarity.Uncommon => MemeHunterUiColors.UncommonGreen,
                MemeRarity.Rare => MemeHunterUiColors.RarePurple,
                MemeRarity.Legendary => MemeHunterUiColors.BrightGreen,
                _ => MemeHunterUiColors.CommonGrey
            };
        }
    }
}