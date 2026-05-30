using System;
using System.Collections.Generic;

namespace Gamio.Games.Sudoku
{
    public class SudokuGenerator
    {
        private readonly string seed;
        private Random rng;

        public SudokuGenerator(string seedValue)
        {
            seed = seedValue;
        }

        public SudokuPuzzle Generate(int gridSize, int boxSize, int cellsToRemove)
        {
            rng = new Random(seed.GetHashCode());
            var size = gridSize;
            var board = new int[size, size];
            SolveBoard(board, size, boxSize);
            var solution = (int[,])board.Clone();
            var cells = new SudokuCell[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c] = new SudokuCell(r, c) { Value = board[r, c], IsGiven = true };
            RemoveCells(cells, size, cellsToRemove);
            return new SudokuPuzzle(size, size, boxSize, cells, solution);
        }

        private bool SolveBoard(int[,] board, int size, int boxSize)
        {
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (board[r, c] == 0)
                    {
                        var nums = GetShuffledNumbers(size);
                        foreach (var num in nums)
                        {
                            if (IsValid(board, r, c, num, size, boxSize))
                            {
                                board[r, c] = num;
                                if (SolveBoard(board, size, boxSize))
                                    return true;
                                board[r, c] = 0;
                            }
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        private bool IsValid(int[,] board, int row, int col, int num, int size, int boxSize)
        {
            for (int c = 0; c < size; c++)
                if (board[row, c] == num) return false;
            for (int r = 0; r < size; r++)
                if (board[r, col] == num) return false;
            int boxR = row / boxSize * boxSize;
            int boxC = col / boxSize * boxSize;
            for (int r = boxR; r < boxR + boxSize; r++)
                for (int c = boxC; c < boxC + boxSize; c++)
                    if (board[r, c] == num) return false;
            return true;
        }

        private List<int> GetShuffledNumbers(int size)
        {
            var nums = new List<int>();
            for (int i = 1; i <= size; i++) nums.Add(i);
            for (int i = nums.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (nums[i], nums[j]) = (nums[j], nums[i]);
            }
            return nums;
        }

        private void RemoveCells(SudokuCell[,] cells, int size, int count)
        {
            var indices = new List<int>();
            for (int i = 0; i < size * size; i++) indices.Add(i);
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            for (int i = 0; i < Math.Min(count, indices.Count); i++)
            {
                int r = indices[i] / size;
                int c = indices[i] % size;
                cells[r, c] = new SudokuCell(r, c);
            }
        }
    }
}
