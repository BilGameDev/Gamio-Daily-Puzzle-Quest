using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamio.Games.Hitori
{
    public class HitoriGenerator
    {
        private readonly string seed;
        private Random rng;

        public HitoriGenerator(string seedValue)
        {
            seed = seedValue;
        }

        public HitoriPuzzle Generate(int gridSize)
        {
            rng = new Random(seed.GetHashCode());

            var solution = new int[gridSize, gridSize];
            GenerateLatinSquare(solution);

            var isBlack = SelectBlackCells(gridSize);

            var cells = new HitoriCell[gridSize, gridSize];
            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    int number;
                    if (isBlack[r, c])
                    {
                        var whitePositions = new List<int>();
                        for (int cc = 0; cc < gridSize; cc++)
                            if (!isBlack[r, cc]) whitePositions.Add(cc);
                        if (whitePositions.Count > 0)
                        {
                            int wc = whitePositions[rng.Next(whitePositions.Count)];
                            number = solution[r, wc];
                        }
                        else
                        {
                            number = solution[r, c];
                        }
                    }
                    else
                    {
                        number = solution[r, c];
                    }

                    cells[r, c] = new HitoriCell
                    {
                        Row = r,
                        Col = c,
                        Number = number,
                        IsBlackInSolution = isBlack[r, c]
                    };
                }
            }

            return new HitoriPuzzle(gridSize, cells);
        }

        private void GenerateLatinSquare(int[,] grid)
        {
            int n = grid.GetLength(0);

            for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                grid[r, c] = (r + c) % n + 1;

            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                for (int c = 0; c < n; c++)
                {
                    int tmp = grid[i, c];
                    grid[i, c] = grid[j, c];
                    grid[j, c] = tmp;
                }
            }

            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                for (int r = 0; r < n; r++)
                {
                    int tmp = grid[r, i];
                    grid[r, i] = grid[r, j];
                    grid[r, j] = tmp;
                }
            }

            var mapping = Enumerable.Range(1, n).OrderBy(x => rng.Next()).ToArray();
            for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                grid[r, c] = mapping[grid[r, c] - 1];
        }

        private bool[,] SelectBlackCells(int gridSize)
        {
            var isBlack = new bool[gridSize, gridSize];
            var candidates = new List<(int r, int c)>();
            for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                candidates.Add((r, c));

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }

            int target = Math.Max(1, gridSize * gridSize / 5);
            int placed = 0;

            foreach (var (r, c) in candidates)
            {
                if (placed >= target) break;

                bool adjacent = false;
                foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    int nr = r + dr, nc = c + dc;
                    if (nr >= 0 && nr < gridSize && nc >= 0 && nc < gridSize && isBlack[nr, nc])
                    {
                        adjacent = true;
                        break;
                    }
                }
                if (adjacent) continue;

                isBlack[r, c] = true;
                if (AreWhiteCellsConnected(gridSize, isBlack))
                {
                    placed++;
                }
                else
                {
                    isBlack[r, c] = false;
                }
            }

            return isBlack;
        }

        private static bool AreWhiteCellsConnected(int gridSize, bool[,] isBlack)
        {
            int startR = -1, startC = -1;
            int whiteCount = 0;

            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    if (!isBlack[r, c])
                    {
                        whiteCount++;
                        if (startR == -1) { startR = r; startC = c; }
                    }
                }
            }

            if (whiteCount <= 1) return true;

            var visited = new bool[gridSize, gridSize];
            var queue = new Queue<(int, int)>();
            queue.Enqueue((startR, startC));
            visited[startR, startC] = true;
            int count = 1;

            while (queue.Count > 0)
            {
                var (r, c) = queue.Dequeue();
                foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    int nr = r + dr, nc = c + dc;
                    if (nr >= 0 && nr < gridSize && nc >= 0 && nc < gridSize && !visited[nr, nc] && !isBlack[nr, nc])
                    {
                        visited[nr, nc] = true;
                        count++;
                        queue.Enqueue((nr, nc));
                    }
                }
            }

            return count == whiteCount;
        }
    }
}
