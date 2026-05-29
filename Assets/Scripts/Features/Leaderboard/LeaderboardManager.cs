using System;
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

        public LeaderboardManager(CloudAPIService cloudApi)
        {
            api = cloudApi;
        }

        public void FetchLeaderboard(int seedId)
        {
            CurrentSeedId = seedId;
            api.GetLeaderboard(seedId, response =>
            {
                CurrentEntries = response.entries;
                TotalParticipants = response.totalParticipants;
                OnLeaderboardUpdated?.Invoke();
            }, error =>
            {
                OnError?.Invoke(error);
            });
        }

        public void FetchMyRank()
        {
            api.GetMyRank(rankResponse =>
            {
                MyRanks = rankResponse;
                OnLeaderboardUpdated?.Invoke();
            }, error =>
            {
                OnError?.Invoke(error);
            });
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
