using System;
using System.Collections;
using System.Diagnostics;
using System.Timers;
using Gamio.Features.UI;
using TMPro;
using UnityEngine;

namespace Gamio.Core
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] Stopwatch stopwatch;
        [SerializeField] TextMeshProUGUI timerObject;
        [SerializeField] protected bool launchOnStart = true;

        [Header("Test")]
        [SerializeField] bool testMode = false;
        [SerializeField] protected Difficulty testDifficulty;
        [SerializeField] protected string testSeed;

        IUIEvents uIEvents;
        GamioManager gamioManager;

        protected void LaunchGame(IGame game)
        {
            game.Initialize();
            stopwatch = new Stopwatch();
            timerObject.gameObject.SetActive(false);

            if (testMode || gamioManager == null)
            {
                StartCoroutine(RunGame(game, testSeed, testDifficulty));
            }
            else
            {
                if (gamioManager.ChallengeActive)
                {
                    void Solved()
                    {
                        stopwatch.Stop();

                        uIEvents.SolvedChallenge(stopwatch.Elapsed.Seconds);
                        game.OnSolved -= Solved;
                    }

                    game.OnSolved += Solved;

                    timerObject.gameObject.SetActive(true);

                    StartCoroutine(RunGame(game, gamioManager.ChallengeSeed, Difficulty.Hard));
                }
                else
                {
                    StartCoroutine(RunGame(game, UnityEngine.Random.Range(1000, 9999).ToString(), EnumUtility.GetRandomEnum<Difficulty>()));
                }
            }

            SolvedHandler solvedHandler = new GameObject("SolvedHandler").AddComponent<SolvedHandler>();
            solvedHandler.Setup(game);
        }
        IEnumerator RunGame(IGame game, string seed, Difficulty difficulty)
        {
            if (gamioManager.ChallengeActive)
                yield return CountdownUI.Show(transform);

            stopwatch.Start();
            StartCoroutine(game.Run(seed, difficulty));
        }


        private void OnEnable()
        {
            uIEvents = GamioAppContext.Get<IUIEvents>();

            if (uIEvents != null)
            {
                uIEvents.OnResetRequested += ResetPuzzle;
                uIEvents.OnHintRequested += OnHint;
            }
        }

        private void OnDisable()
        {
            if (uIEvents != null)
            {
                uIEvents.OnResetRequested -= ResetPuzzle;
                uIEvents.OnHintRequested -= OnHint;
            }
        }

        protected virtual void Start()
        {
            gamioManager = GamioAppContext.Get<GamioManager>();
        }

        protected virtual void Update()
        {
            if (stopwatch.IsRunning)
            {
                timerObject.text = $"Time: {FormatTime(stopwatch.Elapsed)}";
            }
        }

        string FormatTime(TimeSpan ts)
        {
            if (ts.Hours > 0)
                return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.Minutes > 0)
                return $"{ts.Minutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }

        protected virtual void ResetPuzzle() { }
        protected virtual void OnHint() { }
    }
}
