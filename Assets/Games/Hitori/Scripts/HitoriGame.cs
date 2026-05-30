using System;
using System.Collections;
using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Hitori
{
    public class HitoriGame : IGame
    {
        public static HitoriGame Instance { get; private set; }
        public static HitoriGridController CurrentController { get; private set; }
        public static HitoriGameSettingsSO ActiveSettings { get; private set; }
        public static Difficulty CurrentDifficulty { get; private set; }
        public static string CurrentSeed { get; private set; }
        public static event Action<HitoriGridController> OnControllerCreated;
        public static bool TutorialDeferred { get; set; }

        public static void FireControllerCreated(HitoriGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(HitoriGridController controller)
        {
            CurrentController = controller;
        }

        public string GameId => "hitori";
        public string DisplayName => "Hitori";
        private HitoriPuzzle puzzle;
        private HitoriGridController gridController;

        public HitoriPuzzle Puzzle => puzzle;
        public HitoriGridController Grid => gridController;
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
            ActiveSettings = Resources.Load<HitoriGameSettingsSO>("HitoriSettings");

            var generator = new HitoriGenerator(CurrentSeed);
            puzzle = generator.Generate(ActiveSettings.GetGridSize(CurrentDifficulty));
            SetupController(puzzle);
        }

        private void SetupController(HitoriPuzzle puzzleData)
        {
            gridController = new HitoriGridController(puzzleData);
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
