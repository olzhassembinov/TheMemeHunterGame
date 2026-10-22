using UnityEngine;

namespace MemeHunter.UI
{
    [CreateAssetMenu(menuName = "Meme Hunter/Profile Presentation Data")]
    public sealed class ProfilePresentationData : ScriptableObject
    {
        [SerializeField] string nickname = "DigItalitE";
        [SerializeField, Min(0)] int age = 10;
        [SerializeField] string city = "Astana";
        [SerializeField, Min(1)] int level = 12;
        [SerializeField, Min(0)] int currentXp = 220;
        [SerializeField, Min(1)] int xpForNextLevel = 860;
        [SerializeField, Min(0)] int money = 6099;
        [SerializeField] Sprite profilePhoto;
        [SerializeField] string[] bestMemeTitles;
        [SerializeField] Sprite[] bestMemeArtworks;

        public string Nickname => nickname;
        public int Age => age;
        public string City => city;
        public int Level => level;
        public int CurrentXp => currentXp;
        public int XpForNextLevel => xpForNextLevel;
        public int Money => money;
        public Sprite ProfilePhoto => profilePhoto;
        public string[] BestMemeTitles => bestMemeTitles;
        public Sprite[] BestMemeArtworks => bestMemeArtworks;

        public void SetPlayer(string _name, string _city, int _age)
        {
            nickname = _name;
            city = _city;
            age = _age;
        }
    }
}