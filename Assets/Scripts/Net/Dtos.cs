using Newtonsoft.Json;

namespace MemeHunter.Net
{
    // Field names on the left are C# style, [JsonProperty] names are exactly what the server sends.
    // If Postman shows a different type (for example "reward" with decimals), change the C# type here:
    // this is the only place that must match the server.

    public class PlayerDto
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("currency_balance")] public int CurrencyBalance;
        [JsonProperty("level")] public int Level;
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
    }

    public class SubmissionResultDto
    {
        [JsonProperty("submission_id")] public string SubmissionId;
        [JsonProperty("status")] public string Status;   // "sent" or "failed"
        [JsonProperty("reward")] public int Reward;      // instant base reward
    }
}
