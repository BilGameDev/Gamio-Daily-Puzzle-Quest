using System;
using Gamio.Core;
using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

namespace Gamio.Games.Pipes
{
    public class PipesGridUI : GameUI
    {
        [Header("References")]
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private PipesCellItem cellPrefab;
        [Header("Visual")]
        [SerializeField] private Color cellBackground = new Color(0.12f, 0.12f, 0.14f);
        [SerializeField] private float gapRatio = 0.08f;

        private PipesGridController grid;
        private PipesCellItem[,] cells;
        private int size;
        private float cellSize;
        private bool showSolution;

        public event Action OnSolved;

        protected override void OnEnable()
        {
            base.OnEnable();
            PipesGame.OnControllerCreated += OnControllerCreated;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PipesGame.OnControllerCreated -= OnControllerCreated;
        }

        protected override void Start()
        {
            base.Start();

            if (launchOnStart)
            {
                LaunchGame(new PipesGame());
            }
        }

        public void Setup(PipesGridController controller)
        {
            CleanupGrid();

            grid = controller;
            size = grid.Puzzle.GridSize;
            grid.OnSolved += HandleSolved;
            showSolution = false;

            var config = PipesGame.ActiveSettings.GetConfig(PipesGame.CurrentDifficulty);
            cellSize = config.cellSize.x;

            BuildGrid();
        }

        private void BuildGrid()
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = size;
            gridLayout.cellSize = new Vector2(cellSize, cellSize);

            var spacing = Mathf.RoundToInt(cellSize * gapRatio);
            gridLayout.spacing = new Vector2(spacing, spacing);

            cells = new PipesCellItem[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Row = r;
                    cell.Col = c;
                    cell.GetComponent<Image>().color = cellBackground;
                    cell.transform.localScale = Vector3.zero;
                    float delay = (r * size + c) * 0.025f;
                    cell.transform.DOScale(Vector3.one, 0.3f).SetDelay(delay).SetEase(Ease.OutBack);
                    cell.OnClick += OnCellClick;
                    cells[r, c] = cell;
                }

            RefreshAll();
        }

        private void OnCellClick(int row, int col)
        {
            HapticsHelper.PlaySoftImpact();
            HapticsHelper.PlayEmphasis(0.3f, 0.6f);
            grid.TapCell(row, col);
            cells[row, col].PlayTapAnimation();
            RefreshAll();
            grid.Check();
        }

        private void HandleSolved()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            OnSolved?.Invoke();

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    if (grid.Puzzle.Cells[r, c].IsPort)
                        cells[r, c].SetPortConnected(grid.Puzzle.Cells[r, c].PortDirection);

            float delay = 0f;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    int row = r, col = c;
                    cells[r, c].PlaySolvedAnimation(delay);
                    DOVirtual.DelayedCall(delay, () => HapticsHelper.PlayEmphasis(0.2f + (row + col) % 3 * 0.1f, 0.4f));
                    delay += 0.03f;
                }
        }

        private void OnControllerCreated(PipesGridController controller)
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
            }
        }

        public void Undo()
        {
            grid.Undo();
            RefreshAll();
        }

        public void Check()
        {
            grid.Check();
            RefreshAll();
        }

        protected override void ResetPuzzle()
        {
            showSolution = false;
            grid.ResetPuzzle();
            RefreshAll();
        }

        protected override void OnHint()
        {
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    var cell = grid.Puzzle.Cells[r, c];
                    if (cell.IsPort || cell.IsFixed) continue;
                    if (!grid.Puzzle.IsRotationCorrect(r, c))
                    {
                        grid.Puzzle.SetRotation(r, c, grid.Puzzle.GetTargetRotation(r, c));
                        cells[r, c].PlayTapAnimation();
                        RefreshAll();
                        grid.Check();
                        return;
                    }
                }
        }

        private void RefreshAll()
        {
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    var cell = grid.Puzzle.Cells[r, c];
                    int currentRot = grid.Puzzle.GetRotation(r, c);

                    if (showSolution)
                        currentRot = grid.Puzzle.GetTargetRotation(r, c);

                    cells[r, c].SetVisual(cell.Type, currentRot, cell.IsPort, cell.PortDirection);
                }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            CleanupGrid();
            PipesGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}
