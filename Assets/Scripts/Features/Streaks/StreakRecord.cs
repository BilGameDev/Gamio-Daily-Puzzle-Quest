using System;

namespace Gamio.Features.Streaks
{
    [Serializable]
    public class StreakRecord
    {
        public string GameId;
        public int CurrentStreak;
        public int LongestStreak;
        public DateTime LastPlayedDate;
        public DateTime? LastCompletedDate;
    }
}
