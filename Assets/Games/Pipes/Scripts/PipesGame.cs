using System;
using System.Collections;
using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Pipes
{
    public class PipesGame : IGame
    {
        public static PipesGame Instance { get; private set; }
        public static PipesGridController CurrentController { get; private set; }
        public static PipesGameSettingsSO ActiveSettings { get; private set; }
        public static Difficulty CurrentDifficulty { get; private set; }
        public static string CurrentSeed { get; private set; }
        public static MonoBehaviour TutorialUIPrefab { get; set; }
        public static event Action<PipesGridController> OnControllerCreated;
        public static bool TutorialDeferred { get; set; }

        public static void FireControllerCreated(PipesGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(PipesGridController controller)
        {
            CurrentController = controller;
        }

        public string GameId => "pipes";
        public string DisplayName => "Pipes";
        private PipesPuzzle puzzle;
        private PipesGridController gridController;

        public PipesPuzzle Puzzle => puzzle;
        public PipesGridController Grid => gridController;
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
            ActiveSettings = Resources.Load<PipesGameSettingsSO>("PipesSettings");

            var config = ActiveSettings.GetConfig(CurrentDifficulty);
            var generator = new PipesGenerator(CurrentSeed);
            puzzle = generator.Generate(config.gridSize);

            SetupController(puzzle);
        }

        private void SetupController(PipesPuzzle puzzleData)
        {
            gridController = new PipesGridController(puzzleData);
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
