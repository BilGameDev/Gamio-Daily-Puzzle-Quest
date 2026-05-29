using System;
using System.Collections.Generic;
using Gamio.Core;

namespace Gamio.Games.Kings
{
    public class KingsGridController
    {
        private readonly KingsPuzzle puzzle;
        private bool solved;

        public KingsPuzzle Puzzle => puzzle;
        public bool IsSolved => solved;

        public event Action OnSolved;
        public event Action<int, int> OnCellChanged;
        public event Action<int, int, int, int> OnPlacementDenied;

        public KingsGridController(KingsPuzzle inputPuzzle)
        {
            puzzle = inputPuzzle;
        }

        public bool TapCell(int row, int col)
        {
            if (solved) return false;

            var state = puzzle.GetState(row, col);
            if (state != KingsCellState.Empty)
            {
                if (puzzle.TryRemove(row, col, out var cascade))
                {
                    OnCellChanged?.Invoke(row, col);
                    if (cascade != null)
                    {
                        foreach (var (nr, nc) in cascade)
                            OnCellChanged?.Invoke(nr, nc);
                    }
                    CheckSolved();
                    return true;
                }
                return false;
            }

            if (puzzle.TryPlaceNull(row, col))
            {
                OnCellChanged?.Invoke(row, col);
                CheckSolved();
                return true;
            }
            return false;
        }

        public bool HoldCell(int row, int col)
        {
            if (solved) return false;

            var state = puzzle.GetState(row, col);

            if (state == KingsCellState.King)
            {
                if (puzzle.TryRemove(row, col, out var cascade))
                {
                    OnCellChanged?.Invoke(row, col);
                    if (cascade != null)
                        foreach (var (nr, nc) in cascade)
                            OnCellChanged?.Invoke(nr, nc);
                    CheckSolved();
                    return true;
                }
                return false;
            }

            if (state == KingsCellState.Null)
                puzzle.TryRemove(row, col, out _);

            if (puzzle.TryPlaceKing(row, col, out var autoFilled))
            {
                OnCellChanged?.Invoke(row, col);
                foreach (var (nr, nc) in autoFilled)
                    OnCellChanged?.Invoke(nr, nc);
                CheckSolved();
                return true;
            }

            if (puzzle.FindConflict(row, col, out int conflictR, out int conflictC))
                OnPlacementDenied?.Invoke(row, col, conflictR, conflictC);

            return false;
        }

        public bool Undo()
        {
            if (solved) return false;
            solved = false;
            return puzzle.Undo();
        }

        private void CheckSolved()
        {
            if (solved) return;
            if (puzzle.IsSolved())
            {
                solved = true;
                OnSolved?.Invoke();
            }
        }

        public void ResetPuzzle()
        {
            puzzle.Reset();
            solved = false;
        }

        public bool CanPlaceKing(int row, int col)
        {
            if (puzzle.GetState(row, col) != KingsCellState.Empty) return false;
            return !puzzle.HasAdjacentKing(row, col);
        }

        public void Dispose()
        {
            OnSolved = null;
            OnCellChanged = null;
            OnPlacementDenied = null;
        }
    }
}