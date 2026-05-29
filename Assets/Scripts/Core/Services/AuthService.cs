using System;
using UnityEngine;

namespace Gamio.Core.Services
{
    public class AuthService
    {
        private const string SessionTokenKey = "Gamio_SessionToken";
        private const string UserIdKey = "Gamio_UserId";
        private const string UsernameKey = "Gamio_Username";

        private readonly CloudAPIService cloudApiService;
        private string sessionToken;
        private string userId;

        public bool IsAuthenticated => !string.IsNullOrEmpty(sessionToken);
        public string UserId => userId;
        public string SessionToken => sessionToken;
        public string DisplayName { get; private set; } = "Player";
        public string Username { get; private set; } = "";

        public event Action OnAuthChanged;
        public event Action<string> OnAuthError;

        public AuthService(CloudAPIService api)
        {
            cloudApiService = api;
            LoadSession();
        }

        public void AuthenticateWithGoogle(string idToken)
        {
            cloudApiService.VerifyGoogleToken(idToken, OnAuthSuccess, error =>
            {
                Debug.LogError($"[Auth] Verification failed: {error}");
                OnAuthError?.Invoke(error);
            });
        }

        public void UpdateUsername(string username)
        {
            cloudApiService.UpdateUsername(username, result =>
            {
                Username = result.username;
                PlayerPrefs.SetString(UsernameKey, Username);
                PlayerPrefs.Save();
                OnAuthChanged?.Invoke();
            }, error =>
            {
                Debug.LogError($"[Auth] Username update failed: {error}");
                OnAuthError?.Invoke(error);
            });
        }

        private void OnAuthSuccess(AuthResult result)
        {
            sessionToken = result.sessionToken;
            userId = result.userId;
            DisplayName = result.displayName;
            Username = result.username ?? "";

            PlayerPrefs.SetString(SessionTokenKey, sessionToken);
            PlayerPrefs.SetString(UserIdKey, userId);
            PlayerPrefs.SetString(UsernameKey, Username);
            PlayerPrefs.Save();

            cloudApiService.SetSessionToken(sessionToken);
            OnAuthChanged?.Invoke();
        }

        public void ClearSession()
        {
            sessionToken = null;
            userId = null;

            PlayerPrefs.DeleteKey(SessionTokenKey);
            PlayerPrefs.DeleteKey(UserIdKey);
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.Save();

            OnAuthChanged?.Invoke();
        }

        private void LoadSession()
        {
            sessionToken = PlayerPrefs.GetString(SessionTokenKey, "");
            userId = PlayerPrefs.GetString(UserIdKey, "");
            Username = PlayerPrefs.GetString(UsernameKey, "");

            if (!string.IsNullOrEmpty(sessionToken))
                cloudApiService.SetSessionToken(sessionToken);
        }
    }
}
