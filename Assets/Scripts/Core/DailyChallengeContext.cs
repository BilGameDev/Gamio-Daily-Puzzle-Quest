namespace Gamio.Core.Services
{
    public class DailyChallengeContext
    {
        public int SeedId { get; set; }
        public string Seed { get; set; }
        public string GameType { get; set; }
        public bool DailyCompleted { get; set; }
        public float TotalTimeSeconds { get; set; }
    }
}
