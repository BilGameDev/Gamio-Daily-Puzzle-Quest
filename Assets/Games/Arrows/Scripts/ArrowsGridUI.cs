using DG.Tweening;
using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.InputSystem;
using Gamio.Core;
using Gamio.Features.UI;

namespace Gamio.Games.Arrows
{
    public class ArrowsGridUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CenteredGridLayout gridLayout;
        [SerializeField] private ArrowsCellItem cellPrefab;
        [SerializeField] private Vector2 cellSize = new Vector2(105, 105);

        private ArrowsGridController grid;
        private ArrowsCellItem[,] cells;
        private int rows;
        private int cols;

        private ArrowsGameSettingsSO settings;

        public event System.Action OnSolved;

        private void OnEnable()
        {
            ArrowsGame.OnControllerCreated += OnControllerCreated;
            GamioEvents.OnResetRequested += OnResetRequested;
            if (ArrowsGame.CurrentController != null)
                Setup(ArrowsGame.CurrentController);
        }

        private void OnDisable()
        {
            ArrowsGame.OnControllerCreated -= OnControllerCreated;
            GamioEvents.OnResetRequested -= OnResetRequested;
            if (grid != null)
            {
                grid.OnTileRemoved -= OnTileRemoved;
                grid.OnTileBlocked -= OnTileBlocked;
                grid.OnTileRestored -= OnTileRestored;
                grid.OnSolved -= HandleSolved;
                grid.OnPuzzleReset -= HandleReset;
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
            grid.OnPuzzleReset += HandleReset;

            BuildGrid();
        }

        private void Update()
        {
            if (grid == null) return;

            if (Keyboard.current.rKey.wasPressedThisFrame)
                grid.ResetPuzzle();

            if (Keyboard.current.zKey.wasPressedThisFrame && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed))
                grid.Undo();

            if (Keyboard.current.escapeKey.wasPressedThisFrame && ArrowsGame.CurrentController != null)
            {
                grid.ResetPuzzle();
            }
        }

        private void BuildGrid()
        {
            gridLayout.constraintCount = cols;
            gridLayout.cellSize = ArrowsGame.ActiveSettings != null ? ArrowsGame.CurrentCellSize : cellSize;

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
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Selection);
            grid.TrySlideTile(row, col);
        }

        private void OnTileRemoved(int row, int col)
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.LightImpact);
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
                .OnComplete(() =>
                {
                    cellItem.SetVisible(false);
                    cellItem.RectTransform.anchoredPosition = Vector2.zero;
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
                            .SetDelay(delay).SetEase(Ease.OutQuad);
                        delay += 0.015f;
                    }
                }
            }

            OnSolved?.Invoke();
        }

        private void HandleReset()
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

        private void OnResetRequested()
        {
            if (grid != null)
                grid.ResetPuzzle();
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
                grid.OnPuzzleReset -= HandleReset;
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
