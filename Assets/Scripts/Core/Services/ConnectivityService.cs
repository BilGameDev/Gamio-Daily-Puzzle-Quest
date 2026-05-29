using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Gamio.Core.Services
{
    public class ConnectivityService
    {
        private static readonly string[] CheckUrls = {
            "https://clients3.google.com/generate_204",
            "https://www.gstatic.com/generate_204",
            "https://gamio-api.viridianbil.workers.dev/api/config",
        };

        private const float CheckTimeout = 5f;
        private bool _connected = true;
        private bool _checking;

        public bool IsConnected => _connected;
        public event Action OnConnectivityChanged;

        public ConnectivityService()
        {
#if !UNITY_EDITOR
            _connected = Application.internetReachability != NetworkReachability.NotReachable;
#endif
        }

        public IEnumerator CheckRoutine(Action<bool> onResult)
        {
            if (_checking) yield break;
            _checking = true;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                SetConnected(false);
                _checking = false;
                onResult?.Invoke(false);
                yield break;
            }

            bool reached = false;
            for (int i = 0; i < CheckUrls.Length && !reached; i++)
            {
                using var req = UnityWebRequest.Head(CheckUrls[i]);
                req.timeout = Mathf.FloorToInt(CheckTimeout);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success ||
                    req.responseCode == 204 || req.responseCode == 200)
                {
                    reached = true;
                }
            }

            SetConnected(reached);
            _checking = false;
            onResult?.Invoke(_connected);
        }

        public void Check(Action<bool> onResult)
        {
            CoroutineRunner.Instance.StartCoroutine(CheckRoutine(onResult));
        }

        private void SetConnected(bool value)
        {
            if (_connected == value) return;
            _connected = value;
            OnConnectivityChanged?.Invoke();
        }
    }
}
