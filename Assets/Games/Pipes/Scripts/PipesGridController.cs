using System;

namespace Gamio.Games.Pipes
{
    public class PipesGridController
    {
        private readonly PipesPuzzle puzzle;
        private bool solved;

        public PipesPuzzle Puzzle => puzzle;
        public bool IsSolved => solved;

        public event Action OnSolved;
        public event Action<int, int> OnCellTapped;

        public PipesGridController(PipesPuzzle puzzleData)
        {
            puzzle = puzzleData;
        }

        public void TapCell(int row, int col)
        {
            if (solved) return;
            if (puzzle.Cells[row, col].IsFixed) return;
            if (puzzle.Cells[row, col].Type == PipeType.Empty) return;
            puzzle.CycleRotation(row, col);
            OnCellTapped?.Invoke(row, col);
        }

        public void Undo()
        {
            if (solved) return;
            puzzle.Undo();
        }

        public void Check()
        {
            if (solved) return;
            if (puzzle.IsSolved())
            {
                solved = true;
                OnSolved?.Invoke();
            }
        }

        public void ForceSolve()
        {
            if (solved) return;
            solved = true;
            OnSolved?.Invoke();
        }

        public void ResetPuzzle()
        {
            puzzle.Scramble();
            solved = false;
        }

        public void Dispose()
        {
            OnSolved = null;
            OnCellTapped = null;
        }
    }
}
