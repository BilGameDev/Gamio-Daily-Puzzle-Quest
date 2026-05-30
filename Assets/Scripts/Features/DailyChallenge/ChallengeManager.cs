using System;
using System.Threading.Tasks;
using Gamio.Core;
using Gamio.Core.Services;
using Gamio.Features.DailyChallenge;
using Gamio.Features.Leaderboard;
using UnityEngine;

namespace Gamio.Root
{
    public class ChallengeManager : MonoBehaviour
    {
        private ChallengePopupUI popup;
        private string gameType;
        private float totalTime;
        private bool active;
        public bool IsActive => active;

        ICloudDataEvents cloudDataEvents;
        IUIEvents uiEvents;
        GamioManager gamioManager;
        GamesLibrary gamesLibrary;
        CloudAPIService cloudAPIService;

        void Awake()
        {
            GamioAppContext.Register(this);
        }

        void Start()
        {
            gamioManager = GamioAppContext.Get<GamioManager>();
            gamesLibrary = GamioAppContext.Get<GamesLibrary>();
            cloudAPIService = GamioAppContext.Get<CloudAPIService>();
        }

        void OnEnable()
        {
            cloudDataEvents = GamioAppContext.Get<ICloudDataEvents>();
            uiEvents = GamioAppContext.Get<IUIEvents>();

            if (cloudDataEvents != null)
                cloudDataEvents.OnSeedFetched += OnSeedFetched;

            if (uiEvents != null)
            {
                uiEvents.OnChallengeRequested += ShowPopup;
                uiEvents.OnBackRequested += OnBackRequested;
                uiEvents.OnChallengeSolved += OnChallengeSolved;
            }

        }

        void OnDisable()
        {
            if (cloudDataEvents != null)
                cloudDataEvents.OnSeedFetched -= OnSeedFetched;

            if (uiEvents != null)
            {
                uiEvents.OnChallengeRequested -= ShowPopup;
                uiEvents.OnBackRequested -= OnBackRequested;
                uiEvents.OnChallengeSolved -= OnChallengeSolved;
            }
        }

        void OnSeedFetched(SeedResponse seedResponse)
        {
            if (!seedResponse.dailyCompleted && !string.IsNullOrEmpty(seedResponse.gameType))
            {
                gameType = seedResponse.gameType;
                totalTime = seedResponse.totalTimeSeconds ?? 0;
            }
        }

        void ShowPopup()
        {
            if (string.IsNullOrEmpty(gameType)) return;
            active = true;

            popup = ChallengePopupUI.Show(transform, gameType, totalTime);
            popup.OnBeginRequested += OnBeginGame;
            popup.OnCloseRequested += OnClosePopup;
        }

        void OnBeginGame()
        {
            gamioManager?.SetChallengeActive(true);
            uiEvents.RequestGameScene(gamesLibrary?.GetGameScene(gameType));
            popup = null;
        }

        void OnClosePopup()
        {
            popup = null;
        }

        void OnChallengeSolved(float solveTime)
        {
            Debug.Log(solveTime);
            _ = SubmitDaily(solveTime);
        }

        async Task SubmitDaily(float solveTime)
        {
            try
            {
                var dailySublit = await cloudAPIService.SubmitDaily(gamioManager.ChallengeId, solveTime);
                if (dailySublit.success)
                {
                    await LeaderboardPopupUI.Show(
                new LeaderboardManager(cloudAPIService,
                GamioAppContext.Get<AuthService>()),
                gamioManager.ChallengeId,
                LeaderboardMode.Result);
                }
            }
            catch (Exception error)
            {
                Debug.Log(error.Message);
            }
        }

        void OnBackRequested()
        {
            if (gamioManager.ChallengeActive)
            {
                gamioManager.SetChallengeActive(false);
            }
        }

        public void OnChallengeCompleted()
        {
            if (!active) return;
            active = false;

            if (popup != null)
            {
                popup.OnCloseRequested -= OnClosePopup;
                popup.Dismiss();
                popup = null;
            }

            gamioManager?.SetChallengeActive(false);
        }
    }
}
