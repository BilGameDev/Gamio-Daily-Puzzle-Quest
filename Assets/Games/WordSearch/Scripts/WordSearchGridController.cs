using System;
using UnityEngine;

namespace Gamio.Games.WordSearch
{
    public class WordSearchGridController
    {
        private readonly WordSearchPuzzle puzzle;
        private int dragStartR = -1, dragStartC = -1;
        private int dragEndR, dragEndC;
        private int lockedDirR, lockedDirC;
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
            lockedDirR = 0;
            lockedDirC = 0;
            isDragging = true;
        }

        public void UpdateDrag(int row, int col)
        {
            if (!isDragging) return;

            if (lockedDirR == 0 && lockedDirC == 0)
            {
                int dr = row - dragStartR;
                int dc = col - dragStartC;

                if (dr == 0 && dc == 0)
                {
                    dragEndR = dragStartR;
                    dragEndC = dragStartC;
                    return;
                }

                if (dr == 0)
                {
                    lockedDirR = 0;
                    lockedDirC = dc > 0 ? 1 : -1;
                }
                else if (dc == 0)
                {
                    lockedDirR = dr > 0 ? 1 : -1;
                    lockedDirC = 0;
                }
                else if (Mathf.Abs(dr) == Mathf.Abs(dc))
                {
                    lockedDirR = dr > 0 ? 1 : -1;
                    lockedDirC = dc > 0 ? 1 : -1;
                }
                else
                {
                    dragEndR = Mathf.Clamp(row, 0, puzzle.GridSize - 1);
                    dragEndC = Mathf.Clamp(col, 0, puzzle.GridSize - 1);
                    return;
                }
            }

            int distR = lockedDirR != 0 ? (row - dragStartR) * lockedDirR : int.MaxValue;
            int distC = lockedDirC != 0 ? (col - dragStartC) * lockedDirC : int.MaxValue;
            int dist = Mathf.Min(distR, distC);
            dist = Mathf.Max(0, dist);

            dragEndR = Mathf.Clamp(dragStartR + lockedDirR * dist, 0, puzzle.GridSize - 1);
            dragEndC = Mathf.Clamp(dragStartC + lockedDirC * dist, 0, puzzle.GridSize - 1);
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
