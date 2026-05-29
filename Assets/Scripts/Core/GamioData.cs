using System;

namespace Gamio.Core.Services
{
    [Serializable]
    public class AuthResult
    {
        public string userId;
        public string sessionToken;
        public string displayName;
        public string username;
        public string email;
        public string avatarUrl;
    }

    [Serializable]
    public class SeedResponse
    {
        public string date;
        public int seedId;
        public string seed;
        public string gameType;
        public bool dailyCompleted;
        public float? totalTimeSeconds;
        public StreakInfo streak;
    }

    [Serializable]
    public class StreakInfo
    {
        public int current;
        public int longest;
    }

    [Serializable]
    public class DailySubmitResult
    {
        public bool success;
        public bool alreadyCompleted;
        public float totalTimeSeconds;
        public StreakInfo streak;
    }

    [Serializable]
    public class StreakResponse
    {
        public int current;
        public int longest;
        public string lastCompletedDate;
        public RecentCompletion[] recentCompletions;
        public string[] completionDates;
    }

    [Serializable]
    public class RecentCompletion
    {
        public string date;
        public float total_time_seconds;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public int seedId;
        public int totalParticipants;
        public LeaderboardEntry[] entries;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string userId;
        public string displayName;
        public string avatarUrl;
        public float timeSeconds;
        public int streakCount;
        public string completedAt;
    }

    [Serializable]
    public class MyRankResponse
    {
        public string userId;
        public SeedRanking[] rankings;
    }

    [Serializable]
    public class SeedRanking
    {
        public int seedId;
        public int rank;
        public int totalParticipants;
        public float timeSeconds;
        public int streakCount;
        public string completedAt;
    }

    [Serializable]
    public class ConfigResponse
    {
        public string[] gameTypes;
    }

    [Serializable]
    public class ApiError
    {
        public string error;
    }

    [Serializable]
    public class UsernameResult
    {
        public bool success;
        public string username;
    }

    [Serializable]
    public class OfflineDailySubmit
    {
        public string date;
        public int challengeId;
        public float timeSeconds;
        public DateTime queuedAt;
    }

    [Serializable]
    public class OfflineSyncData
    {
        public System.Collections.Generic.List<OfflineDailySubmit> dailySubmits = new();
    }
}
