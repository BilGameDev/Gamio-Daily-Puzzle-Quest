using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Gamio.Core.Services
{
    public class AuthService
    {
        private const string SessionTokenKey = "Gamio_SessionToken";
        private const string UserIdKey = "Gamio_UserId";
        private const string UsernameKey = "Gamio_Username";
        private const string DisplayNameKey = "Gamio_Displayname";

        private readonly CloudAPIService cloudApiService;
        private readonly ILoginEvents loginEvents;
        private string sessionToken;
        private string userId;

        public bool IsAuthenticated => !string.IsNullOrEmpty(sessionToken);
        public string UserId => userId;
        public string SessionToken => sessionToken;
        public string DisplayName { get; private set; } = "Player";
        public string Username { get; private set; } = "";

        public AuthService(CloudAPIService api, ILoginEvents login)
        {
            cloudApiService = api;
            loginEvents = login;
        }

        public async Task AuthenticateWithGoogle(string idToken)
        {
            if (LoadSession())
            {
                cloudApiService.SetSessionToken(sessionToken);
                loginEvents?.AuthSuccess();
            }
            else
            {
                try
                {
                    OnAuthSuccess(await cloudApiService.VerifyGoogleToken(idToken));
                }
                catch (Exception e)
                {
                    loginEvents?.AuthFailed(e.Message);
                }
            }
        }

        public async Task UpdateUsername(string username)
        {
            try
            {
                var result = await cloudApiService.UpdateUsername(username);
                Username = result.username;
                PlayerPrefs.SetString(UsernameKey, Username);
                PlayerPrefs.Save();
                loginEvents?.AuthSuccess();
            }
            catch (Exception error)
            {
                Debug.LogError($"[Auth] Username update failed: {error.Message}");
                loginEvents?.AuthFailed(error.Message);
                throw;
            }
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
            PlayerPrefs.SetString(DisplayNameKey, DisplayName);
            PlayerPrefs.Save();

            cloudApiService.SetSessionToken(sessionToken);
            loginEvents?.AuthSuccess();
        }

        public void ClearSession()
        {
            sessionToken = null;
            userId = null;

            PlayerPrefs.DeleteKey(SessionTokenKey);
            PlayerPrefs.DeleteKey(UserIdKey);
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.Save();

            loginEvents?.AuthSuccess();
        }

        private bool LoadSession()
        {
            sessionToken = PlayerPrefs.GetString(SessionTokenKey, "");
            userId = PlayerPrefs.GetString(UserIdKey, "");
            Username = PlayerPrefs.GetString(UsernameKey, "");
            DisplayName = PlayerPrefs.GetString(DisplayNameKey, "Player");

            return !string.IsNullOrEmpty(sessionToken);
        }
    }
}
