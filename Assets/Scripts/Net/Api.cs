using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace MemeHunter.Net
{
    /// <summary>
    /// Thrown for any failed call. Message is already human-readable: show it to the player as is.
    /// </summary>
    public class ApiException : Exception
    {
        public long StatusCode;       // 0 when the server could not be reached at all
        public bool IsNetworkError;   // no connection / timeout

        public ApiException(string message, long statusCode, bool isNetworkError) : base(message)
        {
            StatusCode = statusCode;
            IsNetworkError = isNetworkError;
        }
    }

    /// <summary>
    /// One static method per backend endpoint. All methods are async: call them with `await`
    /// inside a try/catch (ApiException) and update the UI afterwards.
    /// </summary>
    public static class Api
    {
        // ---------- Endpoints ----------

        public static async Awaitable<PlayerDto> CreatePlayer(string displayName)
        {
            // JSON body: {"display_name": "..."}
            var body = JsonConvert.SerializeObject(new { display_name = displayName });
            var req = new UnityWebRequest(ApiConfig.BaseUrl + "/players", UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            return await Send<PlayerDto>(req, ApiConfig.DefaultTimeoutSeconds);
        }

        public static async Awaitable<PlayerDto> GetPlayer(string playerId)
        {
            var req = UnityWebRequest.Get(ApiConfig.BaseUrl + "/players/" + playerId);
            return await Send<PlayerDto>(req, ApiConfig.DefaultTimeoutSeconds);
        }

        public static async Awaitable<NearbyCardDto[]> GetNearby(double lat, double lng)
        {
            // Invariant culture: "43.25" and never "43,25" (a Russian-locale phone would send a comma).
            var url = ApiConfig.BaseUrl + "/cards/nearby?lat=" + Num(lat) + "&lng=" + Num(lng);
            var req = UnityWebRequest.Get(url);
            return await Send<NearbyCardDto[]>(req, ApiConfig.DefaultTimeoutSeconds);
        }

        public static async Awaitable<ClaimDto> ClaimCard(string cardId, string playerId)
        {
            // The backend expects a FORM field (not JSON) named player_id.
            var form = new WWWForm();
            form.AddField("player_id", playerId);
            var req = UnityWebRequest.Post(ApiConfig.BaseUrl + "/cards/" + cardId + "/claim", form);
            return await Send<ClaimDto>(req, ApiConfig.DefaultTimeoutSeconds);
        }

        public static async Awaitable<SubmissionResultDto> SubmitPhoto(
            string playerId, string cardId, double lat, double lng, byte[] jpeg)
        {
            // multipart/form-data: four text fields + one file field called "photo".
            var form = new WWWForm();
            form.AddField("player_id", playerId);
            form.AddField("card_id", cardId);
            form.AddField("lat", Num(lat));
            form.AddField("lng", Num(lng));
            form.AddBinaryData("photo", jpeg, "photo.jpg", "image/jpeg");
            var req = UnityWebRequest.Post(ApiConfig.BaseUrl + "/submissions", form);
            // Long timeout: the backend answers only after doskaz.kz answered.
            return await Send<SubmissionResultDto>(req, ApiConfig.SubmitTimeoutSeconds);
        }

        // ---------- Plumbing ----------

        static string Num(double value) => value.ToString("F6", CultureInfo.InvariantCulture);

        static async Awaitable<T> Send<T>(UnityWebRequest req, int timeoutSeconds)
        {
            using (req)   // frees native memory when finished, even on errors
            {
                req.timeout = timeoutSeconds;
                var op = req.SendWebRequest();

                // Wait without blocking the game: one frame at a time until the request finished.
                while (!op.isDone)
                    await Awaitable.NextFrameAsync();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var json = req.downloadHandler.text;
                    try
                    {
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[Api] Bad JSON: " + json + "\n" + e);
                        throw new ApiException("Unexpected answer from the server.", req.responseCode, false);
                    }
                }

                // The server answered with 4xx/5xx: its body has an explanation, e.g. {"detail": "Player not found"}.
                if (req.result == UnityWebRequest.Result.ProtocolError)
                {
                    var detail = ReadDetail(req.downloadHandler?.text);
                    Debug.LogWarning($"[Api] {req.method} {req.url} -> {req.responseCode} {detail}");
                    throw new ApiException(detail, req.responseCode, false);
                }

                // Everything else: no Wi-Fi, wrong address, server off, timeout, HTTP blocked by Android.
                Debug.LogWarning($"[Api] {req.method} {req.url} failed: {req.error}");
                throw new ApiException("Can't reach the server. Check your connection and try again.", 0, true);
            }
        }

        /// <summary>FastAPI errors look like {"detail": "text"} or {"detail": [ {...validation...} ]}.</summary>
        static string ReadDetail(string body)
        {
            if (string.IsNullOrEmpty(body))
                return "Server error.";
            try
            {
                var detail = JObject.Parse(body)["detail"];
                if (detail == null) return "Server error.";
                if (detail.Type == JTokenType.String) return detail.ToString();
                return "Some of the entered data is invalid.";   // 422 validation list
            }
            catch
            {
                return "Server error.";
            }
        }
    }
}
