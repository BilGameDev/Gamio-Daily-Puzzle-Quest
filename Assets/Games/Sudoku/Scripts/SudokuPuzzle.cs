using System;
using System.Collections.Generic;

namespace Gamio.Games.Sudoku
{
    public class SudokuPuzzle
    {
        public int GridRows { get; }
        public int GridCols { get; }
        public int GridSize => Math.Max(GridRows, GridCols);
        public int BoxSize { get; }
        public int MaxNumber { get; }
        public SudokuCell[,] Cells { get; }
        public int[,] Solution { get; }

        private readonly Stack<(int row, int col, int prevValue)> history;

        public SudokuPuzzle(int gridRows, int gridCols, int boxSize, SudokuCell[,] cells, int[,] solution, int maxNumber = -1)
        {
            GridRows = gridRows;
            GridCols = gridCols;
            BoxSize = boxSize;
            MaxNumber = maxNumber > 0 ? maxNumber : Math.Max(GridRows, GridCols);
            Cells = cells;
            Solution = solution;
            history = new Stack<(int, int, int)>();
        }

        public bool SetValue(int row, int col, int value)
        {
            if (row < 0 || row >= GridRows || col < 0 || col >= GridCols)
                return false;
            if (Cells[row, col].IsGiven)
                return false;
            history.Push((row, col, Cells[row, col].Value));
            var cell = Cells[row, col];
            cell.Value = value;
            Cells[row, col] = cell;
            return true;
        }

        public bool Undo()
        {
            if (history.Count == 0) return false;
            var (row, col, prevValue) = history.Pop();
            var cell = Cells[row, col];
            cell.Value = prevValue;
            Cells[row, col] = cell;
            return true;
        }

        public bool IsSolved()
        {
            for (int r = 0; r < GridRows; r++)
                for (int c = 0; c < GridCols; c++)
                    if (Cells[r, c].Value != Solution[r, c]) return false;
            return true;
        }

        public bool HasConflict(int row, int col, int number)
        {
            for (int c = 0; c < GridCols; c++)
                if (c != col && Cells[row, c].Value == number)
                    return true;
            for (int r = 0; r < GridRows; r++)
                if (r != row && Cells[r, col].Value == number)
                    return true;
            int boxR = row / BoxSize * BoxSize;
            int boxC = col / BoxSize * BoxSize;
            for (int r = boxR; r < boxR + BoxSize; r++)
                for (int c = boxC; c < boxC + BoxSize; c++)
                    if ((r != row || c != col) && Cells[r, c].Value == number)
                        return true;
            return false;
        }

        public bool IsCorrect(int row, int col)
        {
            return Cells[row, col].Value == Solution[row, col];
        }

        public void ResetPlayerCells()
        {
            history.Clear();
            for (int r = 0; r < GridRows; r++)
                for (int c = 0; c < GridCols; c++)
                    if (!Cells[r, c].IsGiven)
                        Cells[r, c] = new SudokuCell(r, c);
        }
    }
}
