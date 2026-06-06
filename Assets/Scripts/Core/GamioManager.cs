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
    public int ChallengeId => challengeId;
    public bool StreakPending => streakPending;
    public ChallengeInfo[] Challenges => challenges;
    public IGame CurrentGame => currentGame;
    private StreakInfo streakInfo;
    private bool dailyCompleted;
    private bool challengeActive;
    private string challengeSeed;
    private int challengeId;
    private bool streakPending;
    private IGame currentGame;
    private ChallengeInfo[] challenges;

    CloudAPIService cloudAPIService;
    ILoginEvents loginEvents;
    OfflineQueue offlineQueue;
    ICloudDataEvents cloudDataEvents;
    IUIEvents uiEvents;

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
        uiEvents = GamioAppContext.Get<IUIEvents>();

        if (loginEvents != null)
        {
            loginEvents.OnAuthSuccess += FetchData;
        }
    }
    void OnDisable()
    {
        GamioAppContext.Clear();
        
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

    public void SetStreakPending(bool pending)
    {
        streakPending = pending;
    }

    public void SetStreak(StreakInfo streak)
    {
        streakInfo = streak;
    }

    public void SetDailyCompleted(bool completed)
    {
        dailyCompleted = completed;
    }

    public void SetCurrentGame(IGame game)
    {
        currentGame = game;
        uiEvents?.LaunchGame(game);
    }

    public void SelectChallenge(int index)
    {
        if (challenges == null || index < 0 || index >= challenges.Length) return;
        var challenge = challenges[index];
        challengeSeed = challenge.seed;
        challengeId = challenge.seedId;
    }

    public async void FetchData()
    {
        try
        {
            var seedsTask = cloudAPIService.GetSeeds();
            var streaksTask = cloudAPIService.GetStreaks();

            var (seeds, streaks) = await UniTask.WhenAll(seedsTask, streaksTask);

            challenges = seeds.challenges;
            dailyCompleted = seeds.dailyCompleted;
            streakInfo = seeds.streak;

            cloudDataEvents.SeedFetched(seeds);
            cloudDataEvents.StreakFetched(streaks);

            if (offlineQueue != null && offlineQueue.PendingCount > 0)
            {
                await offlineQueue.SyncAll();
            }

            cloudDataEvents.AllDataFetched();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Bootstrapper] Failed to fetch data: {e.Message}");
        }
    }
}
