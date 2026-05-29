using System.Collections.Generic;

namespace Gamio.Games.Hitori
{
    public class HitoriPuzzle
    {
        public int GridSize { get; }
        public HitoriCell[,] Cells { get; }

        private readonly HitoriCellState[,] playerState;
        private readonly Stack<(int row, int col, HitoriCellState prevState)> undoStack;

        public HitoriPuzzle(int gridSize, HitoriCell[,] cells)
        {
            GridSize = gridSize;
            Cells = cells;
            playerState = new HitoriCellState[gridSize, gridSize];
            undoStack = new Stack<(int, int, HitoriCellState)>();
        }

        public HitoriCellState GetState(int r, int c)
        {
            return playerState[r, c];
        }

        public HitoriCellState CycleState(int r, int c)
        {
            var prev = playerState[r, c];
            var next = prev switch
            {
                HitoriCellState.None => HitoriCellState.Black,
                HitoriCellState.Black => HitoriCellState.White,
                HitoriCellState.White => HitoriCellState.None,
                _ => HitoriCellState.None
            };
            playerState[r, c] = next;
            undoStack.Push((r, c, prev));
            return next;
        }

        public bool Undo()
        {
            if (undoStack.Count == 0) return false;
            var (r, c, prev) = undoStack.Pop();
            playerState[r, c] = prev;
            return true;
        }

        public void Reset()
        {
            undoStack.Clear();
            for (int r = 0; r < GridSize; r++)
            for (int c = 0; c < GridSize; c++)
                playerState[r, c] = HitoriCellState.None;
        }

        public bool IsSolved()
        {
            if (!CheckNoAdjacentBlacks()) return false;
            if (!CheckWhiteCellConnectivity()) return false;
            if (!CheckRowColumnUniqueness()) return false;
            return true;
        }

        public HashSet<(int, int)> GetViolations()
        {
            var violations = new HashSet<(int, int)>();

            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (playerState[r, c] != HitoriCellState.Black) continue;
                    if (r < GridSize - 1 && playerState[r + 1, c] == HitoriCellState.Black)
                    {
                        violations.Add((r, c));
                        violations.Add((r + 1, c));
                    }
                    if (c < GridSize - 1 && playerState[r, c + 1] == HitoriCellState.Black)
                    {
                        violations.Add((r, c));
                        violations.Add((r, c + 1));
                    }
                }
            }

            for (int r = 0; r < GridSize; r++)
            {
                for (int c1 = 0; c1 < GridSize; c1++)
                {
                    if (playerState[r, c1] != HitoriCellState.White) continue;
                    for (int c2 = c1 + 1; c2 < GridSize; c2++)
                    {
                        if (playerState[r, c2] != HitoriCellState.White) continue;
                        if (Cells[r, c1].Number == Cells[r, c2].Number)
                        {
                            violations.Add((r, c1));
                            violations.Add((r, c2));
                        }
                    }
                }
            }

            for (int c = 0; c < GridSize; c++)
            {
                for (int r1 = 0; r1 < GridSize; r1++)
                {
                    if (playerState[r1, c] != HitoriCellState.White) continue;
                    for (int r2 = r1 + 1; r2 < GridSize; r2++)
                    {
                        if (playerState[r2, c] != HitoriCellState.White) continue;
                        if (Cells[r1, c].Number == Cells[r2, c].Number)
                        {
                            violations.Add((r1, c));
                            violations.Add((r2, c));
                        }
                    }
                }
            }

            return violations;
        }

        private bool CheckNoAdjacentBlacks()
        {
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (playerState[r, c] != HitoriCellState.Black) continue;
                    if (r > 0 && playerState[r - 1, c] == HitoriCellState.Black) return false;
                    if (r < GridSize - 1 && playerState[r + 1, c] == HitoriCellState.Black) return false;
                    if (c > 0 && playerState[r, c - 1] == HitoriCellState.Black) return false;
                    if (c < GridSize - 1 && playerState[r, c + 1] == HitoriCellState.Black) return false;
                }
            }
            return true;
        }

        private bool CheckWhiteCellConnectivity()
        {
            int startR = -1, startC = -1;
            int whiteCount = 0;

            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (playerState[r, c] == HitoriCellState.White)
                    {
                        whiteCount++;
                        if (startR == -1) { startR = r; startC = c; }
                    }
                }
            }

            if (whiteCount <= 1) return true;

            var visited = new bool[GridSize, GridSize];
            var queue = new Queue<(int, int)>();
            queue.Enqueue((startR, startC));
            visited[startR, startC] = true;
            int visitedCount = 1;

            while (queue.Count > 0)
            {
                var (r, c) = queue.Dequeue();
                foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    int nr = r + dr, nc = c + dc;
                    if (nr >= 0 && nr < GridSize && nc >= 0 && nc < GridSize && !visited[nr, nc] && playerState[nr, nc] == HitoriCellState.White)
                    {
                        visited[nr, nc] = true;
                        visitedCount++;
                        queue.Enqueue((nr, nc));
                    }
                }
            }

            return visitedCount == whiteCount;
        }

        private bool CheckRowColumnUniqueness()
        {
            for (int r = 0; r < GridSize; r++)
            {
                var seen = new HashSet<int>();
                for (int c = 0; c < GridSize; c++)
                {
                    if (playerState[r, c] != HitoriCellState.White) continue;
                    if (!seen.Add(Cells[r, c].Number)) return false;
                }
            }

            for (int c = 0; c < GridSize; c++)
            {
                var seen = new HashSet<int>();
                for (int r = 0; r < GridSize; r++)
                {
                    if (playerState[r, c] != HitoriCellState.White) continue;
                    if (!seen.Add(Cells[r, c].Number)) return false;
                }
            }

            return true;
        }
    }
}
