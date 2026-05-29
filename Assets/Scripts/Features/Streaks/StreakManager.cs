using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gamio.Features.Streaks
{
    public class StreakManager
    {
        private const string SaveKey = "Gamio_Streaks";
        private readonly Dictionary<string, StreakRecord> records = new();
        private int totalGames;
        private StreakRecord allGamesRecord;

        public IReadOnlyDictionary<string, StreakRecord> Records => records;

        public int CloudCurrentStreak { get; set; }
        public int CloudLongestStreak { get; set; }
        public bool UseCloudStreaks { get; set; }

        public event Action OnStreaksUpdated;

        public void SetTotalGames(int count)
        {
            totalGames = count;
        }

        public void Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return;

            var wrapper = JsonUtility.FromJson<StreakSaveData>(json);
            if (wrapper?.Records != null)
            {
                foreach (var r in wrapper.Records)
                    records[r.GameId] = r;
            }
        }

        public void Save()
        {
            var wrapper = new StreakSaveData
            {
                Records = records.Values.ToList()
            };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        public void RecordPlay(string gameId)
        {
            var record = GetOrCreate(gameId);
            var today = DateTime.Today;

            if (record.LastPlayedDate < today)
                record.LastPlayedDate = today;
        }

        public void RecordCompletion(string gameId)
        {
            var record = GetOrCreate(gameId);
            var today = DateTime.Today;

            if (record.LastCompletedDate.HasValue)
            {
                var daysSinceLast = (today - record.LastCompletedDate.Value).Days;
                if (daysSinceLast == 1)
                    record.CurrentStreak++;
                else if (daysSinceLast > 1)
                    record.CurrentStreak = 1;
            }
            else
            {
                record.CurrentStreak = 1;
            }

            record.LastCompletedDate = today;
            record.LastPlayedDate = today;

            if (record.CurrentStreak > record.LongestStreak)
                record.LongestStreak = record.CurrentStreak;

            Save();
            TryUpdateAllGamesStreak(today);
            OnStreaksUpdated?.Invoke();
        }

        /// <summary>
        /// Sync local streak state from cloud data.
        /// Cloud streak overrides individual game streaks.
        /// </summary>
        public void SyncFromCloud(int cloudCurrentStreak, int cloudLongestStreak, string lastCompletedDate)
        {
            CloudCurrentStreak = cloudCurrentStreak;
            CloudLongestStreak = cloudLongestStreak;
            UseCloudStreaks = true;

            // Update the all-games record
            var allRecord = GetOrCreate("__all__");
            allRecord.CurrentStreak = cloudCurrentStreak;
            allRecord.LongestStreak = cloudLongestStreak;

            if (!string.IsNullOrEmpty(lastCompletedDate) &&
                DateTime.TryParse(lastCompletedDate, out var parsedDate))
            {
                allRecord.LastCompletedDate = parsedDate;
                allRecord.LastPlayedDate = parsedDate;
            }

            OnStreaksUpdated?.Invoke();
        }

        public void ResetToLocal()
        {
            UseCloudStreaks = false;
        }

        private void TryUpdateAllGamesStreak(DateTime today)
        {
            if (totalGames == 0) return;

            var allDone = records.Values
                .Where(r => r.GameId != "__all__")
                .All(r => r.LastCompletedDate == today);

            if (!allDone) return;

            if (allGamesRecord == null)
                allGamesRecord = GetOrCreate("__all__");

            if (allGamesRecord.LastCompletedDate.HasValue)
            {
                var gap = (today - allGamesRecord.LastCompletedDate.Value).Days;
                allGamesRecord.CurrentStreak = gap == 1 ? allGamesRecord.CurrentStreak + 1 : 1;
            }
            else
            {
                allGamesRecord.CurrentStreak = 1;
            }

            allGamesRecord.LastCompletedDate = today;
            if (allGamesRecord.CurrentStreak > allGamesRecord.LongestStreak)
                allGamesRecord.LongestStreak = allGamesRecord.CurrentStreak;

            Save();
        }

        public bool IsCompletedToday(string gameId)
        {
            return GetOrCreate(gameId).LastCompletedDate == DateTime.Today;
        }

        public int GetStreak(string gameId)
        {
            if (UseCloudStreaks && gameId == "__all__")
                return CloudCurrentStreak;

            return GetOrCreate(gameId).CurrentStreak;
        }

        public int GetAllGamesStreak()
        {
            if (UseCloudStreaks)
                return CloudCurrentStreak;

            if (allGamesRecord == null)
                allGamesRecord = GetOrCreate("__all__");
            return allGamesRecord.CurrentStreak;
        }

        private StreakRecord GetOrCreate(string gameId)
        {
            if (!records.TryGetValue(gameId, out var record))
            {
                record = new StreakRecord { GameId = gameId };
                records[gameId] = record;
            }
            return record;
        }

        [Serializable]
        private class StreakSaveData
        {
            public List<StreakRecord> Records;
        }
    }
}
