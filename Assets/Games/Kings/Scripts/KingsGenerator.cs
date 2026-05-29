using System;
using System.Collections.Generic;

namespace Gamio.Games.Kings
{
    public class KingsGenerator
    {
        private readonly int seed;
        private Random rng;

        private static readonly int[] GDR = { -1, 0, 1, 0 };
        private static readonly int[] GDC = { 0, 1, 0, -1 };

        public KingsGenerator(int inputSeed)
        {
            seed = inputSeed;
        }

        public KingsPuzzle Generate(int gridSize)
        {
            rng = new Random(seed);
            int regionCount = gridSize;
            int[,] regionIds = GenerateRegions(gridSize, regionCount);

            var cells = new KingsCell[gridSize, gridSize];
            for (int r = 0; r < gridSize; r++)
                for (int c = 0; c < gridSize; c++)
                    cells[r, c] = new KingsCell { Row = r, Col = c, SectionIndex = regionIds[r, c] };

            var solution = SolveKings(regionIds, gridSize);
            var solutionGrid = new bool[gridSize, gridSize];
            for (int i = 0; i < gridSize; i++)
            {
                var (sr, sc) = solution[i];
                solutionGrid[sr, sc] = true;
            }

            return new KingsPuzzle(gridSize, cells, regionCount, solutionGrid);
        }

        private int[,] GenerateRegions(int size, int regionCount)
        {
            var assigned = new int[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    assigned[r, c] = -1;

            // Place seeds: one per row, shuffled columns for spread
            var cols = new List<int>();
            for (int i = 0; i < size; i++) cols.Add(i);
            for (int i = cols.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = cols[i]; cols[i] = cols[j]; cols[j] = tmp;
            }
            for (int i = 0; i < regionCount; i++)
                assigned[i, cols[i]] = i;

            // Per-region frontier queues for BFS growth
            var frontiers = new List<(int, int)>[regionCount];
            var regionSizes = new int[regionCount];
            for (int i = 0; i < regionCount; i++)
            {
                regionSizes[i] = 1;
                frontiers[i] = new List<(int, int)>();
            }

            // Initialize frontiers with cardinal neighbors of seeds
            for (int i = 0; i < regionCount; i++)
            {
                int sr = -1, sc = -1;
                for (int r = 0; r < size && sr < 0; r++)
                    for (int c = 0; c < size && sr < 0; c++)
                        if (assigned[r, c] == i) { sr = r; sc = c; }

                AddCardinalNeighbors(frontiers[i], assigned, sr, sc, size);
            }

            int totalAssigned = regionCount;
            while (totalAssigned < size * size)
            {
                // Pick the smallest region that still has frontier cells
                int bestRegion = -1;
                for (int i = 0; i < regionCount; i++)
                {
                    if (frontiers[i].Count == 0) continue;
                    if (bestRegion < 0 || regionSizes[i] < regionSizes[bestRegion])
                        bestRegion = i;
                }

                if (bestRegion < 0) break;

                var frontier = frontiers[bestRegion];
                int idx = rng.Next(frontier.Count);
                var (fr, fc) = frontier[idx];
                frontier.RemoveAt(idx);

                if (assigned[fr, fc] != -1) continue;

                assigned[fr, fc] = bestRegion;
                regionSizes[bestRegion]++;
                totalAssigned++;

                AddCardinalNeighbors(frontier, assigned, fr, fc, size);
            }

            return assigned;
        }

        private void AddCardinalNeighbors(List<(int, int)> frontier, int[,] assigned, int r, int c, int size)
        {
            for (int i = 0; i < 4; i++)
            {
                int nr = r + GDR[i], nc = c + GDC[i];
                if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                if (assigned[nr, nc] != -1) continue;
                if (!frontier.Contains((nr, nc)))
                    frontier.Add((nr, nc));
            }
        }

        private (int, int)[] SolveKings(int[,] regionIds, int size)
        {
            var result = new (int, int)[size];
            var usedCol = new bool[size];
            var usedRegion = new bool[size];

            if (Backtrack(0, result, usedCol, usedRegion, regionIds, size))
                return result;

            throw new Exception("Kings: no valid solution found for generated regions");
        }

        private bool Backtrack(int row, (int, int)[] result, bool[] usedCol, bool[] usedRegion,
            int[,] regionIds, int size)
        {
            if (row == size) return true;

            for (int col = 0; col < size; col++)
            {
                if (usedCol[col]) continue;
                int region = regionIds[row, col];
                if (usedRegion[region]) continue;

                bool adjacent = false;
                for (int r = 0; r < row; r++)
                {
                    if (Math.Abs(result[r].Item1 - row) <= 1 && Math.Abs(result[r].Item2 - col) <= 1)
                    {
                        adjacent = true;
                        break;
                    }
                }
                if (adjacent) continue;

                result[row] = (row, col);
                usedCol[col] = true;
                usedRegion[region] = true;

                if (Backtrack(row + 1, result, usedCol, usedRegion, regionIds, size))
                    return true;

                usedCol[col] = false;
                usedRegion[region] = false;
            }

            return false;
        }
    }
}