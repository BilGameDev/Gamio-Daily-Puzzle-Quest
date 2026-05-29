using System.Collections;
using Gamio.Core;
using Gamio.Features.DailyChallenge;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gamio.Root
{
    public class ChallengeManager : MonoBehaviour
    {
        private string gameType;
        private float gameStartTime;
        private float totalTime;
        private bool active;
        private bool timerActive;
        private ChallengePopupUI popup;

        public bool IsActive => active;
        public bool HasChallenge => !string.IsNullOrEmpty(gameType);
        public float TotalTime => active && timerActive
            ? totalTime + (Time.time - gameStartTime)
            : totalTime;

        public event System.Action<string, float> OnGameLaunchRequested;
        public event System.Action<float> OnChallengeCompleted;
        public event System.Action OnChallengeCancelled;

        public void SetChallengeData(string type, float totalTimeSeconds)
        {
            gameType = type;
            totalTime = totalTimeSeconds;
        }

        public void OnGameStarted()
        {
            gameStartTime = Time.time;
            timerActive = true;
        }

        public void OnPuzzleSolved(string gameId)
        {
            if (!active || string.IsNullOrEmpty(gameType)) return;

            float elapsed = Time.time - gameStartTime;
            timerActive = false;
            totalTime += elapsed;

            active = false;
            GamioEvents.IsChallengeActive = false;
            TutorialService.ChallengeModeActive = false;
            ClosePopup();

            var asyncOp = SceneManager.LoadSceneAsync("Home");
            StartCoroutine(CompleteAfterLoad(asyncOp, elapsed));
        }

        private IEnumerator CompleteAfterLoad(AsyncOperation asyncOp, float elapsed)
        {
            yield return asyncOp;
            yield return null;
            OnChallengeCompleted?.Invoke(elapsed);
        }

        public void Start()
        {
            if (string.IsNullOrEmpty(gameType)) return;
            active = true;
            GamioEvents.IsChallengeActive = true;
            TutorialService.ChallengeModeActive = true;
            ShowPopup();
        }

        public void BeginChallengeGame(string type, float time)
        {
            totalTime = time;
            OnGameLaunchRequested?.Invoke(type, time);
        }

        public void Cancel()
        {
            active = false;
            timerActive = false;
            GamioEvents.IsChallengeActive = false;
            TutorialService.ChallengeModeActive = false;
            ClosePopup();
            OnChallengeCancelled?.Invoke();
        }

        public void Reset()
        {
            gameType = null;
            totalTime = 0;
            active = false;
            GamioEvents.IsChallengeActive = false;
            TutorialService.ChallengeModeActive = false;
            ClosePopup();
        }

        private void ShowPopup()
        {
            if (string.IsNullOrEmpty(gameType)) return;
            ChallengePopupUI.OnBeginRequested = BeginChallengeGame;
            System.Func<float> timeProvider = () => TotalTime;
            if (popup == null)
            {
                popup = ChallengePopupUI.Create(transform, gameType, totalTime, timeProvider);
                popup.AnimateToFull();
            }
            else
            {
                popup.Refresh(gameType, totalTime, true, timeProvider);
            }
            popup.OnCloseRequested -= OnPopupClose;
            popup.OnCloseRequested += OnPopupClose;
        }

        private void OnPopupClose()
        {
            Cancel();
        }

        private void ClosePopup()
        {
            if (popup != null)
            {
                popup.Close();
                popup = null;
            }
        }
    }
}
