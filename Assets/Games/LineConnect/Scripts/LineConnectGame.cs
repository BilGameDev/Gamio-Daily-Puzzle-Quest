using System;
using System.Collections;
using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.LineConnect
{
    public class LineConnectGame : IGame
    {
        public static LineConnectGame Instance { get; private set; }
        public static LineConnectGridController CurrentController { get; private set; }
        public static LineConnectGameSettingsSO ActiveSettings { get; private set; }
        public static event Action<LineConnectGridController> OnControllerCreated;
        public static bool TutorialDeferred { get; set; }
        public static Difficulty CurrentDifficulty { get; private set; }
        public static string CurrentSeed { get; private set; }

        public static void FireControllerCreated(LineConnectGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(LineConnectGridController controller)
        {
            CurrentController = controller;
        }

        public static Vector2 CurrentCellSize => ActiveSettings?.GetConfig(CurrentDifficulty).cellSize ?? new Vector2(100, 110);

        public string GameId => "lineconnect";
        public string DisplayName => "Line Connect";

        private LineConnectPuzzle puzzle;
        private LineConnectGridController gridController;

        public LineConnectPuzzle Puzzle => puzzle;
        public LineConnectGridController Grid => gridController;
        public event Action OnSolved;

        public void Initialize()
        {
            Instance = this;
        }

        public IEnumerator Run(string seed, Difficulty difficulty)
        {
            CurrentDifficulty = difficulty;
            CurrentSeed = seed;

            if (TutorialDeferred)
                yield break;

            InternalRun();
            yield return null;
        }

        public void StartGame()
        {
            TutorialDeferred = false;
            InternalRun();
        }

        private void InternalRun()
        {
            ActiveSettings = Resources.Load<LineConnectGameSettingsSO>("LineConnectSettings");
            if (ActiveSettings == null)
            {
                Debug.LogError("LineConnectGameSettingsSO not found in Resources. Create one via Gamio/LineConnect/Create Settings Asset.");
                return;
            }

            var generator = new LineConnectGenerator(CurrentSeed);
            puzzle = generator.Generate(ActiveSettings.GetConfig(CurrentDifficulty).gridSize);
            SetupController(puzzle);
        }

        private void SetupController(LineConnectPuzzle puzzleData)
        {
            gridController = new LineConnectGridController(puzzleData);
            gridController.OnSolved += OnPuzzleSolved;

            CurrentController = gridController;
            OnControllerCreated?.Invoke(gridController);
        }

        private void OnPuzzleSolved()
        {
            OnSolved?.Invoke();
        }

        public void Cleanup()
        {
            ActiveSettings = null;
            CurrentController = null;
            OnControllerCreated = null;

            if (gridController != null)
            {
                gridController.OnSolved -= OnPuzzleSolved;
                gridController.Dispose();
                gridController = null;
            }
            puzzle = null;
        }
    }
}