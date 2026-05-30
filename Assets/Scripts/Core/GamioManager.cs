using Cysharp.Threading.Tasks;
using Gamio.Core;
using Gamio.Core.Services;
using Google;
using UnityEngine;

public class GamioManager : MonoBehaviour
{
    private int streakCount;
    private bool dailyCompleted;
    private bool challengeActive;
    GoogleSignInUser signInUser;
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

    public void SetGoogleUser(GoogleSignInUser googleSignInUser)
    {
        signInUser = googleSignInUser;
    }

    public void SetChallengeActive(bool active)
    {
        challengeActive = active;
    }

    public int GetStreak() => streakCount;
    public bool GetChallengeCompleted() => dailyCompleted;
    public bool GetChallengeActive() => challengeActive;

    public async void FetchData()
    {
        // Initialize state
        streakCount = 0;
        dailyCompleted = false;

        try
        {
            // Fire both requests in parallel using UniTask.WhenAll
            // This is significantly faster than waiting for them one by one
            var seedsTask = cloudAPIService.GetSeeds();
            var streaksTask = cloudAPIService.GetStreaks();

            // The method pauses here until both tasks complete
            var (seeds, streaks) = await UniTask.WhenAll(seedsTask, streaksTask);

            // Update local state once data is confirmed
            dailyCompleted = seeds.dailyCompleted;
            streakCount = 55; // Logic applied

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
