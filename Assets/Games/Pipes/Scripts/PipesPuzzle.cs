using System;
using System.Collections.Generic;

namespace Gamio.Games.Pipes
{
    [Flags]
    public enum EdgeDirections
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3
    }

    public static class PipeExtensions
    {
        public static EdgeDirections GetOpenEdges(PipeType type, int rotation, bool isPort, int portDirection)
        {
            if (isPort)
                return portDirection switch
                {
                    0 => EdgeDirections.North,
                    1 => EdgeDirections.East,
                    2 => EdgeDirections.South,
                    3 => EdgeDirections.West,
                    _ => EdgeDirections.None
                };

            EdgeDirections baseEdges = type switch
            {
                PipeType.Straight => EdgeDirections.North | EdgeDirections.South,
                PipeType.Bend => EdgeDirections.North | EdgeDirections.East,
                PipeType.TJunction => EdgeDirections.North | EdgeDirections.East | EdgeDirections.West,
                PipeType.Cross => EdgeDirections.North | EdgeDirections.East | EdgeDirections.South | EdgeDirections.West,
                _ => EdgeDirections.None
            };

            int shift = rotation % 4;
            int flags = (int)baseEdges;
            int rotatedFlags = ((flags << shift) | (flags >> (4 - shift))) & 0xF;
            return (EdgeDirections)rotatedFlags;
        }

        public static EdgeDirections Opposite(this EdgeDirections edge)
        {
            return edge switch
            {
                EdgeDirections.North => EdgeDirections.South,
                EdgeDirections.South => EdgeDirections.North,
                EdgeDirections.East => EdgeDirections.West,
                EdgeDirections.West => EdgeDirections.East,
                _ => EdgeDirections.None
            };
        }
    }

    public class PipesPuzzle
    {
        public int GridSize { get; }
        public PipesCell[,] Cells { get; }

        private readonly int[,] playerRotations;
        private readonly int[,] targetRotations;
        private readonly Stack<(int r, int c, int prevRotation)> undoStack;

        public PipesPuzzle(int gridSize, PipesCell[,] cells, int[,] targetRotationsData, int[,] initialRotations)
        {
            GridSize = gridSize;
            Cells = cells;
            targetRotations = targetRotationsData;
            playerRotations = initialRotations;
            undoStack = new Stack<(int, int, int)>();
        }

        public int GetRotation(int r, int c) => playerRotations[r, c];
        public int GetTargetRotation(int r, int c) => targetRotations[r, c];

        public int CycleRotation(int r, int c)
        {
            var prev = playerRotations[r, c];
            int next = (prev + 1) % 4;
            playerRotations[r, c] = next;
            undoStack.Push((r, c, prev));
            return next;
        }

        public void SetRotation(int r, int c, int rotation)
        {
            var prev = playerRotations[r, c];
            playerRotations[r, c] = rotation;
            undoStack.Push((r, c, prev));
        }

        public bool Undo()
        {
            if (undoStack.Count == 0) return false;
            var (r, c, prev) = undoStack.Pop();
            playerRotations[r, c] = prev;
            return true;
        }

        public void Reset()
        {
            undoStack.Clear();
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    playerRotations[r, c] = targetRotations[r, c];
        }

        public void Scramble()
        {
            var rng = new Random();
            undoStack.Clear();
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    playerRotations[r, c] = Cells[r, c].IsFixed
                        ? targetRotations[r, c]
                        : rng.Next(4);
        }

        public bool IsSolved()
        {
            return CheckAllConnectionsValid();
        }

        public bool CheckAllConnectionsValid()
        {
            for (int r = 0; r < GridSize; r++)
            for (int c = 0; c < GridSize; c++)
            {
                var cell = Cells[r, c];
                int rot = playerRotations[r, c];
                var openEdges = PipeExtensions.GetOpenEdges(cell.Type, rot, cell.IsPort, cell.PortDirection);
                if (openEdges == EdgeDirections.None) continue;

                if (openEdges.HasFlag(EdgeDirections.North) && !HasMatchingNeighbor(r - 1, c, EdgeDirections.South))
                    return false;
                if (openEdges.HasFlag(EdgeDirections.South) && !HasMatchingNeighbor(r + 1, c, EdgeDirections.North))
                    return false;
                if (openEdges.HasFlag(EdgeDirections.East) && !HasMatchingNeighbor(r, c + 1, EdgeDirections.West))
                    return false;
                if (openEdges.HasFlag(EdgeDirections.West) && !HasMatchingNeighbor(r, c - 1, EdgeDirections.East))
                    return false;
            }
            return true;
        }

        private bool HasMatchingNeighbor(int nr, int nc, EdgeDirections requiredEdge)
        {
            if (nr < 0 || nr >= GridSize || nc < 0 || nc >= GridSize)
                return false;

            var nCell = Cells[nr, nc];
            int nRot = playerRotations[nr, nc];
            var nOpenEdges = PipeExtensions.GetOpenEdges(nCell.Type, nRot, nCell.IsPort, nCell.PortDirection);
            return nOpenEdges.HasFlag(requiredEdge);
        }

        public bool IsCellConnectedCorrectly(int r, int c)
        {
            var cell = Cells[r, c];
            int rot = playerRotations[r, c];
            var openEdges = PipeExtensions.GetOpenEdges(cell.Type, rot, cell.IsPort, cell.PortDirection);
            if (openEdges == EdgeDirections.None) return true;

            if (openEdges.HasFlag(EdgeDirections.North) && !HasMatchingNeighbor(r - 1, c, EdgeDirections.South))
                return false;
            if (openEdges.HasFlag(EdgeDirections.South) && !HasMatchingNeighbor(r + 1, c, EdgeDirections.North))
                return false;
            if (openEdges.HasFlag(EdgeDirections.East) && !HasMatchingNeighbor(r, c + 1, EdgeDirections.West))
                return false;
            if (openEdges.HasFlag(EdgeDirections.West) && !HasMatchingNeighbor(r, c - 1, EdgeDirections.East))
                return false;
            return true;
        }

        public bool IsRotationCorrect(int r, int c)
        {
            return playerRotations[r, c] == targetRotations[r, c];
        }

        public static readonly int[] DR = { -1, 0, 1, 0 };
        public static readonly int[] DC = { 0, 1, 0, -1 };
    }
}
