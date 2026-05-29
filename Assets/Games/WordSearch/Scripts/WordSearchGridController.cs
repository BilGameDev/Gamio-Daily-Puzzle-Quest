using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamio.Games.WordSearch
{
    public class WordSearchGridController
    {
        private readonly WordSearchPuzzle puzzle;
        private int dragStartR = -1, dragStartC = -1;
        private int dragEndR, dragEndC;
        private bool isDragging;
        private bool solved;

        public WordSearchPuzzle Puzzle => puzzle;
        public bool IsSolved => solved;
        public bool IsDragging => isDragging;
        public int DragStartRow => dragStartR;
        public int DragStartCol => dragStartC;
        public int DragEndRow => dragEndR;
        public int DragEndCol => dragEndC;

        public event Action OnSolved;
        public event Action<string> OnWordFound;

        public WordSearchGridController(WordSearchPuzzle puzzleData)
        {
            puzzle = puzzleData;
        }

        public void StartDrag(int row, int col)
        {
            if (solved) return;
            dragStartR = row;
            dragStartC = col;
            dragEndR = row;
            dragEndC = col;
            isDragging = true;
        }

        public void UpdateDrag(int row, int col)
        {
            if (!isDragging) return;
            dragEndR = Mathf.Clamp(row, 0, puzzle.GridSize - 1);
            dragEndC = Mathf.Clamp(col, 0, puzzle.GridSize - 1);
        }

        public bool EndDrag()
        {
            if (!isDragging) return false;
            isDragging = false;

            bool found = puzzle.TryFindWord(dragStartR, dragStartC, dragEndR, dragEndC, out var foundWord);

            dragStartR = -1;
            dragStartC = -1;

            if (found)
            {
                OnWordFound?.Invoke(foundWord);

                if (puzzle.CheckAllFound())
                {
                    solved = true;
                    OnSolved?.Invoke();
                }
            }

            return found;
        }

        public void CancelDrag()
        {
            isDragging = false;
            dragStartR = -1;
            dragStartC = -1;
        }

        public void Dispose()
        {
            OnSolved = null;
            OnWordFound = null;
        }
    }
}
