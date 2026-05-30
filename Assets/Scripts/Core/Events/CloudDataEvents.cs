using System;
using Gamio.Core.Services;

public interface ICloudDataEvents : IDisposable
{
    event Action<SeedResponse> OnSeedFetched;
    event Action<StreakResponse> OnStreaksFetched;
    event Action<LeaderboardResponse> OnLeaderboardFetched;
    event Action<MyRankResponse> OnMyRankFetched;
    event Action OnAllDataFetched;

    void SeedFetched(SeedResponse response);
    void StreakFetched(StreakResponse response);
    void LeaderboardFetched(LeaderboardResponse response);
    void MyRankFetched(MyRankResponse response);
    void AllDataFetched();
}

public class CloudDataEvents : ICloudDataEvents
{
    public event Action<SeedResponse> OnSeedFetched;
    public event Action<StreakResponse> OnStreaksFetched;
    public event Action<LeaderboardResponse> OnLeaderboardFetched;
    public event Action<MyRankResponse> OnMyRankFetched;
    public event Action OnAllDataFetched;

    public void Dispose()
    {
        OnSeedFetched = null;
        OnStreaksFetched = null;
        OnLeaderboardFetched = null;
        OnMyRankFetched = null;
    }

    public void LeaderboardFetched(LeaderboardResponse response) => OnLeaderboardFetched?.Invoke(response);
    public void MyRankFetched(MyRankResponse response) => OnMyRankFetched?.Invoke(response);
    public void SeedFetched(SeedResponse response) => OnSeedFetched?.Invoke(response);
    public void StreakFetched(StreakResponse response) => OnStreaksFetched?.Invoke(response);
    public void AllDataFetched() => OnAllDataFetched?.Invoke();
}
