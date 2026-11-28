using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace MemeHunter.Net
{
    // Field names on the left are C# style, [JsonProperty] names are exactly what the server sends.
    // This is the only place that must match the server (see the backend README, "Game API").
    // Times come as UTC with "Z": they are read as DateTime (Kind = Utc).

    public class PlayerDto
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("currency_balance")] public int CurrencyBalance;
        [JsonProperty("level")] public int Level;                     // legacy, always 1: use Rank
        [JsonProperty("xp")] public int Xp;
        [JsonProperty("rank")] public string Rank;                    // "rank_1" .. "rank_5" (names: UI/Ranks.cs)
        [JsonProperty("xp_to_next_rank")] public int? XpToNextRank;   // null at the top rank
        [JsonProperty("booster")] public BoosterDto Booster;
        [JsonProperty("current_streak")] public int CurrentStreak;    // already 0 if the streak burned out
        [JsonProperty("streak_freeze_count")] public int StreakFreezeCount;
        [JsonProperty("streak_done_today")] public bool StreakDoneToday;   // false: today's photo is still needed

        /// <summary>When this copy arrived (realtime clock, set by Session.SetPlayer): the booster timer counts from here.</summary>
        [JsonIgnore] public float ReceivedAt;

        public bool BoosterActive => Booster != null && Booster.Active && BoosterSecondsLeft > 0;

        /// <summary>Booster time left now, counted on the phone from the server's seconds_left.</summary>
        public int BoosterSecondsLeft =>
            Booster == null ? 0 : Mathf.Max(0, Booster.SecondsLeft - (int)(Time.realtimeSinceStartup - ReceivedAt));
    }

    public class BoosterDto
    {
        [JsonProperty("active")] public bool Active;
        [JsonProperty("multiplier")] public int? Multiplier;          // 2 or 5, null when inactive
        [JsonProperty("expires_at")] public DateTime? ExpiresAt;
        [JsonProperty("seconds_left")] public int SecondsLeft;
    }

    public class NearbyCardDto
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("rarity")] public string Rarity;
        // Rounded by the server (about 100 m): the exact place is revealed only by Claim.
        [JsonProperty("approx_lat")] public double ApproxLat;
        [JsonProperty("approx_lng")] public double ApproxLng;
    }

    public class ClaimDto
    {
        [JsonProperty("card_id")] public string CardId;
        [JsonProperty("lat")] public double Lat;     // exact
        [JsonProperty("lng")] public double Lng;     // exact
        [JsonProperty("rarity")] public string Rarity;
        /// <summary>The claim ends here (30 minutes): then the card is free again. Null for hunts saved by older builds.</summary>
        [JsonProperty("expires_at")] public DateTime? ExpiresAt;
    }

    public class SubmissionResultDto
    {
        [JsonProperty("submission_id")] public string SubmissionId;
        [JsonProperty("status")] public string Status;   // always "sent"
        [JsonProperty("reward")] public int Reward;      // coins, already added to the balance
        [JsonProperty("xp")] public int Xp;              // XP for this catch (booster included)
        [JsonProperty("streak")] public int Streak;      // daily streak after this catch
    }

    public class CollectionDto
    {
        [JsonProperty("total")] public int Total;
        [JsonProperty("by_rarity")] public Dictionary<string, int> ByRarity = new Dictionary<string, int>();
        [JsonProperty("cards")] public List<CollectedCardDto> Cards = new List<CollectedCardDto>();   // newest first
    }

    public class CollectedCardDto
    {
        [JsonProperty("card_id")] public string CardId;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("collected_at")] public DateTime CollectedAt;
        [JsonProperty("coins")] public int Coins;
        [JsonProperty("xp")] public int Xp;
    }

    public class LeaderboardDto
    {
        [JsonProperty("top")] public List<LeaderboardEntryDto> Top = new List<LeaderboardEntryDto>();
        [JsonProperty("me")] public LeaderboardEntryDto Me;          // the player's own entry, also outside the top
    }

    public class LeaderboardEntryDto
    {
        [JsonProperty("position")] public int Position;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("xp")] public int Xp;
        [JsonProperty("rank")] public string Rank;
        [JsonProperty("cards")] public int Cards;
    }
}
