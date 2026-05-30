using Cysharp.Threading.Tasks;
using Gamio.Core;
using Gamio.Core.Services;
using Google;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class GamioManager : MonoBehaviour
{
    public StreakInfo StreakInfo => streakInfo;
    public bool DailyCompleted => dailyCompleted;
    public bool ChallengeActive => challengeActive;
    public string ChallengeSeed => challengeSeed;

    private StreakInfo streakInfo;
    private bool dailyCompleted;
    private bool challengeActive;
    private string challengeSeed;

    CloudAPIService cloudAPIService;
    ILoginEvents loginEvents;
    OfflineQueue offlineQueue;
    ICloudDataEvents cloudDataEvents;

    void Awake()
    {
        GamioAppContext.Register(this);
    }

    void OnEnable()
    {
        loginEvents = GamioAppContext.Get<ILoginEvents>();
        cloudAPIService = GamioAppContext.Get<CloudAPIService>();
        offlineQueue = GamioAppContext.Get<OfflineQueue>();
        cloudDataEvents = GamioAppContext.Get<ICloudDataEvents>();

        if (loginEvents != null)
        {
            loginEvents.OnAuthSuccess += FetchData;
        }
    }
    void OnDisable()
    {
        if (loginEvents != null)
        {
            loginEvents.OnAuthSuccess -= FetchData;
        }
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void SetChallengeActive(bool active)
    {
        challengeActive = active;
    }

    public async void FetchData()
    {
        try
        {
            var seedsTask = cloudAPIService.GetSeeds();
            var streaksTask = cloudAPIService.GetStreaks();

            var (seeds, streaks) = await UniTask.WhenAll(seedsTask, streaksTask);

            // Update local state once data is confirmed
            dailyCompleted = seeds.dailyCompleted;
            streakInfo = seeds.streak;
            challengeSeed = seeds.seed;

            // Trigger your internal events
            cloudDataEvents.SeedFetched(seeds);
            cloudDataEvents.StreakFetched(streaks);

            // Handle offline syncing if applicable
            if (offlineQueue != null && offlineQueue.PendingCount > 0)
            {
                await offlineQueue.SyncAll();
            }

            cloudDataEvents.AllDataFetched();
        }
        catch (System.Exception e)
        {
            // Centralized error handling
            Debug.LogError($"[Bootstrapper] Failed to fetch data: {e.Message}");
        }
    }
}
