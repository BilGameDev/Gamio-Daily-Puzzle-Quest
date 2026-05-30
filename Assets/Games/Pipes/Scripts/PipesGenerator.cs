using System;
using System.Collections.Generic;

namespace Gamio.Games.Pipes
{
    public class PipesGenerator
    {
        private readonly string seed;
        private Random rng;

        public PipesGenerator(string seedValue)
        {
            seed = seedValue;
        }

        public PipesPuzzle Generate(int gridSize)
        {
            rng = new Random(seed.GetHashCode());
            var adj = new List<int>[gridSize, gridSize];

            for (int r = 0; r < gridSize; r++)
                for (int c = 0; c < gridSize; c++)
                    adj[r, c] = new List<int>();

            BuildSpanningTree(adj, gridSize);
            FixInteriorLeaves(adj, gridSize);

            var cells = new PipesCell[gridSize, gridSize];
            var targetRotations = new int[gridSize, gridSize];
            var initialRotations = new int[gridSize, gridSize];

            for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
            {
                var directions = new List<int>(adj[r, c]);
                bool isPort = directions.Count == 1 && OnBorder(r, c, gridSize);
                int portDir = isPort ? directions[0] : 0;

                var type = isPort ? PipeType.Bend : DetermineType(directions);
                int targetRot = isPort ? 0 : FindRotation(type, directions);

                cells[r, c] = new PipesCell
                {
                    Row = r,
                    Col = c,
                    Type = type,
                    IsFixed = isPort,
                    IsPort = isPort,
                    PortDirection = portDir
                };

                targetRotations[r, c] = isPort ? portDir : targetRot;
                initialRotations[r, c] = isPort ? portDir : rng.Next(4);
            }

            return new PipesPuzzle(gridSize, cells, targetRotations, initialRotations);
        }

        private void BuildSpanningTree(List<int>[,] adj, int size)
        {
            var visited = new bool[size, size];
            var stack = new Stack<(int, int)>();
            visited[0, 0] = true;
            stack.Push((0, 0));

            while (stack.Count > 0)
            {
                var (r, c) = stack.Peek();
                var neighbors = GetShuffledUnvisited(r, c, size, visited);

                if (neighbors.Count > 0)
                {
                    var (nr, nc) = neighbors[0];
                    int dir = Direction(r, c, nr, nc);
                    visited[nr, nc] = true;
                    adj[r, c].Add(dir);
                    adj[nr, nc].Add((dir + 2) % 4);
                    stack.Push((nr, nc));
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        private void FixInteriorLeaves(List<int>[,] adj, int size)
        {
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (adj[r, c].Count == 1 && !OnBorder(r, c, size))
                {
                    var candidates = new List<int>();
                    for (int d = 0; d < 4; d++)
                    {
                        int nr = r + PipesPuzzle.DR[d];
                        int nc = c + PipesPuzzle.DC[d];
                        if (nr < 0 || nr >= size || nc < 0 || nc >= size) continue;
                        if (adj[r, c].Contains(d)) continue;
                        if (adj[nr, nc].Count >= 4) continue;
                        candidates.Add(d);
                    }

                    if (candidates.Count > 0)
                    {
                        int dir = candidates[rng.Next(candidates.Count)];
                        int nr = r + PipesPuzzle.DR[dir];
                        int nc = c + PipesPuzzle.DC[dir];
                        adj[r, c].Add(dir);
                        adj[nr, nc].Add((dir + 2) % 4);
                    }
                }
            }
        }

        private static int Direction(int r1, int c1, int r2, int c2)
        {
            if (r2 == r1 - 1) return 0;
            if (c2 == c1 + 1) return 1;
            if (r2 == r1 + 1) return 2;
            return 3;
        }

        private static bool OnBorder(int r, int c, int size)
        {
            return r == 0 || r == size - 1 || c == 0 || c == size - 1;
        }

        private static PipeType DetermineType(List<int> dirs)
        {
            return dirs.Count switch
            {
                2 => AreOpposite(dirs[0], dirs[1]) ? PipeType.Straight : PipeType.Bend,
                3 => PipeType.TJunction,
                4 => PipeType.Cross,
                _ => PipeType.Empty
            };
        }

        private static bool AreOpposite(int a, int b) => (a + 2) % 4 == b;

        private static int FindRotation(PipeType type, List<int> directions)
        {
            if (type == PipeType.Empty) return 0;
            int targetMask = 0;
            foreach (var d in directions)
                targetMask |= (1 << d);
            for (int rot = 0; rot < 4; rot++)
            {
                var edges = PipeExtensions.GetOpenEdges(type, rot, false, 0);
                int edgeMask = 0;
                if (edges.HasFlag(EdgeDirections.North)) edgeMask |= 1;
                if (edges.HasFlag(EdgeDirections.East)) edgeMask |= 2;
                if (edges.HasFlag(EdgeDirections.South)) edgeMask |= 4;
                if (edges.HasFlag(EdgeDirections.West)) edgeMask |= 8;
                if (edgeMask == targetMask)
                    return rot;
            }
            return 0;
        }

        private List<(int, int)> GetShuffledUnvisited(int r, int c, int size, bool[,] visited)
        {
            var result = new List<(int, int)>();
            for (int i = 0; i < 4; i++)
            {
                int nr = r + PipesPuzzle.DR[i], nc = c + PipesPuzzle.DC[i];
                if (nr >= 0 && nr < size && nc >= 0 && nc < size && !visited[nr, nc])
                    result.Add((nr, nc));
            }
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = result[i];
                result[i] = result[j];
                result[j] = tmp;
            }
            return result;
        }
    }
}
