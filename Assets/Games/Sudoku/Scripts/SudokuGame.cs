using System;
using System.Collections;
using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Sudoku
{
    public class SudokuGame : IGame
    {
        public static SudokuGame Instance { get; private set; }
        public static SudokuGridController CurrentController { get; private set; }
        public static SudokuGameSettingsSO ActiveSettings { get; private set; }
        public static event Action<SudokuGridController> OnControllerCreated;
        public static Difficulty CurrentDifficulty { get; private set; }
        public static int CurrentSeed { get; private set; }
        public static bool TutorialDeferred { get; set; }

        public static void FireControllerCreated(SudokuGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(SudokuGridController controller)
        {
            CurrentController = controller;
        }

        public string GameId => "sudoku";
        public string DisplayName => "Sudoku";

        private SudokuPuzzle puzzle;
        private SudokuGridController gridController;

        public SudokuPuzzle Puzzle => puzzle;
        public SudokuGridController Grid => gridController;
        public event Action OnSolved;

        public void Initialize()
        {
            Instance = this;
        }

        public IEnumerator Run(int seed, Difficulty difficulty)
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
            ActiveSettings = Resources.Load<SudokuGameSettingsSO>("SudokuGameSettings");

            var generator = new SudokuGenerator(CurrentSeed);
            puzzle = generator.Generate(ActiveSettings.GridSize, ActiveSettings.BoxSize, ActiveSettings.GetCellsToRemove(CurrentDifficulty));

            SetupController(puzzle);
        }

        private void SetupController(SudokuPuzzle puzzleData)
        {
            gridController = new SudokuGridController(puzzleData);
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
            CurrentController = null;
            OnControllerCreated = null;

            if (gridController != null)
            {
                gridController.OnSolved -= OnPuzzleSolved;
                gridController.Dispose();
                gridController = null;
            }
            puzzle = null;
            ActiveSettings = null;
        }
    }
}
