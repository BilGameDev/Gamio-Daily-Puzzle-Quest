using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamio.Core;

namespace Gamio.Games.WordGrid
{
    public class WordGridGame : IGame
    {
        public static WordGridGame Instance { get; private set; }
        public static WordGridController CurrentController { get; private set; }
        public static WordGridGameSettingsSO ActiveSettings { get; set; }
        public static event Action<WordGridController> OnControllerCreated;
        public static Difficulty CurrentDifficulty { get; private set; }
        public static string CurrentSeed { get; private set; }
        public static bool TutorialDeferred { get; set; }

        public static void FireControllerCreated(WordGridController controller)
        {
            OnControllerCreated?.Invoke(controller);
        }

        public static void SetCurrentController(WordGridController controller)
        {
            CurrentController = controller;
        }

        public string GameId => "wordgrid";
        public string DisplayName => "Word Grid";

        private WordGridPuzzle puzzle;
        private WordGridController gridController;

        public WordGridPuzzle Puzzle => puzzle;
        public WordGridController Grid => gridController;
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

            ActiveSettings = Resources.Load<WordGridGameSettingsSO>("WordGridGameSettings");

            int wordLength = ActiveSettings.WordLength;

            var wordList = wordLength <= 4
                ? (ActiveSettings.WordList4 ?? Resources.Load<TextAsset>("words_4"))
                : (ActiveSettings.WordList6 ?? Resources.Load<TextAsset>("words_6"));

            var allWords = new List<string>(
                wordList.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            var generator = new WordGridGenerator(CurrentSeed);
            puzzle = generator.Generate(wordLength, allWords);

            SetupController(puzzle, CurrentDifficulty);
        }

        private void SetupController(WordGridPuzzle pzl, Difficulty difficulty)
        {
            gridController = new WordGridController(pzl);
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
