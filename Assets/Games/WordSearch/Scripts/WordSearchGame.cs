using System;
using System.Collections;
using System.Collections.Generic;
using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.WordSearch
{
    public class WordSearchGame : IGame
    {
        public static WordSearchGame Instance { get; private set; }
        public static WordSearchGridController CurrentController { get; private set; }
        public static WordSearchGameSettingsSO ActiveSettings { get; private set; }
        public static Difficulty CurrentDifficulty { get; private set; }
        public static string CurrentSeed { get; private set; }
        public static event Action<WordSearchGridController> OnControllerCreated;
        public static bool TutorialDeferred { get; set; }

        public static void FireControllerCreated(WordSearchGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(WordSearchGridController controller)
        {
            CurrentController = controller;
        }

        public string GameId => "wordsearch";
        public string DisplayName => "Word Search";

        private WordSearchPuzzle puzzle;
        private WordSearchGridController gridController;

        public WordSearchPuzzle Puzzle => puzzle;
        public WordSearchGridController Grid => gridController;
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
            ActiveSettings = Resources.Load<WordSearchGameSettingsSO>("WordSearchGameSettings");

            var config = ActiveSettings.GetConfig(CurrentDifficulty);

            var textAsset = ActiveSettings.WordList ?? Resources.Load<TextAsset>("words");
            var allWords = new List<string>(
                textAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            var generator = new WordSearchGenerator(CurrentSeed);
            puzzle = generator.Generate(config.gridSize, config.wordCount, allWords);

            SetupController(puzzle);
        }

        private void SetupController(WordSearchPuzzle puzzleData)
        {
            gridController = new WordSearchGridController(puzzleData);
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
