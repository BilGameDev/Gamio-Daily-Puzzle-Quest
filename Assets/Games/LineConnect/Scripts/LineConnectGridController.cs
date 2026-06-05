using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamio.Games.LineConnect
{
    public class LineConnectGridController
    {
        private readonly LineConnectPuzzle puzzle;
        private bool solved;
        private int activeColorId;
        private List<(int r, int c)> activePath;
        private bool isDragging;

        public LineConnectPuzzle Puzzle => puzzle;
        public bool IsSolved => solved;
        public bool IsDragging => isDragging;
        public int ActiveColorId => activeColorId;
        public IReadOnlyList<(int r, int c)> ActivePath => activePath;

        public event Action OnSolved;
        public event Action OnVisualsChanged;
        public event Action<int, List<(int r, int c)>> OnPathConnected;

        public LineConnectGridController(LineConnectPuzzle puzzleData)
        {
            puzzle = puzzleData;
            activePath = new List<(int, int)>();
        }

        public void StartDrag(int row, int col)
        {
            if (solved) return;
            var cell = puzzle.Cells[row, col];
            if (!cell.IsEndpoint) return;

            isDragging = true;
            activeColorId = cell.ColorId;
            puzzle.RemoveColor(activeColorId);
            activePath.Clear();
            activePath.Add((row, col));
            OnVisualsChanged?.Invoke();
        }

        public void UpdateDrag(int row, int col)
        {
            if (!isDragging || solved) return;
            var targetCell = puzzle.Cells[row, col];
            if (targetCell.IsEndpoint && targetCell.ColorId != activeColorId) return;

            var last = activePath[activePath.Count - 1];
            if (row == last.r && col == last.c) return;

            int dr = row - last.r;
            int dc = col - last.c;
            if ((Math.Abs(dr) == 1 && dc == 0) || (dr == 0 && Math.Abs(dc) == 1))
            {
                if (activePath.Count >= 2 && row == activePath[activePath.Count - 2].r && col == activePath[activePath.Count - 2].c)
                {
                    activePath.RemoveAt(activePath.Count - 1);
                }
                else
                {
                    int existingId = puzzle.GetPathId(row, col);
                    if (existingId >= 0 && existingId != activeColorId)
                        puzzle.RemoveColor(existingId);
                    activePath.Add((row, col));
                }

                if (targetCell.IsEndpoint && targetCell.ColorId == activeColorId && activePath.Count >= 2)
                {
                    var first = activePath[0];
                    if (first.r != row || first.c != col)
                    {
                        var connectedPath = new List<(int r, int c)>(activePath);
                        foreach (var (pr, pc) in connectedPath)
                            puzzle.AssignCell(pr, pc, activeColorId);
                        activePath.Clear();
                        isDragging = false;
                        OnVisualsChanged?.Invoke();
                        OnPathConnected?.Invoke(activeColorId, connectedPath);
                        bool isFull = puzzle.IsSolved();
                        if (isFull)
                        {
                            solved = true;
                            OnSolved?.Invoke();
                        }
                        return;
                    }
                }

                OnVisualsChanged?.Invoke();
            }
        }

        public void EndDrag()
        {
            if (!isDragging) return;
            isDragging = false;

            if (activePath.Count >= 2)
            {
                var last = activePath[activePath.Count - 1];
                var lastCell = puzzle.Cells[last.r, last.c];

                if (lastCell.IsEndpoint && lastCell.ColorId == activeColorId)
                {
                    foreach (var (r, c) in activePath)
                        puzzle.AssignCell(r, c, activeColorId);
                }
            }

            activePath.Clear();
            OnVisualsChanged?.Invoke();
            bool isFull = puzzle.IsSolved();
            if (isFull)
            {
                solved = true;
                OnSolved?.Invoke();
            }
        }

        public void ResetPuzzle()
        {
            puzzle.Reset();
            solved = false;
            isDragging = false;
            activePath.Clear();
            OnVisualsChanged?.Invoke();
        }

        public void Dispose()
        {
            OnSolved = null;
            OnVisualsChanged = null;
            OnPathConnected = null;
        }
    }
}
