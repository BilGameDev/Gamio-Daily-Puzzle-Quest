using System;
using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace Gamio.Games.Hitori
{
    public class HitoriGridUI : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private HitoriCellItem cellPrefab;
        [SerializeField] private Color cellColor = Color.white;
        [SerializeField] private float maxCellSize = 120f;

        private HitoriGridController grid;
        private HitoriCellItem[,] cells;
        private int size;
        private bool showSolution;
        private int hintRevealCount;

        public event Action OnSolved;

        private void OnEnable()
        {
            HitoriGame.OnControllerCreated += OnControllerCreated;
            if (HitoriGame.CurrentController != null)
                Setup(HitoriGame.CurrentController);
        }

        private void OnDisable()
        {
            HitoriGame.OnControllerCreated -= OnControllerCreated;
        }

        public void Setup(HitoriGridController controller)
        {
            CleanupGrid();

            grid = controller;
            size = grid.Puzzle.GridSize;
            grid.OnSolved += HandleSolved;
            BuildGrid();
        }

        private void BuildGrid()
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = size;
            UpdateCellSize();

            cells = new HitoriCellItem[size, size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Init(r, c, grid.Puzzle.Cells[r, c].Number);
                    cell.Image.color = cellColor;
                    cell.transform.localScale = Vector3.zero;
                    float delay = (r * size + c) * 0.02f;
                    cell.transform.DOScale(Vector3.one, 0.25f).SetDelay(delay).SetEase(Ease.OutBack);
                    cell.OnClick += OnCellClick;
                    cells[r, c] = cell;
                }
            }

            RefreshAll();
        }

        private void OnCellClick(int row, int col)
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Selection);
            grid.TapCell(row, col);
            cells[row, col].PlayTapAnimation();
            RefreshAll();
            foreach (var (vr, vc) in grid.Puzzle.GetViolations())
                cells[vr, vc].PlayViolationAnimation();
        }

        private void HandleSolved()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            OnSolved?.Invoke();

            float delay = 0;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int row = r, col = c;
                    var cell = cells[r, c];
                    cell.transform.DOKill();
                    cell.transform.localScale = Vector3.one;
                    cell.transform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 6, 0.5f)
                        .SetDelay(delay).SetEase(Ease.OutQuad);
                    delay += 0.03f;
                }
            }
        }

        private void Update()
        {
            if (grid == null) return;
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                showSolution = false;
                hintRevealCount++;
                RefreshAll();
            }
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                showSolution = !showSolution;
                RefreshAll();
            }
        }

        private void OnControllerCreated(HitoriGridController controller)
        {
            Setup(controller);
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

        public void ResetPuzzle()
        {
            grid.ResetPuzzle();
            RefreshAll();
        }

        private void RefreshAll()
        {
            int hintCount = 0;
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                bool isHintCell = false;
                if (hintRevealCount > 0 && grid.Puzzle.Cells[r, c].IsBlackInSolution &&
                    grid.Puzzle.GetState(r, c) != HitoriCellState.Black)
                {
                    if (hintCount < hintRevealCount)
                    {
                        isHintCell = true;
                    }
                    hintCount++;
                }

                if (isHintCell)
                {
                    cells[r, c].SetVisual(HitoriCellState.Black, cellColor);
                    cells[r, c].Image.color = new Color(0.2f, 0.5f, 0.2f);
                }
                else if (showSolution)
                {
                    var solState = grid.Puzzle.Cells[r, c].IsBlackInSolution
                        ? HitoriCellState.Black : HitoriCellState.White;
                    cells[r, c].SetVisual(solState, cellColor);
                }
                else
                {
                    var state = grid.Puzzle.GetState(r, c);
                    cells[r, c].SetVisual(state, cellColor);
                }
            }
        }

        private void UpdateCellSize()
        {
            var rect = gridLayout.GetComponent<RectTransform>().rect;
            float availW = rect.width - gridLayout.padding.left - gridLayout.padding.right;
            float availH = rect.height - gridLayout.padding.top - gridLayout.padding.bottom;
            float cellW = (availW - (size - 1) * gridLayout.spacing.x) / size;
            float cellH = (availH - (size - 1) * gridLayout.spacing.y) / size;
            float s = Mathf.Min(cellW, cellH, maxCellSize);
            gridLayout.cellSize = new Vector2(s, s);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (gridLayout != null && grid != null)
            {
                UpdateCellSize();
                LayoutRebuilder.MarkLayoutForRebuild(gridLayout.GetComponent<RectTransform>());
            }
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
                grid = null;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            CleanupGrid();
            HitoriGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}
