using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Gamio.Services
{
    public class GoogleAuthManager : MonoBehaviour
    {
        const string k_refreshTokenKey = "google_refresh_token";
        const string k_tokenEndpoint = "https://oauth2.googleapis.com/token";
        public bool loginOnStart;

        [Header("Listener Settings")]
        public int listenPort = 3000;

        private TcpListener listener;

        [Header("Google OAuth Credentials")]
        public string clientId;
        public string clientSecret;
        public string redirectUri = "http://localhost:3000"; // Must match Google OAuth config

        private string authCode;

        public static UnityAction<string> OnGoogleAuthTokenReceived;
        public static UnityAction<string> OnGoogleLoginFailed;

        public bool HasStoredSession => !string.IsNullOrEmpty(PlayerPrefs.GetString(k_refreshTokenKey, string.Empty));

        void Start()
        {
            if (loginOnStart)
            {
                Login();
            }
        }

        public void Login()
        {
            if (!HasStoredSession)
            {
                StartGoogleLogin();
                return;
            }

            StartCoroutine(RefreshIdToken());
        }

        public void StartGoogleLogin()
        {
            string scope = "openid%20email";
            string state = Guid.NewGuid().ToString("N");
            string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={state}&access_type=offline&prompt=consent";

            Application.OpenURL(authUrl);
            StartListening();
        }

        public static void ClearStoredSession()
        {
            PlayerPrefs.DeleteKey(k_refreshTokenKey);
            PlayerPrefs.Save();
        }

        public void OnReceivedAuthCode(string code) // Call this with the code captured from browser redirect
        {
            authCode = code;
            StartCoroutine(ExchangeAuthCodeForToken());
        }

        IEnumerator ExchangeAuthCodeForToken()
        {
            WWWForm form = new WWWForm();
            form.AddField("code", authCode);
            form.AddField("client_id", clientId);
            form.AddField("client_secret", clientSecret);
            form.AddField("redirect_uri", redirectUri);
            form.AddField("grant_type", "authorization_code");

            using (UnityWebRequest www = UnityWebRequest.Post(k_tokenEndpoint, form))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Token exchange failed: {www.error}");
                    OnGoogleLoginFailed?.Invoke($"Google token exchange failed: {www.error}");
                }
                else
                {
                    var json = www.downloadHandler.text;
                    var tokenResponse = JsonUtility.FromJson<GoogleTokenResponse>(json);

                    if (!string.IsNullOrEmpty(tokenResponse.refresh_token))
                    {
                        PlayerPrefs.SetString(k_refreshTokenKey, tokenResponse.refresh_token);
                        PlayerPrefs.Save();
                    }

                    OnGoogleAuthTokenReceived?.Invoke(tokenResponse.id_token);
                }
            }
        }

        IEnumerator RefreshIdToken()
        {
            var refreshToken = PlayerPrefs.GetString(k_refreshTokenKey, string.Empty);
            if (string.IsNullOrEmpty(refreshToken))
            {
                OnGoogleLoginFailed?.Invoke("No stored Google session found.");
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("client_id", clientId);
            form.AddField("client_secret", clientSecret);
            form.AddField("refresh_token", refreshToken);
            form.AddField("grant_type", "refresh_token");

            using (UnityWebRequest www = UnityWebRequest.Post(k_tokenEndpoint, form))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Silent Google refresh failed: {www.error}");
                    ClearStoredSession();
                    OnGoogleLoginFailed?.Invoke($"Silent Google login failed: {www.error}");
                    yield break;
                }

                var json = www.downloadHandler.text;
                var tokenResponse = JsonUtility.FromJson<GoogleTokenResponse>(json);

                if (string.IsNullOrEmpty(tokenResponse.id_token))
                {
                    Debug.LogError("Silent Google refresh did not return an id_token.");
                    ClearStoredSession();
                    OnGoogleLoginFailed?.Invoke("Silent Google login failed.");
                    yield break;
                }

                OnGoogleAuthTokenReceived?.Invoke(tokenResponse.id_token);
            }
        }

        public void StartListening()
        {
            Task.Run(() =>
            {
                try
                {
                    listener = new TcpListener(IPAddress.Loopback, listenPort);
                    listener.Start();

                    Debug.Log($"Listening on http://localhost:{listenPort} for Google redirect...");

                    while (true)
                    {
                        using (var client = listener.AcceptTcpClient())
                        using (var stream = client.GetStream())
                        using (var reader = new StreamReader(stream))
                        using (var writer = new StreamWriter(stream))
                        {
                            var requestLine = reader.ReadLine();
                            if (requestLine == null) continue;

                            var parts = requestLine.Split(' ');
                            if (parts.Length < 2) continue;

                            var path = parts[1];
                            var query = path.Split('?');
                            if (query.Length < 2) continue;

                            var queryParams = query[1].Split('&');
                            foreach (var param in queryParams)
                            {
                                if (param.StartsWith("code="))
                                {
                                    var code = Uri.UnescapeDataString(param.Substring(5));
                                    Debug.Log("Received Google OAuth code: " + code);

                                    // Send success response
                                    string response = "<html><body><h2>Login successful! You can return to the app.</h2></body></html>";
                                    string header = "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: " + response.Length + "\r\n\r\n";
                                    writer.Write(header + response);
                                    writer.Flush();

                                    // Pass the code back to the auth manager
                                    authCode = code;

                                    UnityMainThreadHelper.Run(() => StartCoroutine(ExchangeAuthCodeForToken())); // Notify listeners on main thread

                                    listener.Stop(); // Stop after receiving once
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("OAuth listener error: " + ex);
                }
            });
        }

        void OnDestroy()
        {
            listener?.Stop();
        }

        [Serializable]
        public class GoogleTokenResponse
        {
            public string access_token;
            public string expires_in;
            public string refresh_token;
            public string scope;
            public string token_type;
            public string id_token;
        }
    }
}
