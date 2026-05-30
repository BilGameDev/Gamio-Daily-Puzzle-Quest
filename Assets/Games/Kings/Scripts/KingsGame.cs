using System;
using System.Collections;
using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Kings
{
    public class KingsGame : IGame
    {
        public static KingsGame Instance { get; private set; }
        public static KingsGridController CurrentController { get; private set; }
        public static KingsGameSettingsSO ActiveSettings { get; private set; }
        public static Difficulty CurrentDifficulty { get; private set; }
        public static string CurrentSeed { get; private set; }
        public static event Action<KingsGridController> OnControllerCreated;

        public static bool TutorialDeferred { get; set; }

        public static void FireControllerCreated(KingsGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(KingsGridController controller)
        {
            CurrentController = controller;
        }

        public string GameId => "kings";
        public string DisplayName => "Crowns";

        private KingsPuzzle puzzle;
        private KingsGridController gridController;

        public KingsPuzzle Puzzle => puzzle;
        public KingsGridController Grid => gridController;
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
            ActiveSettings = Resources.Load<KingsGameSettingsSO>("KingsSettings");

            var config = ActiveSettings.GetConfig(CurrentDifficulty);
            var generator = new KingsGenerator(CurrentSeed);

            puzzle = generator.Generate(config.gridSize);
            SetupController(puzzle);
        }

        private void SetupController(KingsPuzzle inputPuzzle)
        {
            gridController = new KingsGridController(inputPuzzle);
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