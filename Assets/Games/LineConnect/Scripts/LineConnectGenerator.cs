using System;
using System.Collections.Generic;

namespace Gamio.Games.LineConnect
{
    public class LineConnectGenerator
    {
        private readonly string seed;

        public LineConnectGenerator(string seedValue)
        {
            seed = seedValue;
        }

        private static readonly (float h, float s, float v)[] PaletteSpecs =
        {
            (0.00f, 0.85f, 0.95f),
            (0.55f, 0.80f, 0.85f),
            (0.12f, 0.80f, 0.95f),
            (0.72f, 0.70f, 0.85f),
            (0.30f, 0.75f, 0.90f),
            (0.88f, 0.80f, 0.90f),
            (0.18f, 0.70f, 0.95f),
            (0.62f, 0.65f, 0.85f),
            (0.45f, 0.70f, 0.90f),
            (0.95f, 0.75f, 0.85f),
            (0.07f, 0.85f, 0.90f),
            (0.50f, 0.65f, 0.85f),
            (0.25f, 0.80f, 0.95f),
            (0.80f, 0.75f, 0.90f),
            (0.38f, 0.70f, 0.85f),
            (0.68f, 0.80f, 0.90f),
        };

        public LineConnectPuzzle Generate(int gridSize)
        {
            var rng = new Random(seed.GetHashCode());
            int total = gridSize * gridSize;

            var path = GenerateWindingPath(gridSize, rng);

            int colorCount = Math.Max(3, total / 5);
            int minLen = 4;
            int baseLen = total / colorCount;

            var rawSegments = new List<List<(int, int)>>();
            int pos = 0;
            for (int id = 0; id < colorCount && pos < total; id++)
            {
                int remaining = total - pos;
                int len = Math.Min(rng.Next(minLen, Math.Max(minLen + 1, baseLen + 2)), remaining);
                if (remaining - len > 0 && remaining - len < minLen && rawSegments.Count > 0)
                {
                    foreach (var cell in path.GetRange(pos, remaining))
                        rawSegments[rawSegments.Count - 1].Add(cell);
                    pos = total;
                    break;
                }
                var seg = new List<(int, int)>();
                for (int i = 0; i < len; i++) seg.Add(path[pos + i]);
                rawSegments.Add(seg);
                pos += len;
            }

            if (pos < total && rawSegments.Count > 0)
            {
                foreach (var cell in path.GetRange(pos, total - pos))
                    rawSegments[rawSegments.Count - 1].Add(cell);
            }

            for (int i = 1; i < rawSegments.Count; i++)
            {
                int push = Math.Min(2, rawSegments[i].Count - 2);
                if (push > 0)
                {
                    var moved = rawSegments[i].GetRange(0, push);
                    rawSegments[i].RemoveRange(0, push);
                    rawSegments[i - 1].AddRange(moved);
                }
            }

            var segments = new List<List<(int, int)>>();
            for (int i = 0; i < rawSegments.Count; i++)
                if (rawSegments[i].Count >= 2) segments.Add(rawSegments[i]);

            var cells = new LineConnectCell[gridSize, gridSize];
            for (int r = 0; r < gridSize; r++)
                for (int c = 0; c < gridSize; c++)
                    cells[r, c] = new LineConnectCell { Row = r, Col = c, ColorId = -1, IsEndpoint = false };

            var palette = new UnityEngine.Color[segments.Count];
            for (int i = 0; i < segments.Count; i++)
            {
                var spec = PaletteSpecs[i % PaletteSpecs.Length];
                palette[i] = UnityEngine.Color.HSVToRGB(spec.h, spec.s, spec.v);
            }

            for (int id = 0; id < segments.Count; id++)
            {
                var seg = segments[id];
                for (int i = 0; i < seg.Count; i++)
                {
                    var (r, c) = seg[i];
                    cells[r, c] = new LineConnectCell { Row = r, Col = c, ColorId = id, IsEndpoint = i == 0 || i == seg.Count - 1 };
                }
            }

            return new LineConnectPuzzle(gridSize, cells, segments.Count, segments, palette);
        }

        private List<(int, int)> GenerateWindingPath(int gridSize, Random rng)
        {
            int N = gridSize;
            var path = new List<(int, int)>(N * N);

            for (int r = 0; r < N - 1; r += 2)
            {
                bool leftToRight = ((r / 2) % 2 == 0);

                if (leftToRight)
                {
                    path.Add((r, 0));
                    path.Add((r + 1, 0));
                    for (int c = 1; c < N; c++)
                    {
                        if (c % 2 == 1)
                        {
                            path.Add((r + 1, c));
                            path.Add((r, c));
                        }
                        else
                        {
                            path.Add((r, c));
                            path.Add((r + 1, c));
                        }
                    }
                }
                else
                {
                    path.Add((r, N - 1));
                    path.Add((r + 1, N - 1));
                    for (int c = N - 2; c >= 0; c--)
                    {
                        int steps = N - 2 - c;
                        if (steps % 2 == 0)
                        {
                            path.Add((r + 1, c));
                            path.Add((r, c));
                        }
                        else
                        {
                            path.Add((r, c));
                            path.Add((r + 1, c));
                        }
                    }
                }
            }

            if (N % 2 == 1)
            {
                int r = N - 1;
                bool lastBlockLeftToRight = (((N - 3) / 2) % 2 == 0);
                if (lastBlockLeftToRight)
                {
                    for (int c = N - 1; c >= 0; c--)
                        path.Add((r, c));
                }
                else
                {
                    for (int c = 0; c < N; c++)
                        path.Add((r, c));
                }
            }

            return path;
        }
    }
}
