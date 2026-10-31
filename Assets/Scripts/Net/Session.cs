using System;
using Newtonsoft.Json;
using UnityEngine;

namespace MemeHunter.Net
{
    /// <summary>
    /// What the app must remember between launches. Backed by PlayerPrefs
    /// (the same store the designer's onboarding uses for Nickname/City/Day/Month/Year).
    /// </summary>
    public static class Session
    {
        const string PlayerIdKey = "PlayerId";
        const string HuntKey = "ActiveHunt";

        /// <summary>Last player data received from the server (null until fetched).</summary>
        public static PlayerDto Player { get; private set; }

        /// <summary>Fires whenever Player changes: handy for HUD labels that show coins.</summary>
        public static event Action<PlayerDto> PlayerChanged;

        public static string PlayerId => PlayerPrefs.GetString(PlayerIdKey, string.Empty);
        public static bool HasPlayer => !string.IsNullOrEmpty(PlayerId);

        /// <summary>Call right after registration: stores the id permanently.</summary>
        public static void SavePlayer(PlayerDto player)
        {
            PlayerPrefs.SetString(PlayerIdKey, player.Id);
            PlayerPrefs.Save();
            SetPlayer(player);
        }

        /// <summary>Call after every successful GetPlayer: updates the cached copy.</summary>
        public static void SetPlayer(PlayerDto player)
        {
            Player = player;
            PlayerChanged?.Invoke(player);
        }

        // ----- Active hunt: a card the player already claimed and is walking to -----

        /// <summary>The claimed card with its EXACT coordinates, or null. Survives app restarts.</summary>
        public static ClaimDto ActiveHunt
        {
            get
            {
                var json = PlayerPrefs.GetString(HuntKey, string.Empty);
                return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<ClaimDto>(json);
            }
        }

        public static void SaveHunt(ClaimDto claim)
        {
            PlayerPrefs.SetString(HuntKey, JsonConvert.SerializeObject(claim));
            PlayerPrefs.Save();
        }

        public static void ClearHunt()
        {
            PlayerPrefs.DeleteKey(HuntKey);
            PlayerPrefs.Save();
        }

        /// <summary>Forget the player completely (used when the server no longer knows the id).</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(HuntKey);
            PlayerPrefs.DeleteKey(PlayerIdKey);
            PlayerPrefs.Save();
            Player = null;
        }
    }
}
