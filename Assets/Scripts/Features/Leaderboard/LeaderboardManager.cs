using System;
using System.Threading.Tasks;
using Gamio.Core.Services;

namespace Gamio.Features.Leaderboard
{
    public class LeaderboardManager
    {
        private readonly CloudAPIService api;

        public LeaderboardEntry[] CurrentEntries { get; private set; }
        public int CurrentSeedId { get; private set; }
        public int TotalParticipants { get; private set; }
        public MyRankResponse MyRanks { get; private set; }
        public string MyUserId { get; set; }

        public event Action OnLeaderboardUpdated;
        public event Action<string> OnError;

        public LeaderboardManager(CloudAPIService cloudApi, AuthService authService)
        {
            api = cloudApi;
            MyUserId = authService?.UserId;
        }

        public async Task FetchLeaderboard(int seedId)
        {
            CurrentSeedId = seedId;

            try
            {
                var leaderboard = await api.GetLeaderboard(seedId);
                CurrentEntries = leaderboard.entries;
                TotalParticipants = leaderboard.totalParticipants;
                OnLeaderboardUpdated?.Invoke();
            }
            catch (Exception error)
            {
                OnError?.Invoke(error.Message);
            }

        }

        public async Task FetchMyRank()
        {
            try
            {
                var rank = await api.GetMyRank();
                MyRanks = rank;
                OnLeaderboardUpdated?.Invoke();
            }
            catch (Exception error)
            {
                OnError?.Invoke(error.Message);
                throw;
            }
        }

        public void SetTestData(LeaderboardEntry[] entries, int myRank, int seedId)
        {
            CurrentEntries = entries;
            CurrentSeedId = seedId;
            TotalParticipants = entries.Length;
            MyRanks = new MyRankResponse
            {
                userId = MyUserId,
                rankings = new[]
                {
                    new SeedRanking
                    {
                        seedId = seedId,
                        rank = myRank,
                        totalParticipants = entries.Length,
                        timeSeconds = entries[myRank - 1].timeSeconds,
                        streakCount = entries[myRank - 1].streakCount,
                        completedAt = System.DateTime.Now.ToString("o"),
                    }
                }
            };
            OnLeaderboardUpdated?.Invoke();
        }
    }
}
