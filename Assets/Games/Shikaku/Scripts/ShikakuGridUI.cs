using System.Collections.Generic;
using Gamio.Core;
using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using System;

namespace Gamio.Games.Shikaku
{
    public class ShikakuGridUI : GameUI
    {
        [Header("References")]
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private ShikakuCellItem cellPrefab;
        [SerializeField] private RectTransform selectionOverlay;
        [Header("Colors")]
        [SerializeField] private Color cellColor = Color.white;

        private static readonly Color[] SolutionOverlayPalette = new[]
        {
            new Color(0.9f, 0.3f, 0.3f, 0.45f),
            new Color(0.3f, 0.9f, 0.3f, 0.45f),
            new Color(0.3f, 0.3f, 0.9f, 0.45f),
            new Color(0.9f, 0.9f, 0.3f, 0.45f),
            new Color(0.9f, 0.3f, 0.9f, 0.45f),
            new Color(0.3f, 0.9f, 0.9f, 0.45f),
            new Color(0.9f, 0.6f, 0.2f, 0.45f),
            new Color(0.6f, 0.2f, 0.9f, 0.45f),
        };

        private ShikakuGridController grid;
        private ShikakuCellItem[,] cells;
        private int rows;
        private int cols;
        private bool showSolution;
        private int hintRevealCount;
        private Color currentDragColor;
        private List<RectTransform> placedOverlays;
        private List<RectTransform> solutionOverlays;
        private readonly List<int> removedBuffer = new List<int>();

        public event Action OnSolved;

        protected override void OnEnable()
        {
            base.OnEnable();
            ShikakuGame.OnControllerCreated += OnControllerCreated;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ShikakuGame.OnControllerCreated -= OnControllerCreated;
        }

        protected override void Start()
        {
            base.Start();
            
            if (launchOnStart)
            {
                LaunchGame(new ShikakuGame());
            }
        }

        public void Setup(ShikakuGridController controller)
        {
            CleanupGrid();

            grid = controller;
            rows = grid.Puzzle.Rows;
            cols = grid.Puzzle.Cols;
            grid.OnSolved += HandleSolved;
            BuildGrid();
        }

        private void BuildGrid()
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = cols;
            var cfg = ShikakuGame.ActiveSettings.GetConfig(ShikakuGame.CurrentDifficulty);
            gridLayout.cellSize = cfg.cellSize;

            cells = new ShikakuCellItem[rows, cols];
            placedOverlays = new List<RectTransform>();
            solutionOverlays = new List<RectTransform>();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Init(r, c, grid.Puzzle.Cells[r, c].Number);
                    cell.Image.color = cellColor;
                    cell.transform.localScale = Vector3.zero;
                    float delay = (r * cols + c) * 0.02f;
                    cell.transform.DOScale(Vector3.one, 0.25f).SetDelay(delay).SetEase(Ease.OutBack);
                    cell.OnPointerDownEvent += (row, col) => OnCellDown(row, col);
                    cell.OnPointerEnterEvent += (row, col) => OnCellEnter(row, col);
                    cell.OnPointerUpEvent += OnCellUp;
                    cells[r, c] = cell;
                }
            }

            if (selectionOverlay != null)
                selectionOverlay.gameObject.SetActive(false);

            RefreshVisuals();
        }

        private void RemovePlacedOverlayAt(int idx)
        {
            if (idx >= placedOverlays.Count) return;
            var o = placedOverlays[idx];
            if (o != null)
            {
                o.DOKill();
                var img = o.GetComponent<Image>();
                img?.DOFade(0f, 0.15f);
                o.DOScale(Vector3.one * 0.8f, 0.15f).OnComplete(() => { if (o != null) Destroy(o.gameObject); });
            }
            placedOverlays.RemoveAt(idx);
        }

        private List<GameObject> hintOverlays = new List<GameObject>();

        private void ShowHintOverlays()
        {
            var rects = grid.Puzzle.SolutionRects;
            int showCount = Mathf.Min(hintRevealCount, rects.Count);
            for (int i = hintOverlays.Count; i < showCount; i++)
            {
                var r = rects[i];
                var overlay = new GameObject("HintOverlay", typeof(RectTransform), typeof(Image));
                overlay.transform.SetParent(selectionOverlay.parent, false);
                var img = overlay.GetComponent<Image>();
                img.color = new Color(1f, 1f, 0f, 0.5f);
                var rt = overlay.GetComponent<RectTransform>();
                PositionOverlayOnRect(rt, r.Row, r.Col, r.Bottom, r.Right);
                overlay.gameObject.SetActive(true);
                hintOverlays.Add(overlay);
            }
        }

        private void HideHintOverlays()
        {
            if (hintOverlays == null) return;
            foreach (var o in hintOverlays)
                if (o != null) Destroy(o);
            hintOverlays.Clear();
        }

        private void AddPlacedOverlay(ShikakuRect rect)
        {
            var overlay = Instantiate(selectionOverlay, selectionOverlay.parent);
            PositionOverlayOnRect(overlay, rect.Row, rect.Col, rect.Bottom, rect.Right);
            var img = overlay.GetComponent<Image>();
            var c = rect.Color;
            c.a = 0f;
            img.color = c;
            overlay.localScale = Vector3.one * 0.5f;
            overlay.gameObject.SetActive(true);
            c.a = 0.75f;
            img.DOColor(c, 0.2f);
            overlay.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            placedOverlays.Add(overlay);
        }

        private void OnCellDown(int row, int col)
        {
            var tapRect = new ShikakuRect { Row = row, Col = col, Height = 1, Width = 1 };
            grid.Puzzle.RemoveRectsOverlapping(tapRect, removedBuffer);
            if (removedBuffer.Count > 0)
            {
                for (int i = 0; i < removedBuffer.Count; i++)
                    RemovePlacedOverlayAt(removedBuffer[i]);
                RefreshVisuals();
                return;
            }

            currentDragColor = Color.HSVToRGB(UnityEngine.Random.value, 0.28f, 0.92f);
            grid.StartDrag(row, col);
            grid.RemoveOverlappingDuringDrag(removedBuffer);
            for (int i = 0; i < removedBuffer.Count; i++)
                RemovePlacedOverlayAt(removedBuffer[i]);
            RefreshVisuals();
        }

        private void OnCellEnter(int row, int col)
        {
            if (!grid.IsDragging) return;
            grid.UpdateDrag(row, col);
            grid.RemoveOverlappingDuringDrag(removedBuffer);
            for (int i = 0; i < removedBuffer.Count; i++)
                RemovePlacedOverlayAt(removedBuffer[i]);
            RefreshVisuals();
        }

        private void OnCellUp()
        {
            if (!grid.IsDragging) return;

            bool placed = grid.EndDrag(currentDragColor);
            if (placed)
            {
                HapticsHelper.PlaySoftImpact();
                var rects = grid.Puzzle.PlayerRects;
                AddPlacedOverlay(rects[rects.Count - 1]);
                RefreshVisuals();
            }
            else
            {
                RefreshVisuals();
            }
        }

        private void HandleSolved()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            OnSolved?.Invoke();
            RefreshVisuals();

            float delay = 0.5f;
            int i = 0;
            foreach (var overlay in placedOverlays)
            {
                int idx = i;
                if (overlay != null)
                {
                    overlay.DOKill();
                    overlay.localScale = Vector3.one;
                    overlay.DOPunchScale(Vector3.one * 0.08f, 0.6f, 3, 0.3f)
                        .SetDelay(delay).SetEase(Ease.OutQuad);
                }
                delay += 0.05f;
                i++;
            }
        }

        private void OnControllerCreated(ShikakuGridController controller)
        {
            Setup(controller);
        }

        public void Undo()
        {
            grid.Undo();
            if (placedOverlays.Count > 0)
                RemovePlacedOverlayAt(placedOverlays.Count - 1);
            RefreshVisuals();
        }

        public void Check()
        {
            grid.Check();
            RefreshVisuals();
        }

        protected override void ResetPuzzle()
        {
            grid.ResetPuzzle();
            ClearPlacedOverlays();
            RefreshVisuals();
        }

        protected override void OnHintGranted()
        {
            if (grid == null) return;
            var rects = grid.Puzzle.SolutionRects;
            if (hintRevealCount >= rects.Count) return;
            var solRect = rects[hintRevealCount];
            hintRevealCount++;

            currentDragColor = Color.HSVToRGB(UnityEngine.Random.value, 0.28f, 0.92f);
            grid.StartDrag(solRect.Row, solRect.Col);
            grid.UpdateDrag(solRect.Bottom, solRect.Right);
            grid.EndDrag(currentDragColor);

            ClearPlacedOverlays();
            foreach (var pr in grid.Puzzle.PlayerRects)
                AddPlacedOverlay(pr);
            RefreshVisuals();

            if (grid.Puzzle.IsSolved())
                grid.Check();
        }

        private void ClearPlacedOverlays()
        {
            if (placedOverlays == null) return;

            foreach (var o in placedOverlays)
            {
                if (o != null)
                {
                    o.DOKill();
                    var img = o.GetComponent<Image>();
                    img?.DOFade(0f, 0.15f);
                    o.DOScale(Vector3.one * 0.8f, 0.15f).OnComplete(() => { if (o != null) Destroy(o.gameObject); });
                }
            }
            placedOverlays.Clear();
        }

        private void ShowSolutionOverlays()
        {
            HideSolutionOverlays();
            var rects = grid.Puzzle.SolutionRects;
            for (int i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                var overlay = Instantiate(selectionOverlay, selectionOverlay.parent);
                PositionOverlayOnRect(overlay, r.Row, r.Col, r.Bottom, r.Right);
                var img = overlay.GetComponent<Image>();
                var c = SolutionOverlayPalette[i % SolutionOverlayPalette.Length];
                img.color = c;
                overlay.gameObject.SetActive(true);
                solutionOverlays.Add(overlay);
            }
        }

        private void HideSolutionOverlays()
        {
            if (solutionOverlays == null) return;
            foreach (var o in solutionOverlays)
                if (o != null) Destroy(o.gameObject);
            solutionOverlays.Clear();
        }

        private void PositionOverlayOnRect(RectTransform overlay, int r1, int c1, int r2, int c2)
        {
            var topLeftCell = cells[r1, c1].GetComponent<RectTransform>();
            var bottomRightCell = cells[r2, c2].GetComponent<RectTransform>();
            var parentRT = overlay.parent as RectTransform;
            var canvas = topLeftCell.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            var corners = new Vector3[4];
            topLeftCell.GetWorldCorners(corners);
            var worldTL = corners[1];
            bottomRightCell.GetWorldCorners(corners);
            var worldBR = corners[3];

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, RectTransformUtility.WorldToScreenPoint(cam, worldTL), cam, out var localTL);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, RectTransformUtility.WorldToScreenPoint(cam, worldBR), cam, out var localBR);

            var w = localBR.x - localTL.x;
            var h = localTL.y - localBR.y;

            overlay.anchoredPosition = new Vector2(
                localTL.x + w * overlay.pivot.x,
                localTL.y - h * (1f - overlay.pivot.y));
            overlay.sizeDelta = new Vector2(w, h);
        }

        public void RefreshVisuals()
        {
            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (selectionOverlay == null) return;
            var overlayImg = selectionOverlay.GetComponent<Image>();

            if (!grid.IsDragging)
            {
                if (selectionOverlay.gameObject.activeSelf)
                {
                    selectionOverlay.DOKill();
                    overlayImg?.DOFade(0f, 0.12f);
                    selectionOverlay.DOScale(Vector3.one * 0.8f, 0.12f).OnComplete(() =>
                    {
                        if (selectionOverlay != null) selectionOverlay.gameObject.SetActive(false);
                    });
                }
                return;
            }

            int r1 = Mathf.Min(grid.DragStartRow, grid.DragEndRow);
            int c1 = Mathf.Min(grid.DragStartCol, grid.DragEndCol);
            int r2 = Mathf.Max(grid.DragStartRow, grid.DragEndRow);
            int c2 = Mathf.Max(grid.DragStartCol, grid.DragEndCol);

            var topLeftCell = cells[r1, c1].GetComponent<RectTransform>();
            var bottomRightCell = cells[r2, c2].GetComponent<RectTransform>();
            var parentRT = selectionOverlay.parent as RectTransform;
            var canvas = topLeftCell.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            var corners = new Vector3[4];
            topLeftCell.GetWorldCorners(corners);
            var worldTL = corners[1];
            bottomRightCell.GetWorldCorners(corners);
            var worldBR = corners[3];

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, RectTransformUtility.WorldToScreenPoint(cam, worldTL), cam, out var localTL);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, RectTransformUtility.WorldToScreenPoint(cam, worldBR), cam, out var localBR);

            var w = localBR.x - localTL.x;
            var h = localTL.y - localBR.y;

            var targetPos = new Vector2(
                localTL.x + w * selectionOverlay.pivot.x,
                localTL.y - h * (1f - selectionOverlay.pivot.y));
            var targetSize = new Vector2(w, h);

            var dragColor = currentDragColor;
            dragColor.a = 1f;

            if (!selectionOverlay.gameObject.activeSelf)
            {
                selectionOverlay.gameObject.SetActive(true);
                selectionOverlay.anchoredPosition = targetPos;
                selectionOverlay.sizeDelta = targetSize;
                selectionOverlay.localScale = Vector3.one * 0.5f;
                selectionOverlay.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                if (overlayImg != null)
                {
                    overlayImg.color = dragColor;
                    overlayImg.DOFade(1f, 0.15f).From(0f);
                }
            }
            else
            {
                selectionOverlay.DOKill();
                selectionOverlay.localScale = Vector3.one;
                selectionOverlay.DOSizeDelta(targetSize, 0.15f).SetEase(Ease.OutQuad);
                selectionOverlay.DOAnchorPos(targetPos, 0.15f).SetEase(Ease.OutQuad);
                if (overlayImg != null)
                    overlayImg.DOColor(dragColor, 0.15f);
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

            ClearPlacedOverlays();
            HideSolutionOverlays();
            HideHintOverlays();

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
            ShikakuGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}
