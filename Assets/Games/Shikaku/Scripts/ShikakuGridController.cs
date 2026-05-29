using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamio.Games.Shikaku
{
    public class ShikakuGridController
    {
        private readonly ShikakuPuzzle puzzle;
        private int dragStartRow = -1;
        private int dragStartCol = -1;
        private int dragEndRow;
        private int dragEndCol;
        private bool isDragging;
        private bool solved;

        public ShikakuPuzzle Puzzle => puzzle;
        public bool IsSolved => solved;
        public bool IsDragging => isDragging;
        public int DragStartRow => dragStartRow;
        public int DragStartCol => dragStartCol;
        public int DragEndRow => dragEndRow;
        public int DragEndCol => dragEndCol;

        public event Action OnSolved;
        public event Action OnRectPlaced;

        public ShikakuGridController(ShikakuPuzzle pz)
        {
            puzzle = pz;
        }

        public void StartDrag(int row, int col)
        {
            if (solved) return;
            dragStartRow = row;
            dragStartCol = col;
            dragEndRow = row;
            dragEndCol = col;
            isDragging = true;
        }

        public void UpdateDrag(int row, int col)
        {
            if (!isDragging) return;
            dragEndRow = Math.Clamp(row, 0, puzzle.Rows - 1);
            dragEndCol = Math.Clamp(col, 0, puzzle.Cols - 1);
        }

        public void RemoveOverlappingDuringDrag(List<int> removedIndices)
        {
            if (!isDragging || dragStartRow < 0) return;
            var minR = Math.Min(dragStartRow, dragEndRow);
            var maxR = Math.Max(dragStartRow, dragEndRow);
            var minC = Math.Min(dragStartCol, dragEndCol);
            var maxC = Math.Max(dragStartCol, dragEndCol);
            var rect = new ShikakuRect { Row = minR, Col = minC, Height = maxR - minR + 1, Width = maxC - minC + 1 };
            puzzle.RemoveRectsOverlapping(rect, removedIndices);
        }

        public bool EndDrag(Color color)
        {
            if (!isDragging) return false;
            isDragging = false;

            var minR = Math.Min(dragStartRow, dragEndRow);
            var maxR = Math.Max(dragStartRow, dragEndRow);
            var minC = Math.Min(dragStartCol, dragEndCol);
            var maxC = Math.Max(dragStartCol, dragEndCol);

            var rect = new ShikakuRect
            {
                Row = minR,
                Col = minC,
                Height = maxR - minR + 1,
                Width = maxC - minC + 1
            };

            var placed = puzzle.TryAddRect(rect, color);

            OnRectPlaced?.Invoke();

            if (placed && !solved && puzzle.IsGridFullyCovered())
                Check();

            dragStartRow = -1;
            dragStartCol = -1;

            return placed;
        }

        public void CancelDrag()
        {
            isDragging = false;
            dragStartRow = -1;
            dragStartCol = -1;
        }

        public bool CanPlaceAt(int row, int col, int height, int width)
        {
            if (row + height > puzzle.Rows || col + width > puzzle.Cols)
                return false;

            var testRect = new ShikakuRect { Row = row, Col = col, Height = height, Width = width };

            foreach (var existing in puzzle.PlayerRects)
            {
                if (existing.Overlaps(testRect))
                    return false;
            }

            var hasNumber = false;
            for (var r = row; r < row + height; r++)
            {
                for (var c = col; c < col + width; c++)
                {
                    if (puzzle.Cells[r, c].Number.HasValue)
                    {
                        if (hasNumber) return false;
                        if (puzzle.Cells[r, c].Number.Value != height * width) return false;
                        hasNumber = true;
                    }
                }
            }

            return hasNumber;
        }

        public int[] GetPreviewRange()
        {
            if (!isDragging || dragStartRow < 0) return null;
            return new int[]
            {
                Math.Min(dragStartRow, dragEndRow),
                Math.Min(dragStartCol, dragEndCol),
                Math.Max(dragStartRow, dragEndRow),
                Math.Max(dragStartCol, dragEndCol)
            };
        }

        public bool IsCellInPreview(int row, int col)
        {
            if (!isDragging || dragStartRow < 0) return false;
            var minR = Math.Min(dragStartRow, dragEndRow);
            var maxR = Math.Max(dragStartRow, dragEndRow);
            var minC = Math.Min(dragStartCol, dragEndCol);
            var maxC = Math.Max(dragStartCol, dragEndCol);
            return row >= minR && row <= maxR && col >= minC && col <= maxC;
        }

        public bool IsCellSolved(int row, int col)
        {
            foreach (var rect in puzzle.PlayerRects)
            {
                if (rect.ContainsCell(row, col))
                    return true;
            }
            return false;
        }

        public void Undo()
        {
            if (solved) return;
            puzzle.RemoveLastRect();
        }

        public void Check()
        {
            if (solved) return;
            if (puzzle.IsSolved())
            {
                solved = true;
                OnSolved?.Invoke();
            }
        }

        public void ResetPuzzle()
        {
            while (puzzle.PlayerRects.Count > 0)
                puzzle.RemoveLastRect();
            solved = false;
        }

        public void Dispose()
        {
            OnSolved = null;
            OnRectPlaced = null;
        }
    }
}
