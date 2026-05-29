using System;
using System.Collections;
using System.Text;
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

        public Coroutine VerifyGoogleToken(string idToken, Action<AuthResult> onSuccess, Action<string> onError)
        {
            var json = $"{{\"idToken\":\"{EscapeJson(idToken)}\"}}";
            return Post("/api/auth/verify", json, false, onSuccess, onError);
        }

        public Coroutine GetSeeds(Action<SeedResponse> onSuccess, Action<string> onError)
        {
            return Get("/api/seeds", onSuccess, onError);
        }

        public Coroutine SubmitDaily(int challengeId, float timeSeconds,
            Action<DailySubmitResult> onSuccess, Action<string> onError)
        {
            var json = $"{{\"challengeId\":{challengeId},\"timeSeconds\":{timeSeconds}}}";
            return Post("/api/daily/submit", json, true, onSuccess, onError);
        }

        public Coroutine SyncOffline(int challengeId, float timeSeconds,
            Action<DailySubmitResult> onSuccess, Action<string> onError)
        {
            var json = $"{{\"challengeId\":{challengeId},\"timeSeconds\":{timeSeconds}}}";
            return Post("/api/daily/sync", json, true, onSuccess, onError);
        }

        public Coroutine GetStreaks(Action<StreakResponse> onSuccess, Action<string> onError)
        {
            return Get("/api/streaks", onSuccess, onError);
        }

        public Coroutine GetLeaderboard(int seedId, Action<LeaderboardResponse> onSuccess, Action<string> onError)
        {
            return Get($"/api/leaderboard/{seedId}", onSuccess, onError);
        }

        public Coroutine GetMyRank(Action<MyRankResponse> onSuccess, Action<string> onError)
        {
            return Get("/api/leaderboard/me", onSuccess, onError);
        }

        public Coroutine UpdateUsername(string username, Action<UsernameResult> onSuccess, Action<string> onError)
        {
            var json = $"{{\"username\":\"{EscapeJson(username)}\"}}";
            return Post("/api/users/username", json, true, onSuccess, onError);
        }

        public Coroutine DeleteCompletions(Action<ApiError> onSuccess, Action<string> onError)
        {
            return Delete("/api/user/completions", onSuccess, onError);
        }

        public Coroutine GetConfig(Action<ConfigResponse> onSuccess, Action<string> onError)
        {
            return Get("/api/config", onSuccess, onError);
        }

        private Coroutine Get<T>(string path, Action<T> onSuccess, Action<string> onError) where T : class
        {
            return CoroutineRunner.Instance.StartCoroutine(GetRoutine(path, onSuccess, onError));
        }

        private Coroutine Post<T>(string path, string json, bool requiresAuth,
            Action<T> onSuccess, Action<string> onError) where T : class
        {
            return CoroutineRunner.Instance.StartCoroutine(PostRoutine(path, json, requiresAuth, onSuccess, onError));
        }

        private Coroutine Delete<T>(string path, Action<T> onSuccess, Action<string> onError) where T : class
        {
            return CoroutineRunner.Instance.StartCoroutine(DeleteRoutine(path, onSuccess, onError));
        }

        private IEnumerator DeleteRoutine<T>(string path, Action<T> onSuccess, Action<string> onError) where T : class
        {
            var url = _baseUrl + path;
            using var req = new UnityWebRequest(url, "DELETE");
            req.downloadHandler = new DownloadHandlerBuffer();
            AddHeaders(req);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            if (req.responseCode >= 400)
            {
                var msg = TryParseApiError(req.downloadHandler.text) ?? $"HTTP {req.responseCode}";
                onError?.Invoke(msg);
                yield break;
            }

            ProcessResponse(req.downloadHandler.text, onSuccess, onError);
        }

        private IEnumerator GetRoutine<T>(string path, Action<T> onSuccess, Action<string> onError) where T : class
        {
            var url = _baseUrl + path;
            using var req = UnityWebRequest.Get(url);
            AddHeaders(req);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            if (req.responseCode >= 400)
            {
                var msg = TryParseApiError(req.downloadHandler.text) ?? $"HTTP {req.responseCode}";
                onError?.Invoke(msg);
                yield break;
            }

            ProcessResponse(req.downloadHandler.text, onSuccess, onError);
        }

        private IEnumerator PostRoutine<T>(string path, string json, bool requiresAuth,
            Action<T> onSuccess, Action<string> onError) where T : class
        {
            var url = _baseUrl + path;
            using var req = new UnityWebRequest(url, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            if (requiresAuth) AddHeaders(req);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            if (req.responseCode >= 400)
            {
                var msg = TryParseApiError(req.downloadHandler.text) ?? $"HTTP {req.responseCode}";
                onError?.Invoke(msg);
                yield break;
            }

            ProcessResponse(req.downloadHandler.text, onSuccess, onError);
        }

        private void ProcessResponse<T>(string text, Action<T> onSuccess, Action<string> onError) where T : class
        {
            var result = JsonUtility.FromJson<T>(text);
            if (result != null)
                onSuccess?.Invoke(result);
            else
                onError?.Invoke("Failed to parse response");
        }

        private void AddHeaders(UnityWebRequest req)
        {
            if (!string.IsNullOrEmpty(_sessionToken))
                req.SetRequestHeader("Authorization", $"Bearer {_sessionToken}");
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string TryParseApiError(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                var err = JsonUtility.FromJson<ApiError>(text);
                return err?.error;
            }
            catch
            {
                return null;
            }
        }
    }
}
