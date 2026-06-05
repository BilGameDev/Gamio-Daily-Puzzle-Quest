using System;
using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using Gamio.Core;
namespace Gamio.Games.Kings
{
    public class KingsGridUI : GameUI
    {
        [Header("References")]
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private KingsCellItem cellPrefab;
        [Header("Layout")]
        [SerializeField] private Vector2 desiredCellSize = new Vector2(120, 130);
        [SerializeField] private Vector2 maxCellSize = new Vector2(200, 220);

        private KingsGridController grid;
        private KingsCellItem[,] cells;
        private int size;
        private bool showSolution;
        private int hintRevealCount;

        public event Action OnSolved;

        protected override void OnEnable()
        {
            base.OnEnable();
            KingsGame.OnControllerCreated += OnControllerCreated;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            KingsGame.OnControllerCreated -= OnControllerCreated;
        }

        protected override void Start()
        {
            base.Start();

            if (launchOnStart)
            {
                LaunchGame(new KingsGame());
            }
        }

        public void Setup(KingsGridController controller)
        {
            CleanupGrid();

            grid = controller;
            size = grid.Puzzle.GridSize;
            grid.OnSolved += HandleSolved;
            grid.OnCellChanged += HandleCellChanged;
            grid.OnPlacementDenied += HandlePlacementDenied;
            showSolution = false;
            hintRevealCount = 0;
            BuildGrid();
        }

        private void BuildGrid()
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = size;
            gridLayout.cellSize = desiredCellSize;

            var spacing = Mathf.RoundToInt(Mathf.Min(desiredCellSize.x, desiredCellSize.y) * 0.06f);
            gridLayout.spacing = new Vector2(spacing, spacing);

            cells = new KingsCellItem[size, size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    var puzzleCell = grid.Puzzle.Cells[r, c];
                    Color sectionColor = PastelColors.GetDistinct(puzzleCell.SectionIndex);

                    cell.Init(r, c, puzzleCell.SectionIndex, sectionColor);
                    cell.transform.localScale = Vector3.zero;
                    float delay = (r * size + c) * 0.015f;
                    cell.transform.DOScale(Vector3.one, 0.3f).SetDelay(delay).SetEase(Ease.OutBack);
                    cell.OnTap += OnCellTap;
                    cell.OnHold += OnCellHold;
                    cells[r, c] = cell;
                }
            }

            RefreshAll();
        }

        private void OnCellTap(int row, int col)
        {
            if (grid.IsSolved) return;
            HapticsHelper.PlaySoftImpact();
            if (grid.TapCell(row, col))
            {
                HapticsHelper.PlayEmphasis(0.3f, 0.5f);
                cells[row, col].PlayTapAnimation();
            }
        }

        private void OnCellHold(int row, int col)
        {
            if (grid.IsSolved) return;
            if (grid.HoldCell(row, col))
            {
                HapticsHelper.PlayEmphasis(0.5f, 0.7f);
                cells[row, col].PlayTapAnimation();
            }
        }

        private void HandleCellChanged(int row, int col)
        {
            var state = grid.Puzzle.GetState(row, col);
            cells[row, col].SetState(state);
        }

        private void HandlePlacementDenied(int row, int col, int conflictR, int conflictC)
        {
            cells[row, col].PlayInvalidAnimation();
            if (conflictR >= 0 && conflictC >= 0)
                cells[conflictR, conflictC].PlayInvalidAnimation();
        }

        private void HandleSolved()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            OnSolved?.Invoke();

            float delay = 0f;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int row = r, col = c;
                    cells[r, c].PlaySolvedAnimation(delay);
                    DOVirtual.DelayedCall(delay, () => HapticsHelper.PlayEmphasis(0.2f + (row + col) % 3 * 0.1f, 0.4f));
                    delay += 0.03f;
                }
            }
        }

        private void OnControllerCreated(KingsGridController controller)
        {
            Setup(controller);
        }

        public void Undo()
        {
            if (grid.Undo())
                RefreshAll();
        }

        protected override void ResetPuzzle()
        {
            if (grid == null) return;
            grid.ResetPuzzle();
            showSolution = false;
            hintRevealCount = 0;
            RefreshAll();
        }

        protected override void OnHint()
        {
            if (grid == null || grid.IsSolved) return;
            showSolution = false;

            int hintTarget = hintRevealCount;
            int hintIndex = 0;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (grid.Puzzle.IsKingInSolution(r, c) &&
                        grid.Puzzle.GetState(r, c) != KingsCellState.King)
                    {
                        if (hintIndex == hintTarget)
                        {
                            hintRevealCount++;
                            grid.HoldCell(r, c);
                            cells[r, c].PlayHintAnimation();
                            return;
                        }
                        hintIndex++;
                    }
                }
            }
        }

        private void RefreshAll()
        {
            if (showSolution)
            {
                for (int r = 0; r < size; r++)
                    for (int c = 0; c < size; c++)
                        cells[r, c].SetState(grid.Puzzle.IsKingInSolution(r, c)
                            ? KingsCellState.King : KingsCellState.Empty);
                return;
            }

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c].SetState(grid.Puzzle.GetState(r, c));
        }

        private void UpdateCellSize()
        {
            var rect = gridLayout.GetComponent<RectTransform>().rect;
            float availW = Mathf.Max(0, rect.width - gridLayout.padding.left - gridLayout.padding.right);
            float availH = Mathf.Max(0, rect.height - gridLayout.padding.top - gridLayout.padding.bottom);
            float totalSpacingX = (size - 1) * gridLayout.spacing.x;
            float totalSpacingY = (size - 1) * gridLayout.spacing.y;
            float maxCellW = Mathf.Max(1, (availW - totalSpacingX) / size);
            float maxCellH = Mathf.Max(1, (availH - totalSpacingY) / size);

            float aspect = desiredCellSize.x / desiredCellSize.y;
            float cellWByH = maxCellH * aspect;
            float cellHByW = maxCellW / aspect;

            float cellW = Mathf.Min(cellWByH <= maxCellW ? cellWByH : maxCellW, maxCellSize.x);
            float cellH = Mathf.Min(cellWByH <= maxCellW ? maxCellH : cellHByW, maxCellSize.y);

            gridLayout.cellSize = new Vector2(Mathf.Max(cellW, 10), Mathf.Max(cellH, 10));
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
                grid.OnCellChanged -= HandleCellChanged;
                grid.OnPlacementDenied -= HandlePlacementDenied;
                grid = null;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            CleanupGrid();
            KingsGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}