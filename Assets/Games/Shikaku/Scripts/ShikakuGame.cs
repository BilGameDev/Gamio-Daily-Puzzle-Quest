using System;
using System.Collections;
using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Shikaku
{
    public class ShikakuGame : IGame
    {
        public static ShikakuGame Instance { get; private set; }
        public static ShikakuGridController CurrentController { get; private set; }
        public static ShikakuGameSettingsSO ActiveSettings { get; private set; }
        public static Difficulty CurrentDifficulty { get; private set; }
        public static string CurrentSeed { get; private set; }
        public static event Action<ShikakuGridController> OnControllerCreated;
        public static bool TutorialDeferred { get; set; }

        public static void FireControllerCreated(ShikakuGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(ShikakuGridController controller)
        {
            CurrentController = controller;
        }

        public string GameId => "shikaku";
        public string DisplayName => "Shikaku";

        private ShikakuPuzzle puzzle;
        private ShikakuGridController gridController;

        public ShikakuPuzzle Puzzle => puzzle;
        public ShikakuGridController Grid => gridController;
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
            ActiveSettings = Resources.Load<ShikakuGameSettingsSO>("ShikakuSettings");

            var config = ActiveSettings.GetConfig(CurrentDifficulty);
            var generator = new ShikakuGenerator(CurrentSeed);
            puzzle = generator.Generate(config.gridSize);

            SetupController(puzzle);
        }

        private void SetupController(ShikakuPuzzle pz)
        {
            gridController = new ShikakuGridController(pz);
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
        }
    }
}
