using System.Collections.Generic;
using UnityEngine;

namespace Gamio.Games.Arrows
{
    public class ArrowsGenerator
    {
        private readonly int seed;

        public ArrowsGenerator(int seedValue)
        {
            seed = seedValue;
        }

        public ArrowsPuzzle Generate(int rows, int cols, float density)
        {
            var rng = new System.Random(seed);
            rows = Mathf.Max(3, rows);
            cols = Mathf.Max(3, cols);
            float d = Mathf.Clamp01(density);

            int obstacleCount = Mathf.Max(1, Mathf.RoundToInt(cols * d * 0.8f));
            int availCells = rows * cols - Mathf.Min(obstacleCount, rows * cols - 1);
            int minChainDepth = Mathf.Max(1, Mathf.FloorToInt(d * 3));

            int targetTiles = Mathf.Max(6, Mathf.RoundToInt(availCells * Mathf.Lerp(0.55f, 0.80f, d)));

            for (int attempt = 0; attempt < 300; attempt++)
            {
                var cells = CreateEmptyGrid(rows, cols);
                PlaceObstacles(cells, obstacleCount, rng);

                var positions = GetShuffledPositions(rows, cols, cells, rng);

                int placed = 0;
                foreach (var (r, c) in positions)
                {
                    if (placed >= targetTiles) break;
                    cells[r, c] = new ArrowsCell
                    {
                        Row = r,
                        Col = c,
                        Direction = (ArrowDirection)rng.Next(4),
                        IsEmpty = false
                    };
                    placed++;
                }

                if (placed < targetTiles) continue;

                int chainDepth = MeasureChainDepth(cells, rows, cols);
                if (chainDepth >= minChainDepth)
                    return new ArrowsPuzzle(rows, cols, cells);

                if (attempt > 0 && attempt % 50 == 0)
                    targetTiles = Mathf.Max(6, targetTiles - 1);
            }

            return FallbackGenerate(rows, cols, obstacleCount, minChainDepth, targetTiles, rng);
        }

        private ArrowsPuzzle FallbackGenerate(int rows, int cols, int obstacleCount, int minChainDepth, int targetTiles, System.Random rng)
        {
            int count = targetTiles;
            while (count >= 6)
            {
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    var cells = CreateEmptyGrid(rows, cols);
                    PlaceObstacles(cells, obstacleCount, rng);

                    var positions = GetShuffledPositions(rows, cols, cells, rng);
                    Shuffle(positions, rng);

                    int placed = 0;
                    foreach (var (r, c) in positions)
                    {
                        if (placed >= count) break;
                        cells[r, c] = new ArrowsCell { Row = r, Col = c, Direction = (ArrowDirection)rng.Next(4), IsEmpty = false };
                        placed++;
                    }

                    if (placed < count) continue;

                    int chainDepth = MeasureChainDepth(cells, rows, cols);
                    if (chainDepth >= minChainDepth)
                        return new ArrowsPuzzle(rows, cols, cells);
                }
                count--;
            }

            var final = CreateEmptyGrid(rows, cols);
            PlaceObstacles(final, obstacleCount, rng);
            return new ArrowsPuzzle(rows, cols, final);
        }

        private ArrowsCell[,] CreateEmptyGrid(int rows, int cols)
        {
            var cells = new ArrowsCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cells[r, c] = new ArrowsCell { Row = r, Col = c, Direction = ArrowDirection.None, IsEmpty = true };
            return cells;
        }

        private void PlaceObstacles(ArrowsCell[,] cells, int count, System.Random rng)
        {
            if (count <= 0) return;
            int rows = cells.GetLength(0), cols = cells.GetLength(1);
            var positions = new List<(int, int)>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (cells[r, c].IsEmpty)
                        positions.Add((r, c));
            Shuffle(positions, rng);
            int placed = 0;
            foreach (var (r, c) in positions)
            {
                if (placed >= count) break;
                cells[r, c] = new ArrowsCell { Row = r, Col = c, Direction = ArrowDirection.None, IsEmpty = false, IsObstacle = true };
                placed++;
            }
        }

        private List<(int, int)> GetShuffledPositions(int rows, int cols, ArrowsCell[,] cells, System.Random rng)
        {
            var positions = new List<(int, int)>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (!cells[r, c].IsObstacle)
                        positions.Add((r, c));
            Shuffle(positions, rng);
            return positions;
        }

        private int MeasureChainDepth(ArrowsCell[,] cells, int rows, int cols)
        {
            var sim = new bool[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    sim[r, c] = cells[r, c] is { IsEmpty: false, IsObstacle: false };

            int totalRounds = 0;
            bool anyRemoved;
            do
            {
                anyRemoved = false;
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        if (!sim[r, c]) continue;
                        if (CanSlideInSim(sim, cells, rows, cols, r, c))
                        {
                            sim[r, c] = false;
                            anyRemoved = true;
                        }
                    }
                if (anyRemoved) totalRounds++;
            } while (anyRemoved);

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (sim[r, c]) return 0;

            return totalRounds;
        }

        private bool CanSlideInSim(bool[,] remaining, ArrowsCell[,] cells, int rows, int cols, int r, int c)
        {
            var dir = cells[r, c].Direction;
            int dr = 0, dc = 0;
            switch (dir)
            {
                case ArrowDirection.Up: dr = -1; break;
                case ArrowDirection.Down: dr = 1; break;
                case ArrowDirection.Left: dc = -1; break;
                case ArrowDirection.Right: dc = 1; break;
                default: return false;
            }

            int nr = r + dr, nc = c + dc;
            while (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
            {
                if (remaining[nr, nc] || cells[nr, nc].IsObstacle) return false;
                nr += dr;
                nc += dc;
            }
            return true;
        }

        private void Shuffle<T>(List<T> list, System.Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
}
