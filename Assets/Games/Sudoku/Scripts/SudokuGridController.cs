using System;

namespace Gamio.Games.Sudoku
{
    public class SudokuGridController
    {
        private readonly SudokuPuzzle puzzle;
        private bool solved;
        private int selectedRow = -1;
        private int selectedCol = -1;

        public SudokuPuzzle Puzzle => puzzle;
        public bool IsSolved => solved;
        public int SelectedRow => selectedRow;
        public int SelectedCol => selectedCol;

        public event Action OnSolved;
        public event Action<int, int> OnWrongNumber;
        public event Action OnSelectionChanged;
        public event Action OnCellChanged;

        public SudokuGridController(SudokuPuzzle puzzleData)
        {
            puzzle = puzzleData;
        }

        public void SelectCell(int row, int col)
        {
            if (solved) return;
            if (row < 0 || row >= puzzle.GridRows || col < 0 || col >= puzzle.GridCols)
                return;
            selectedRow = row;
            selectedCol = col;
            OnSelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            selectedRow = -1;
            selectedCol = -1;
            OnSelectionChanged?.Invoke();
        }

        public void EnterNumber(int number)
        {
            if (solved || selectedRow < 0 || selectedCol < 0) return;
            if (number < 0 || number > puzzle.MaxNumber) return;
            if (puzzle.Cells[selectedRow, selectedCol].IsGiven) return;

            puzzle.SetValue(selectedRow, selectedCol, number);
            OnCellChanged?.Invoke();

            if (number > 0 && !puzzle.IsCorrect(selectedRow, selectedCol))
            {
                OnWrongNumber?.Invoke(selectedRow, selectedCol);
            }

            if (!solved && puzzle.IsSolved())
            {
                solved = true;
                OnSolved?.Invoke();
            }
        }

        public void Undo()
        {
            if (solved) return;
            if (puzzle.Undo())
                OnCellChanged?.Invoke();
        }

        public void ResetPuzzle()
        {
            puzzle.ResetPlayerCells();
            solved = false;
            ClearSelection();
            OnCellChanged?.Invoke();
        }

        public void Dispose()
        {
            OnSolved = null;
            OnWrongNumber = null;
            OnSelectionChanged = null;
            OnCellChanged = null;
        }
    }
}
