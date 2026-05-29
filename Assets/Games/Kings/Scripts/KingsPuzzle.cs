using System;
using System.Collections.Generic;

namespace Gamio.Games.Kings
{
    public class KingsPuzzle
    {
        public int GridSize { get; }
        public KingsCell[,] Cells { get; }
        public int RegionCount { get; }

        private readonly KingsCellState[,] playerState;
        private readonly Stack<UndoAction> undoStack;
        private readonly int[] crownsPerRow;
        private readonly int[] crownsPerCol;
        private readonly int[] crownsPerRegion;
        private readonly int[,] nullClaimCount;
        private readonly bool[,] solution;
        private int totalCrowns;

        public struct CellChange
        {
            public int Row; public int Col; public KingsCellState Prev;
        }

        public struct UndoAction
        {
            public CellChange[] Changes;
        }

        public static readonly int[] DR = { -1, -1, -1, 0, 0, 1, 1, 1 };
        public static readonly int[] DC = { -1, 0, 1, -1, 1, -1, 0, 1 };

        public KingsPuzzle(int gridSize, KingsCell[,] cells, int regionCount, bool[,] solutionData)
        {
            GridSize = gridSize;
            Cells = cells;
            RegionCount = regionCount;
            solution = solutionData;
            playerState = new KingsCellState[gridSize, gridSize];
            undoStack = new Stack<UndoAction>();
            crownsPerRow = new int[gridSize];
            crownsPerCol = new int[gridSize];
            crownsPerRegion = new int[regionCount];
            nullClaimCount = new int[gridSize, gridSize];
        }

        public KingsCellState GetState(int r, int c) => playerState[r, c];
        public int CrownsPerRow(int r) => crownsPerRow[r];
        public int CrownsPerCol(int c) => crownsPerCol[c];
        public int CrownsPerRegion(int ri) => crownsPerRegion[ri];
        public int TotalCrowns => totalCrowns;
        public bool IsNullClaimed(int r, int c) => nullClaimCount[r, c] > 0;
        public bool IsKingInSolution(int r, int c) => solution?[r, c] ?? false;

        public bool TryPlaceNull(int r, int c)
        {
            if (playerState[r, c] != KingsCellState.Empty) return false;

            undoStack.Push(new UndoAction
            {
                Changes = new[] { new CellChange { Row = r, Col = c, Prev = KingsCellState.Empty } }
            });
            playerState[r, c] = KingsCellState.Null;
            return true;
        }

        public bool TryRemove(int r, int c, out List<(int, int)> cascadeRemoved)
        {
            cascadeRemoved = null;
            if (playerState[r, c] == KingsCellState.Empty) return false;
            if (playerState[r, c] == KingsCellState.Null && nullClaimCount[r, c] > 0) return false;

            var prevState = playerState[r, c];
            var changes = new List<CellChange>();
            changes.Add(new CellChange { Row = r, Col = c, Prev = prevState });
            playerState[r, c] = KingsCellState.Empty;

            if (prevState == KingsCellState.King)
            {
                crownsPerRow[r]--;
                crownsPerCol[c]--;
                crownsPerRegion[Cells[r, c].SectionIndex]--;
                totalCrowns--;

                cascadeRemoved = new List<(int, int)>();
                int region = Cells[r, c].SectionIndex;

                for (int rr = 0; rr < GridSize; rr++)
                {
                    for (int cc = 0; cc < GridSize; cc++)
                    {
                        if (rr == r && cc == c) continue;
                        bool claimedByThisKing = rr == r || cc == c || Cells[rr, cc].SectionIndex == region
                            || (Math.Abs(rr - r) <= 1 && Math.Abs(cc - c) <= 1);
                        if (!claimedByThisKing) continue;

                        if (nullClaimCount[rr, cc] > 0)
                            nullClaimCount[rr, cc]--;

                        if (nullClaimCount[rr, cc] == 0 && playerState[rr, cc] == KingsCellState.Null)
                        {
                            changes.Add(new CellChange { Row = rr, Col = cc, Prev = KingsCellState.Null });
                            playerState[rr, cc] = KingsCellState.Empty;
                            cascadeRemoved.Add((rr, cc));
                        }
                    }
                }
            }

            undoStack.Push(new UndoAction { Changes = changes.ToArray() });
            return true;
        }

        public bool TryPlaceKing(int r, int c, out List<(int, int)> autoFilled)
        {
            autoFilled = new List<(int, int)>();
            if (playerState[r, c] != KingsCellState.Empty) return false;
            if (HasAdjacentKing(r, c)) return false;

            playerState[r, c] = KingsCellState.King;
            crownsPerRow[r]++;
            crownsPerCol[c]++;
            crownsPerRegion[Cells[r, c].SectionIndex]++;
            totalCrowns++;

            var changes = new List<CellChange>();
            changes.Add(new CellChange { Row = r, Col = c, Prev = KingsCellState.Empty });

            int region = Cells[r, c].SectionIndex;

            for (int rr = 0; rr < GridSize; rr++)
            {
                for (int cc = 0; cc < GridSize; cc++)
                {
                    if (rr == r && cc == c) continue;
                    bool claims = rr == r || cc == c || Cells[rr, cc].SectionIndex == region
                        || (Math.Abs(rr - r) <= 1 && Math.Abs(cc - c) <= 1);
                    if (!claims) continue;

                    nullClaimCount[rr, cc]++;

                    if (playerState[rr, cc] == KingsCellState.Empty)
                    {
                        changes.Add(new CellChange { Row = rr, Col = cc, Prev = KingsCellState.Empty });
                        playerState[rr, cc] = KingsCellState.Null;
                        autoFilled.Add((rr, cc));
                    }
                }
            }

            undoStack.Push(new UndoAction { Changes = changes.ToArray() });
            return true;
        }

        public bool FindConflict(int r, int c, out int conflictR, out int conflictC)
        {
            for (int i = 0; i < 8; i++)
            {
                int nr = r + DR[i], nc = c + DC[i];
                if (nr < 0 || nr >= GridSize || nc < 0 || nc >= GridSize) continue;
                if (playerState[nr, nc] == KingsCellState.King)
                {
                    conflictR = nr; conflictC = nc;
                    return true;
                }
            }

            for (int cc = 0; cc < GridSize; cc++)
            {
                if (cc == c) continue;
                if (playerState[r, cc] == KingsCellState.King)
                {
                    conflictR = r; conflictC = cc;
                    return true;
                }
            }

            for (int rr = 0; rr < GridSize; rr++)
            {
                if (rr == r) continue;
                if (playerState[rr, c] == KingsCellState.King)
                {
                    conflictR = rr; conflictC = c;
                    return true;
                }
            }

            int region = Cells[r, c].SectionIndex;
            for (int rr = 0; rr < GridSize; rr++)
            {
                for (int cc = 0; cc < GridSize; cc++)
                {
                    if (rr == r && cc == c) continue;
                    if (Cells[rr, cc].SectionIndex == region && playerState[rr, cc] == KingsCellState.King)
                    {
                        conflictR = rr; conflictC = cc;
                        return true;
                    }
                }
            }

            conflictR = -1; conflictC = -1;
            return false;
        }

        public bool Undo()
        {
            if (undoStack.Count == 0) return false;

            var action = undoStack.Pop();
            foreach (var change in action.Changes)
            {
                if (playerState[change.Row, change.Col] == KingsCellState.King)
                {
                    crownsPerRow[change.Row]--;
                    crownsPerCol[change.Col]--;
                    crownsPerRegion[Cells[change.Row, change.Col].SectionIndex]--;
                    totalCrowns--;

                    int r = change.Row, c = change.Col;
                    int region = Cells[r, c].SectionIndex;
                    for (int rr = 0; rr < GridSize; rr++)
                        for (int cc = 0; cc < GridSize; cc++)
                            if (nullClaimCount[rr, cc] > 0 && (rr == r || cc == c || Cells[rr, cc].SectionIndex == region
                                || (Math.Abs(rr - r) <= 1 && Math.Abs(cc - c) <= 1)))
                                nullClaimCount[rr, cc]--;
                }
                else if (change.Prev == KingsCellState.King)
                {
                    crownsPerRow[change.Row]++;
                    crownsPerCol[change.Col]++;
                    crownsPerRegion[Cells[change.Row, change.Col].SectionIndex]++;
                    totalCrowns++;

                    int r = change.Row, c = change.Col;
                    int region = Cells[r, c].SectionIndex;
                    for (int rr = 0; rr < GridSize; rr++)
                        for (int cc = 0; cc < GridSize; cc++)
                            if (rr == r || cc == c || Cells[rr, cc].SectionIndex == region
                                || (Math.Abs(rr - r) <= 1 && Math.Abs(cc - c) <= 1))
                                nullClaimCount[rr, cc]++;
                }

                playerState[change.Row, change.Col] = change.Prev;
            }
            return true;
        }

        public bool HasAdjacentKing(int r, int c)
        {
            for (int i = 0; i < 8; i++)
            {
                int nr = r + DR[i], nc = c + DC[i];
                if (nr < 0 || nr >= GridSize || nc < 0 || nc >= GridSize) continue;
                if (playerState[nr, nc] == KingsCellState.King)
                    return true;
            }
            return false;
        }

        public bool IsSolved()
        {
            for (int r = 0; r < GridSize; r++)
                if (crownsPerRow[r] != 1) return false;

            for (int c = 0; c < GridSize; c++)
                if (crownsPerCol[c] != 1) return false;

            for (int i = 0; i < RegionCount; i++)
                if (crownsPerRegion[i] != 1) return false;

            if (totalCrowns != GridSize) return false;

            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (playerState[r, c] != KingsCellState.King) continue;
                    for (int i = 0; i < 8; i++)
                    {
                        int nr = r + DR[i], nc = c + DC[i];
                        if (nr < 0 || nr >= GridSize || nc < 0 || nc >= GridSize) continue;
                        if (playerState[nr, nc] == KingsCellState.King)
                            return false;
                    }
                }
            }

            return true;
        }

        public List<string> GetViolations()
        {
            var violations = new List<string>();

            for (int r = 0; r < GridSize; r++)
                if (crownsPerRow[r] > 1)
                    violations.Add($"Row {r + 1} has {crownsPerRow[r]} crowns");

            for (int c = 0; c < GridSize; c++)
                if (crownsPerCol[c] > 1)
                    violations.Add($"Column {c + 1} has {crownsPerCol[c]} crowns");

            for (int i = 0; i < RegionCount; i++)
                if (crownsPerRegion[i] > 1)
                    violations.Add($"Region {i + 1} has {crownsPerRegion[i]} crowns");

            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (playerState[r, c] != KingsCellState.King) continue;
                    for (int i = 0; i < 8; i++)
                    {
                        int nr = r + DR[i], nc = c + DC[i];
                        if (nr < 0 || nr >= GridSize || nc < 0 || nc >= GridSize) continue;
                        if (playerState[nr, nc] == KingsCellState.King)
                        {
                            violations.Add($"Crowns at ({r + 1},{c + 1}) and ({nr + 1},{nc + 1}) are adjacent");
                            break;
                        }
                    }
                }
            }

            return violations;
        }

        public void Reset()
        {
            undoStack.Clear();
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    playerState[r, c] = KingsCellState.Empty;
                    nullClaimCount[r, c] = 0;
                }
                crownsPerRow[r] = 0;
                crownsPerCol[r] = 0;
            }
            for (int i = 0; i < RegionCount; i++)
                crownsPerRegion[i] = 0;
            totalCrowns = 0;
        }
    }
}