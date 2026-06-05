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
        private ChallengeInfo[] availableChallenges;
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
            availableChallenges = seedResponse.challenges;
        }

        void ShowPopup()
        {
            var challenges = gamioManager.Challenges;
            if (challenges == null || challenges.Length == 0) return;
            var idx = System.Array.FindIndex(challenges, c => c.seedId == gamioManager.ChallengeId);
            if (idx < 0) idx = 0;
            var challenge = challenges[idx];
            if (string.IsNullOrEmpty(challenge?.gameType)) return;
            if (popup != null) return;
            active = true;

            popup = ChallengePopupUI.Show(transform, challenge.gameType);
            popup.OnBeginRequested += OnBeginGame;
            popup.OnCloseRequested += OnClosePopup;
        }

        void OnBeginGame()
        {
            gamioManager.SetChallengeActive(true);
            var challenge = System.Array.Find(gamioManager.Challenges, c => c.seedId == gamioManager.ChallengeId);
            uiEvents.RequestGameScene(gamesLibrary?.GetGameScene(challenge?.gameType ?? ""));
            popup = null;
        }

        void OnClosePopup()
        {
            popup = null;
        }

        void OnChallengeSolved(float solveTime)
        {
            gamioManager.SetStreakPending(true);
            _ = SubmitDaily(solveTime);
        }

        async Task SubmitDaily(float solveTime)
        {
            await Task.Delay(2000);

            try
            {
                var dailySublit = await cloudAPIService.SubmitDaily(gamioManager.ChallengeId, solveTime);
                if (dailySublit.success)
                {
                    await LeaderboardPopupUI.Show(
                new LeaderboardManager(cloudAPIService,
                GamioAppContext.Get<AuthService>()),
                LeaderboardMode.Result,
                gamioManager.ChallengeId);

                    gamioManager.SetStreak(dailySublit.streak);
                    gamioManager.SetDailyCompleted(true);
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
