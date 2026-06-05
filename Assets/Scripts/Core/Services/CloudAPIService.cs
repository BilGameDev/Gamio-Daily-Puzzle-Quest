using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Gamio.Core.Services
{
    public class CloudAPIService
    {
        private const string DefaultBaseUrl = "https://gamio-api.viridianbil.workers.dev";
        private string _baseUrl;
        private string _sessionToken;

        public CloudAPIService(string baseUrl = null)
        {
            _baseUrl = baseUrl ?? DefaultBaseUrl;
        }

        public void SetSessionToken(string token) => _sessionToken = token;
        public void SetBaseUrl(string url) => _baseUrl = url;

        // --- Core API Methods ---
        public async UniTask<AuthResult> VerifyGoogleToken(string idToken)
        {
            var json = $"{{\"idToken\":\"{EscapeJson(idToken)}\"}}";
            return await PostAsync<AuthResult>("/api/auth/verify", json, false);
        }

        public async UniTask<SeedResponse> GetSeeds() => await GetAsync<SeedResponse>("/api/seeds");

        public async UniTask<DailySubmitResult> SubmitDaily(int challengeId, float timeSeconds)
        {
            var json = $"{{\"challengeId\":{challengeId},\"timeSeconds\":{timeSeconds}}}";
            return await PostAsync<DailySubmitResult>("/api/daily/submit", json, true);
        }

        public async UniTask<DailySubmitResult> SyncOffline(int challengeId, float timeSeconds)
        {
            var json = $"{{\"challengeId\":{challengeId},\"timeSeconds\":{timeSeconds}}}";
            return await PostAsync<DailySubmitResult>("/api/daily/sync", json, true);
        }

        public async UniTask<StreakResponse> GetStreaks() => await GetAsync<StreakResponse>("/api/streaks");

        public async UniTask<LeaderboardResponse> GetLeaderboard(int seedId) => await GetAsync<LeaderboardResponse>($"/api/leaderboard/{seedId}");

        public async UniTask<TodayLeaderboardsResponse> GetTodayLeaderboards() => await GetAsync<TodayLeaderboardsResponse>("/api/leaderboard/today");

        public async UniTask<MyRankResponse> GetMyRank() => await GetAsync<MyRankResponse>("/api/leaderboard/me");

        public async UniTask<UsernameResult> UpdateUsername(string username)
        {
            var json = $"{{\"username\":\"{EscapeJson(username)}\"}}";
            return await PostAsync<UsernameResult>("/api/users/username", json, true);
        }

        public async UniTask<ApiError> DeleteCompletions() => await DeleteAsync<ApiError>("/api/user/completions");

        public async UniTask<ConfigResponse> GetConfig() => await GetAsync<ConfigResponse>("/api/config");

        // --- Generic Request Handlers ---

        private async UniTask<T> GetAsync<T>(string path) where T : class
        {
            using var req = UnityWebRequest.Get(_baseUrl + path);
            AddHeaders(req);
            await req.SendWebRequest();
            return HandleResponse<T>(req);
        }

        private async UniTask<T> PostAsync<T>(string path, string json, bool requiresAuth) where T : class
        {
            using var req = new UnityWebRequest(_baseUrl + path, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            if (requiresAuth) AddHeaders(req);

            await req.SendWebRequest();
            return HandleResponse<T>(req);
        }

        private async UniTask<T> DeleteAsync<T>(string path) where T : class
        {
            using var req = new UnityWebRequest(_baseUrl + path, "DELETE");
            req.downloadHandler = new DownloadHandlerBuffer();
            AddHeaders(req);

            await req.SendWebRequest();
            return HandleResponse<T>(req);
        }

        // --- Helpers ---

        private T HandleResponse<T>(UnityWebRequest req) where T : class
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                var errorMsg = TryParseApiError(req.downloadHandler.text) ?? $"HTTP {req.responseCode}: {req.error}";
                throw new Exception(errorMsg);
            }

            var result = JsonUtility.FromJson<T>(req.downloadHandler.text);
            if (result == null) throw new Exception("Failed to parse response JSON");
            
            return result;
        }

        private void AddHeaders(UnityWebRequest req)
        {
            if (!string.IsNullOrEmpty(_sessionToken))
                req.SetRequestHeader("Authorization", $"Bearer {_sessionToken}");
        }

        private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string TryParseApiError(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                return JsonUtility.FromJson<ApiError>(text)?.error;
            }
            catch { return null; }
        }
    }
}