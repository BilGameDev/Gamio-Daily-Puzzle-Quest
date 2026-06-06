using Gamio.Core;
using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;

namespace Gamio.Games.LineConnect
{
    public class LineConnectGridUI : GameUI
    {
        [Header("References")]
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private LineConnectCellItem cellPrefab;
        [SerializeField] private LineConnectCellItem endpointPrefab;
        [Header("Layout")]
        [SerializeField] private Vector2 cellSize = new Vector2(100, 110); // fallback only

        private LineConnectGridController grid;
        private LineConnectCellItem[,] cells;
        private int size;
        private bool showSolution;
        private int hintRevealCount;

        public event System.Action OnSolved;

        protected override void OnEnable()
        {
            base.OnEnable();
            LineConnectGame.OnControllerCreated += OnControllerCreated;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            LineConnectGame.OnControllerCreated -= OnControllerCreated;
        }

        protected override void Start()
        {
            base.Start();

            if (launchOnStart)
            {
                LaunchGame(new LineConnectGame());
            }
        }

        public void Setup(LineConnectGridController controller)
        {
            CleanupGrid();

            grid = controller;
            size = grid.Puzzle.GridSize;
            grid.OnSolved += HandleSolved;
            grid.OnVisualsChanged += RefreshAll;
            grid.OnPathConnected += OnPathConnected;
            hintRevealCount = 0;
            showSolution = false;
            BuildGrid();
        }

        protected override void OnHint()
        {
            if (grid == null || grid.IsSolved) return;
            showSolution = false;

            var puzzle = grid.Puzzle;
            int hintTarget = hintRevealCount;
            int hintIndex = 0;
            for (int c = 0; c < puzzle.ColorCount; c++)
            {
                bool completed = false;
                for (int r = 0; r < size && !completed; r++)
                    for (int col = 0; col < size && !completed; col++)
                        if (puzzle.GetPathId(r, col) == c) completed = true;
                if (!completed)
                {
                    if (hintIndex == hintTarget)
                    {
                        hintRevealCount++;
                        RefreshAll();
                        return;
                    }
                    hintIndex++;
                }
            }
        }

        protected override void ResetPuzzle()
        {
            if (grid == null) return;
            grid.ResetPuzzle();
            hintRevealCount = 0;
            showSolution = false;
        }

        private void BuildGrid()
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = size;
            gridLayout.cellSize = LineConnectGame.ActiveSettings != null ? LineConnectGame.CurrentCellSize : cellSize;

            cells = new LineConnectCellItem[size, size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cellData = grid.Puzzle.Cells[r, c];
                    var prefab = cellData.IsEndpoint && endpointPrefab != null ? endpointPrefab : cellPrefab;
                    var cell = Instantiate(prefab, gridLayout.transform);
                    cell.Init(r, c, cellData.ColorId, cellData.IsEndpoint);
                    cell.OnPointerDownEvent += OnCellDown;
                    cell.OnPointerEnterEvent += OnCellEnter;
                    cell.OnPointerUpEvent += OnCellUp;
                    cells[r, c] = cell;
                }
            }

            RefreshAll();
        }

        private void OnCellDown(int row, int col)
        {
            grid.StartDrag(row, col);
        }

        private void OnCellEnter(int row, int col)
        {
            if (!grid.IsDragging) return;
            grid.UpdateDrag(row, col);
        }

        private void OnCellUp()
        {
            grid.EndDrag();
            HapticsHelper.PlaySoftImpact();
        }

        private void RefreshAll()
        {
            var puzzle = grid.Puzzle;

            bool[] hinted = hintRevealCount > 0 ? GetHintedColors() : null;

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int pathId = puzzle.GetPathId(r, c);
                    var cellData = puzzle.Cells[r, c];
                    int solColor = cellData.ColorId;

                    bool isHintCell = hinted != null && solColor >= 0 && hinted[solColor];

                    if (isHintCell)
                    {
                        var color = puzzle.ColorPalette[solColor % puzzle.ColorPalette.Length];
                        color.a = 0.65f;
                        cells[r, c].SetColor(color);
                    }
                    else if (showSolution)
                    {
                        cells[r, c].SetColor(solColor >= 0
                            ? puzzle.ColorPalette[solColor % puzzle.ColorPalette.Length]
                            : new Color(0.15f, 0.15f, 0.17f));
                    }
                    else if (pathId >= 0)
                    {
                        cells[r, c].SetColor(puzzle.ColorPalette[pathId % puzzle.ColorPalette.Length]);
                    }
                    else if (cellData.IsEndpoint)
                    {
                        cells[r, c].SetColor(puzzle.ColorPalette[cellData.ColorId % puzzle.ColorPalette.Length]);
                    }
                    else
                    {
                        cells[r, c].SetColor(new Color(0.15f, 0.15f, 0.17f));
                    }
                }
            }

            if (grid.IsDragging)
            {
                var activeColor = puzzle.ColorPalette[grid.ActiveColorId % puzzle.ColorPalette.Length];
                foreach (var (r, c) in grid.ActivePath)
                {
                    var cd = puzzle.Cells[r, c];
                    Color ac = activeColor;
                    ac.a = cd.IsEndpoint ? 0.8f : 0.55f;
                    cells[r, c].SetColor(ac);
                }
            }
        }

        private bool[] GetHintedColors()
        {
            var puzzle = grid.Puzzle;
            var hinted = new bool[puzzle.ColorCount];
            int hintCount = 0;
            for (int c = 0; c < puzzle.ColorCount && hintCount < hintRevealCount; c++)
            {
                bool completed = false;
                for (int r = 0; r < size && !completed; r++)
                    for (int col = 0; col < size && !completed; col++)
                        if (puzzle.GetPathId(r, col) == c) completed = true;
                if (!completed)
                {
                    hinted[c] = true;
                    hintCount++;
                }
            }
            return hinted;
        }

        private void OnPathConnected(int colorId, List<(int r, int c)> path)
        {
            float delay = 0;
            foreach (var (r, c) in path)
            {
                int row = r, col = c;
                DOVirtual.DelayedCall(delay, () =>
                {
                    cells[row, col].transform.DOKill();
                    cells[row, col].transform.localScale = Vector3.one;
                    cells[row, col].transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 4, 0.5f)
                        .SetEase(Ease.OutQuad);
                });
                delay += 0.03f;
            }
        }

        private void HandleSolved()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            OnSolved?.Invoke();
            float delay = 0;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    int row = r, col = c;
                    cells[r, c].transform.DOKill();
                    cells[r, c].transform.localScale = Vector3.one;
                    cells[r, c].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 4, 0.5f)
                        .SetDelay(delay).SetEase(Ease.OutQuad);
                    delay += 0.015f;
                }
        }

        private void OnControllerCreated(LineConnectGridController controller)
        {
            Setup(controller);
        }

        private void CleanupGrid()
        {
            if (cells != null)
            {
                for (int r = 0; r < cells.GetLength(0); r++)
                    for (int c = 0; c < cells.GetLength(1); c++)
                        if (cells[r, c] != null)
                            Destroy(cells[r, c].gameObject);
                cells = null;
            }

            if (grid != null)
            {
                grid.OnSolved -= HandleSolved;
                grid.OnVisualsChanged -= RefreshAll;
                grid.OnPathConnected -= OnPathConnected;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            CleanupGrid();
            LineConnectGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}