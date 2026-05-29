using System;

namespace Gamio.Games.Hitori
{
    public class HitoriGridController
    {
        private readonly HitoriPuzzle puzzle;
        private bool solved;

        public HitoriPuzzle Puzzle => puzzle;
        public bool IsSolved => solved;

        public event Action OnSolved;
        public event Action<int, int> OnCellTapped;

        public HitoriGridController(HitoriPuzzle puzzleData)
        {
            puzzle = puzzleData;
        }

        public void TapCell(int row, int col)
        {
            if (solved) return;
            puzzle.CycleState(row, col);
            OnCellTapped?.Invoke(row, col);
        }

        public void Undo()
        {
            if (solved) return;
            puzzle.Undo();
        }

        public void Check()
        {
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

        public void Dispose()
        {
            OnSolved = null;
            OnCellTapped = null;
        }
    }
}
