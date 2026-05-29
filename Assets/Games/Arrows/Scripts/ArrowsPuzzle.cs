using System.Collections.Generic;

namespace Gamio.Games.Arrows
{
    public class ArrowsPuzzle
    {
        public int Rows { get; }
        public int Cols { get; }
        public ArrowsCell[,] Cells { get; }

        private readonly bool[,] tiles;
        private readonly Stack<(int row, int col)> undoStack;

        public ArrowsPuzzle(int rows, int cols, ArrowsCell[,] cells)
        {
            Rows = rows;
            Cols = cols;
            Cells = cells;
            tiles = new bool[rows, cols];
            undoStack = new Stack<(int, int)>();

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    tiles[r, c] = cells[r, c] is { IsEmpty: false, IsObstacle: false };
        }

        public bool HasTile(int r, int c)
        {
            return r >= 0 && r < Rows && c >= 0 && c < Cols && tiles[r, c];
        }

        public bool IsObstacle(int r, int c)
        {
            return r >= 0 && r < Rows && c >= 0 && c < Cols && Cells[r, c].IsObstacle;
        }

        private bool IsBlocked(int r, int c)
        {
            return tiles[r, c] || Cells[r, c].IsObstacle;
        }

        public bool CanSlide(int r, int c)
        {
            return FindBlocker(r, c) == null;
        }

        public (int r, int c)? FindBlocker(int r, int c)
        {
            if (!HasTile(r, c)) return null;

            var dir = Cells[r, c].Direction;
            int dr = 0, dc = 0;
            switch (dir)
            {
                case ArrowDirection.Up: dr = -1; break;
                case ArrowDirection.Down: dr = 1; break;
                case ArrowDirection.Left: dc = -1; break;
                case ArrowDirection.Right: dc = 1; break;
                default: return null;
            }

            int nr = r + dr, nc = c + dc;
            while (nr >= 0 && nr < Rows && nc >= 0 && nc < Cols)
            {
                if (IsBlocked(nr, nc)) return (nr, nc);
                nr += dr;
                nc += dc;
            }
            return null;
        }

        public int SlideDistance(int r, int c)
        {
            var dir = Cells[r, c].Direction;
            int dr = 0, dc = 0;
            switch (dir)
            {
                case ArrowDirection.Up: dr = -1; break;
                case ArrowDirection.Down: dr = 1; break;
                case ArrowDirection.Left: dc = -1; break;
                case ArrowDirection.Right: dc = 1; break;
                default: return 0;
            }

            int nr = r + dr, nc = c + dc;
            int steps = 1;
            while (nr >= 0 && nr < Rows && nc >= 0 && nc < Cols)
            {
                if (IsBlocked(nr, nc)) break;
                steps++;
                nr += dr;
                nc += dc;
            }
            return steps;
        }

        public void RemoveTile(int r, int c)
        {
            tiles[r, c] = false;
            undoStack.Push((r, c));
        }

        public bool Undo(out int row, out int col)
        {
            if (undoStack.Count == 0)
            {
                row = col = -1;
                return false;
            }
            var (r, c) = undoStack.Pop();
            tiles[r, c] = true;
            row = r;
            col = c;
            return true;
        }

        public bool IsSolved()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (tiles[r, c]) return false;
            return true;
        }

        public void Reset()
        {
            undoStack.Clear();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    tiles[r, c] = Cells[r, c] is { IsEmpty: false, IsObstacle: false };
        }

        public List<(int r, int c)> GetActiveTiles()
        {
            var active = new List<(int, int)>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (tiles[r, c])
                        active.Add((r, c));
            return active;
        }
    }
}
