using System.Collections.Generic;

namespace Gamio.Games.LineConnect
{
    public class LineConnectPuzzle
    {
        public int GridSize { get; }
        public LineConnectCell[,] Cells { get; }
        public int ColorCount { get; }
        public IReadOnlyList<List<(int r, int c)>> SolutionPaths { get; }
        public UnityEngine.Color[] ColorPalette { get; }

        private readonly int[,] playerPathId;
        private readonly Stack<(int r, int c, int prevId)> undoStack;

        public LineConnectPuzzle(int gridSize, LineConnectCell[,] cells, int colorCount, List<List<(int, int)>> solutionPaths, UnityEngine.Color[] colorPalette)
        {
            GridSize = gridSize;
            Cells = cells;
            ColorCount = colorCount;
            SolutionPaths = solutionPaths;
            ColorPalette = colorPalette;
            playerPathId = new int[gridSize, gridSize];
            undoStack = new Stack<(int, int, int)>();
            for (int r = 0; r < gridSize; r++)
                for (int c = 0; c < gridSize; c++)
                    playerPathId[r, c] = -1;
        }

        public int GetPathId(int r, int c) => playerPathId[r, c];
        public bool IsCellFree(int r, int c) => playerPathId[r, c] < 0;

        public void AssignCell(int r, int c, int colorId)
        {
            int prev = playerPathId[r, c];
            playerPathId[r, c] = colorId;
            undoStack.Push((r, c, prev));
        }

        public bool Undo()
        {
            if (undoStack.Count == 0) return false;
            var (r, c, prev) = undoStack.Pop();
            playerPathId[r, c] = prev;
            return true;
        }

        public void RemoveColor(int colorId)
        {
            var temp = new Stack<(int r, int c, int prev)>();
            while (undoStack.Count > 0)
            {
                var item = undoStack.Pop();
                if (playerPathId[item.r, item.c] == colorId)
                    playerPathId[item.r, item.c] = -1;
                else
                    temp.Push(item);
            }
            while (temp.Count > 0)
                undoStack.Push(temp.Pop());
        }

        public List<(int, int)> GetPathCells(int colorId)
        {
            var cells = new List<(int, int)>();
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    if (playerPathId[r, c] == colorId)
                        cells.Add((r, c));
            return cells;
        }

        public void Reset()
        {
            undoStack.Clear();
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    playerPathId[r, c] = -1;
        }

        public bool IsSolved()
        {
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    if (playerPathId[r, c] < 0) return false;
            return true;
        }
    }
}
