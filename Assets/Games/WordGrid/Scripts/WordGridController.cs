using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamio.Games.WordGrid
{
    public class WordGridController
    {
        private readonly WordGridPuzzle puzzle;
        private int attempts;
        private bool solved;
        private HashSet<char> wrongLetters;
        private HashSet<char> usedLetters;

        public WordGridPuzzle Puzzle => puzzle;
        public int Attempts => attempts;
        public bool IsSolved => solved;
        public HashSet<char> WrongLetters => wrongLetters;
        public HashSet<char> UsedLetters => usedLetters;

        public event Action OnSolved;
        public event Action<int, TileState[]> OnAttemptComplete;
        public event Action OnWordSubmitted;

        public WordGridController(WordGridPuzzle pzl)
        {
            puzzle = pzl;
            attempts = 0;
            solved = false;
            wrongLetters = new HashSet<char>();
            usedLetters = new HashSet<char>();
        }

        public bool PlaceLetter(int cellIndex, char letter)
        {
            if (solved) return false;
            if (cellIndex < 0 || cellIndex >= puzzle.WordLength) return false;
            if (puzzle.Cells[cellIndex].State == TileState.Correct) return false;

            puzzle.Cells[cellIndex].PlacedLetter = letter;
            puzzle.Cells[cellIndex].State = TileState.Filled;
            return true;
        }

        public void RemoveLetter(int cellIndex)
        {
            if (solved) return;
            if (cellIndex < 0 || cellIndex >= puzzle.WordLength) return;
            if (puzzle.Cells[cellIndex].State == TileState.Correct) return;

            puzzle.Cells[cellIndex].PlacedLetter = null;
            puzzle.Cells[cellIndex].State = TileState.Empty;
        }

        public void ForceSolve()
        {
            if (solved) return;
            solved = true;
            for (int i = 0; i < puzzle.WordLength; i++)
            {
                puzzle.Cells[i].PlacedLetter = puzzle.TargetWord[i];
                puzzle.Cells[i].State = TileState.Correct;
            }
            OnSolved?.Invoke();
        }

        public void ResetPuzzle()
        {
            solved = false;
            wrongLetters.Clear();
            usedLetters.Clear();
            puzzle.Reset();
        }

        public bool Submit()
        {
            if (solved) return false;
            if (!puzzle.IsFullyFilled()) return false;

            attempts++;
            string guess = puzzle.GetCurrentGuess();
            string target = puzzle.TargetWord;

            TileState[] results = new TileState[puzzle.WordLength];
            bool[] targetUsed = new bool[puzzle.WordLength];

            for (int i = 0; i < puzzle.WordLength; i++)
            {
                if (guess[i] == target[i])
                {
                    results[i] = TileState.Correct;
                    targetUsed[i] = true;
                    usedLetters.Add(guess[i]);
                }
                else
                {
                    results[i] = TileState.Wrong;
                }
            }

            for (int i = 0; i < puzzle.WordLength; i++)
            {
                if (results[i] == TileState.Correct) continue;

                for (int j = 0; j < puzzle.WordLength; j++)
                {
                    if (!targetUsed[j] && guess[i] == target[j])
                    {
                        results[i] = TileState.WrongPosition;
                        targetUsed[j] = true;
                        usedLetters.Add(guess[i]);
                        break;
                    }
                }

                if (results[i] == TileState.Wrong)
                {
                    wrongLetters.Add(guess[i]);
                }
            }

            for (int i = 0; i < puzzle.WordLength; i++)
            {
                puzzle.Cells[i].State = results[i];
                if (results[i] == TileState.Wrong)
                    puzzle.Cells[i].PlacedLetter = null;
            }

            OnAttemptComplete?.Invoke(attempts, results);
            OnWordSubmitted?.Invoke();

            if (puzzle.AllCorrect())
            {
                solved = true;
                OnSolved?.Invoke();
            }

            return true;
        }

        public bool IsLetterWrong(char letter)
        {
            return wrongLetters.Contains(char.ToUpperInvariant(letter));
        }

        public bool IsLetterUsed(char letter)
        {
            return usedLetters.Contains(char.ToUpperInvariant(letter));
        }

        public void Dispose()
        {
            OnSolved = null;
            OnAttemptComplete = null;
            OnWordSubmitted = null;
        }
    }
}
