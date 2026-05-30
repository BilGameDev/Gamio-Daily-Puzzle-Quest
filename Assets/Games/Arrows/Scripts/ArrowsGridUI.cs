using DG.Tweening;
using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.InputSystem;
using Gamio.Core;
using Gamio.Features.UI;

namespace Gamio.Games.Arrows
{
    public class ArrowsGridUI : GameUI
    {
        [Header("References")]
        [SerializeField] private CenteredGridLayout gridLayout;
        [SerializeField] private ArrowsCellItem cellPrefab;

        private ArrowsGridController grid;
        private ArrowsCellItem[,] cells;
        private int rows;
        private int cols;

        private ArrowsGameSettingsSO settings;

        public event System.Action OnSolved;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (ArrowsGame.CurrentController != null)
                Setup(ArrowsGame.CurrentController);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (grid != null)
            {
                grid.OnTileRemoved -= OnTileRemoved;
                grid.OnTileBlocked -= OnTileBlocked;
                grid.OnTileRestored -= OnTileRestored;
                grid.OnSolved -= HandleSolved;
            }
        }

        protected override void Start()
        {
            base.Start();

            if (launchOnStart)
            {
                LaunchGame(new ArrowsGame());
            }
        }

        public void Setup(ArrowsGridController controller)
        {
            CleanupGrid();

            grid = controller;
            rows = grid.Puzzle.Rows;
            cols = grid.Puzzle.Cols;
            settings = ArrowsGame.ActiveSettings;

            grid.OnTileRemoved += OnTileRemoved;
            grid.OnTileBlocked += OnTileBlocked;
            grid.OnTileRestored += OnTileRestored;
            grid.OnSolved += HandleSolved;

            BuildGrid();
        }

        private void BuildGrid()
        {
            gridLayout.constraintCount = cols;
            gridLayout.cellSize = ArrowsGame.CurrentCellSize;

            cells = new ArrowsCellItem[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cellData = grid.Puzzle.Cells[r, c];
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Init(r, c, cellData.Direction, cellData.IsEmpty, cellData.IsObstacle);
                    cell.OnClick += OnCellClicked;
                    cells[r, c] = cell;
                }
            }
        }

        private void OnCellClicked(int row, int col)
        {
            if (grid == null || grid.IsSolved) return;
            HapticsHelper.PlaySoftImpact();
            grid.TrySlideTile(row, col);
        }

        private void OnTileRemoved(int row, int col)
        {
            HapticsHelper.PlayEmphasis(0.5f, 0.4f);
            var cellItem = cells[row, col];
            var dir = cellItem.Direction;

            Vector2 slideDir = dir switch
            {
                ArrowDirection.Up => Vector2.up,
                ArrowDirection.Down => Vector2.down,
                ArrowDirection.Left => Vector2.left,
                ArrowDirection.Right => Vector2.right,
                _ => Vector2.zero
            };

            int steps = grid.Puzzle.SlideDistance(row, col);
            float cellSize = gridLayout.cellSize.x + gridLayout.spacing.x;
            float distance = steps * cellSize;

            float dur = settings != null ? settings.slideDuration : 0.35f;
            var ease = settings != null ? settings.slideEase : Ease.InBack;

            grid.NotifyAnimationStarted();
            cellItem.SetBlockRaycasts(false);

            cellItem.RectTransform.DOAnchorPos(cellItem.RectTransform.anchoredPosition + slideDir * distance, dur)
                .SetEase(ease)
                .OnPlay(() => HapticsHelper.PlayEmphasis(0.1f, 0.2f))
                .OnComplete(() =>
                {
                    cellItem.SetVisible(false);
                    cellItem.RectTransform.anchoredPosition = Vector2.zero;
                    HapticsHelper.PlayEmphasis(0.3f, 0.5f);
                    grid.NotifyAnimationComplete();
                });
        }

        private void OnTileRestored(int row, int col)
        {
            var cellItem = cells[row, col];
            var cellData = grid.Puzzle.Cells[row, col];
            cellItem.RectTransform.DOKill();
            cellItem.RectTransform.anchoredPosition = Vector2.zero;
            cellItem.transform.localScale = Vector3.one;
            cellItem.Init(row, col, cellData.Direction, false, false);
            cellItem.SetVisible(true);
            cellItem.transform.DOScale(Vector3.one, 0.2f).From(0).SetEase(Ease.OutBack);
        }

        private void OnTileBlocked(int row, int col, int blockerRow, int blockerCol)
        {
            if (blockerRow >= 0 && blockerCol >= 0)
            {
                var blocker = cells[blockerRow, blockerCol];
                if (blocker != null)
                    blocker.Flash();
            }
        }

        private void HandleSolved()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            float delay = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = cells[r, c];
                    if (cell != null && cell.IsVisible() && grid.Puzzle.HasTile(r, c))
                    {
                        cell.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 4, 0.5f)
                            .SetDelay(delay).SetEase(Ease.OutQuad)
                            .OnPlay(() => HapticsHelper.PlayEmphasis(0.2f, 0.4f));
                        delay += 0.015f;
                    }
                }
            }

            OnSolved?.Invoke();
        }

        protected override void ResetPuzzle()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = cells[r, c];
                    if (cell != null)
                    {
                        cell.RectTransform.DOKill();
                        cell.RectTransform.anchoredPosition = Vector2.zero;
                        cell.transform.localScale = Vector3.one;
                        var cellData = grid.Puzzle.Cells[r, c];
                        cell.Init(r, c, cellData.Direction, cellData.IsEmpty, cellData.IsObstacle);
                        cell.SetVisible(true);
                        cell.SetBlockRaycasts(!cellData.IsEmpty && !cellData.IsObstacle);
                    }
                }
            }
        }

        private void OnControllerCreated(ArrowsGridController controller)
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
                grid.OnTileRemoved -= OnTileRemoved;
                grid.OnTileBlocked -= OnTileBlocked;
                grid.OnTileRestored -= OnTileRestored;
                grid.OnSolved -= HandleSolved;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            CleanupGrid();
            ArrowsGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}
