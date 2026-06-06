using UnityEngine;

namespace Gamio.Core.Services
{
    [System.Serializable]
    public class GameSecrets
    {
        public string googleWebClientId = "";
        public string googleClientSecret = "";
        public string googleRedirectUri = "http://localhost:3000";
        public string backendApiUrl = "https://gamio-api.viridianbil.workers.dev";
        public string admobRewardedAdUnitId = "ca-app-pub-5838098451531956/6274858792";
    }

    public static class GameSecretsLoader
    {
        private static GameSecrets _instance;

        public static GameSecrets Load()
        {
            if (_instance != null) return _instance;

            var textAsset = Resources.Load<TextAsset>("secrets");
            if (textAsset == null)
            {
                Debug.LogWarning("[GameSecrets] secrets.json not found in Resources, using defaults");
                _instance = new GameSecrets();
                return _instance;
            }

            _instance = JsonUtility.FromJson<GameSecrets>(textAsset.text) ?? new GameSecrets();
            return _instance;
        }

        public static void Reload()
        {
            _instance = null;
        }
    }
}
