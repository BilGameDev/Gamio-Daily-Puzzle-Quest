using System.Collections.Generic;
using UnityEngine;

namespace Gamio.Games.Shikaku
{
    public class ShikakuPuzzle
    {
        public int Rows => Cells.GetLength(0);
        public int Cols => Cells.GetLength(1);
        public ShikakuCell[,] Cells { get; }
        public IReadOnlyList<ShikakuRect> SolutionRects { get; }

        private readonly List<ShikakuRect> playerRects;

        public ShikakuPuzzle(ShikakuCell[,] cells, IReadOnlyList<ShikakuRect> solution)
        {
            Cells = cells;
            SolutionRects = solution;
            playerRects = new List<ShikakuRect>();
        }

        public IReadOnlyList<ShikakuRect> PlayerRects => playerRects;

        public bool TryAddRect(ShikakuRect rect, Color color)
        {
            if (rect.Row < 0 || rect.Col < 0 || rect.Bottom >= Rows || rect.Right >= Cols)
                return false;

            if (rect.Height <= 0 || rect.Width <= 0)
                return false;

            for (int i = playerRects.Count - 1; i >= 0; i--)
            {
                if (playerRects[i].Overlaps(rect))
                    playerRects.RemoveAt(i);
            }

            rect.Id = playerRects.Count;
            rect.Color = color;
            playerRects.Add(rect);
            return true;
        }

        public void RemoveRectsOverlapping(ShikakuRect rect, List<int> removedIndices)
        {
            removedIndices.Clear();
            for (int i = playerRects.Count - 1; i >= 0; i--)
            {
                if (playerRects[i].Overlaps(rect))
                    removedIndices.Add(i);
            }
            foreach (var idx in removedIndices)
                playerRects.RemoveAt(idx);
        }

        public void RemoveLastRect()
        {
            if (playerRects.Count > 0)
                playerRects.RemoveAt(playerRects.Count - 1);
        }

        public bool IsSolved()
        {
            if (playerRects.Count != SolutionRects.Count)
                return false;

            foreach (var pr in playerRects)
            {
                bool match = false;
                foreach (var sr in SolutionRects)
                {
                    if (pr.Row == sr.Row && pr.Col == sr.Col &&
                        pr.Height == sr.Height && pr.Width == sr.Width)
                    {
                        match = true;
                        break;
                    }
                }
                if (!match) return false;
            }
            return true;
        }

        public bool IsGridFullyCovered()
        {
            var covered = new bool[Rows, Cols];
            foreach (var rect in playerRects)
            {
                for (int r = rect.Row; r <= rect.Bottom; r++)
                for (int c = rect.Col; c <= rect.Right; c++)
                    covered[r, c] = true;
            }
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (!covered[r, c]) return false;
            return true;
        }

        public bool IsCellPlayable(int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Cols)
                return false;
            return Cells[row, col].IsPlayable;
        }
    }
}
