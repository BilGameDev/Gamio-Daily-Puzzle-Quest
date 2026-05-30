using System;
using System.Collections;
using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Arrows
{
    public class ArrowsGame : IGame
    {
        public static ArrowsGame Instance { get; private set; }
        public static ArrowsGridController CurrentController { get; private set; }
        public static ArrowsGameSettingsSO ActiveSettings { get; set; }
        public static event Action<ArrowsGridController> OnControllerCreated;
        public static bool TutorialDeferred { get; set; }
        public static Difficulty CurrentDifficulty { get; private set; }
        public static string CurrentSeed { get; private set; }
        public static Vector2 CurrentCellSize => ActiveSettings?.GetConfig(CurrentDifficulty).cellSize ?? new Vector2(105, 105);

        public static void FireControllerCreated(ArrowsGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(ArrowsGridController controller)
        {
            CurrentController = controller;
        }

        public string GameId => "arrows";
        public string DisplayName => "Arrows";

        private ArrowsPuzzle puzzle;
        private ArrowsGridController gridController;
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
            ActiveSettings = Resources.Load<ArrowsGameSettingsSO>("ArrowsSettings");
            
            if (ActiveSettings == null)
            {
                Debug.LogError("ArrowsGameSettingsSO not found in Resources. Create one via Gamio/Arrows/Create Settings Asset.");
                return;
            }
            var config = ActiveSettings.GetConfig(CurrentDifficulty);
            var generator = new ArrowsGenerator(CurrentSeed);
            puzzle = generator.Generate(config.rows, config.cols, config.density);
            SetupController(puzzle);
        }

        private void SetupController(ArrowsPuzzle puzzleData)
        {
            gridController = new ArrowsGridController(puzzleData);
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