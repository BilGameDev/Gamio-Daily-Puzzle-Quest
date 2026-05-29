using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamio.Core.Services
{
    public class OfflineQueue
    {
        private const string QueueKey = "Gamio_OfflineQueue";
        private readonly CloudAPIService cloudAPIService;
        private readonly List<OfflineDailySubmit> pendingSubmits = new();

        public int PendingCount => pendingSubmits.Count;

        public event Action OnSyncCompleted;
        public event Action<string> OnSyncError;

        public OfflineQueue(CloudAPIService api)
        {
            cloudAPIService = api;
            Load();
        }

        public void QueueDailySubmit(int challengeId, float timeSeconds)
        {
            pendingSubmits.Add(new OfflineDailySubmit
            {
                date = DateTime.Today.ToString("yyyy-MM-dd"),
                challengeId = challengeId,
                timeSeconds = timeSeconds,
                queuedAt = DateTime.Now,
            });
            Save();
        }

        public void SyncAll()
        {
            if (PendingCount == 0) return;
            SyncNext();
        }

        private void SyncNext()
        {
            if (pendingSubmits.Count == 0)
            {
                OnSyncCompleted?.Invoke();
                return;
            }

            var submit = pendingSubmits[0];
            cloudAPIService.SyncOffline(submit.challengeId, submit.timeSeconds,
                _ =>
                {
                    pendingSubmits.RemoveAt(0);
                    Save();
                    SyncNext();
                },
                _ =>
                {
                    OnSyncError?.Invoke("Some items could not be synced. Retry later.");
                });
        }

        public void Clear()
        {
            pendingSubmits.Clear();
            Save();
        }

        private void Save()
        {
            var data = new OfflineSyncData
            {
                dailySubmits = pendingSubmits,
            };
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(QueueKey, json);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            var json = PlayerPrefs.GetString(QueueKey, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var data = JsonUtility.FromJson<OfflineSyncData>(json);
                if (data?.dailySubmits != null)
                    pendingSubmits.AddRange(data.dailySubmits);
            }
            catch (Exception e)
            {
                Debug.LogError($"[OfflineQueue] Failed to load: {e.Message}");
            }
        }
    }
}
