using System.Collections.Generic;
using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.Shikaku
{
    public class ShikakuTutorialUI : TutorialBase
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private ShikakuCellItem cellPrefab;
        [SerializeField] private RectTransform selectionOverlay;
        [Header("Appearance")]
        [SerializeField] private Vector2 cellSize = new Vector2(100, 110);
        [SerializeField] private Color cellColor = Color.white;

        private ShikakuGridController tutorialController;
        private ShikakuCellItem[,] cells;
        private readonly List<RectTransform> placedOverlays = new List<RectTransform>();
        private readonly List<int> removedBuffer = new List<int>();
        private Color currentDragColor;

        private const string GameId = "shikaku";

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                ShikakuGame.TutorialDeferred = true;
        }

        protected override void Start()
        {
            base.Start();
            if (ShikakuGame.TutorialDeferred)
                StartCoroutine(BeginWhenReady(() => ShikakuGame.Instance != null));
        }

        public override void Begin()
        {
            if (isRunning) return;
            isRunning = true;
            panel.SetActive(true);
            Show();
            SetTotalSteps(3);
            StartDragPhase();
        }

        private void StartDragPhase()
        {
            CleanupPhase();

            tutorialController = new ShikakuGridController(CreateDragPuzzle());
            tutorialController.OnRectPlaced += OnDragRectPlaced;
            BuildGrid();
            SetCurrentStep(0);
            ShowInstruction("Drag from one cell to another to create a rectangle.\n\nRelease to place it.");
        }

        private static ShikakuPuzzle CreateDragPuzzle()
        {
            int rows = 2, cols = 1;
            var cells = new ShikakuCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cells[r, c] = new ShikakuCell(r, c);
            cells[0, 0] = new ShikakuCell(0, 0) { Number = 2, AssignedRectId = 0, IsPlayable = true };

            var solution = new List<ShikakuRect>
            {
                new ShikakuRect { Id = 0, Row = 0, Col = 0, Height = 2, Width = 1, Number = 2 }
            };

            return new ShikakuPuzzle(cells, solution.AsReadOnly());
        }

        private void OnDragRectPlaced()
        {
            tutorialController.OnRectPlaced -= OnDragRectPlaced;
            ShowInstruction("Good! You placed a rectangle.");
            DOVirtual.DelayedCall(1.2f, StartNumberPhase);
        }

        private void StartNumberPhase()
        {
            CleanupPhase();

            tutorialController = new ShikakuGridController(CreateNumberPuzzle());
            tutorialController.OnRectPlaced += OnNumberRectPlaced;
            BuildGrid();
            SetCurrentStep(1);
            ShowInstruction("The number tells you the rectangle's area.\n\nA '4' means the rectangle must have exactly 4 cells.\n\nDrag a 4-cell rectangle covering the '4'.");
        }

        private static ShikakuPuzzle CreateNumberPuzzle()
        {
            int size = 2;
            var cells = new ShikakuCell[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c] = new ShikakuCell(r, c);
            cells[0, 0] = new ShikakuCell(0, 0) { Number = 4, AssignedRectId = 0, IsPlayable = true };

            var solution = new List<ShikakuRect>
            {
                new ShikakuRect { Id = 0, Row = 0, Col = 0, Height = 2, Width = 2, Number = 4 }
            };

            return new ShikakuPuzzle(cells, solution.AsReadOnly());
        }

        private void OnNumberRectPlaced()
        {
            tutorialController.OnRectPlaced -= OnNumberRectPlaced;
            ShowInstruction("The rectangle contains the number and its area matches!");
            DOVirtual.DelayedCall(1.5f, StartFillPhase);
        }

        private void StartFillPhase()
        {
            CleanupPhase();

            tutorialController = new ShikakuGridController(CreateFillPuzzle());
            tutorialController.OnSolved += OnFillSolved;
            BuildGrid();
            SetCurrentStep(2);
            ShowInstruction("Fill the entire grid.\n\nEach cell belongs to exactly one rectangle.\n\nThe number is inside its rectangle — not necessarily at a corner.");
        }

        private static ShikakuPuzzle CreateFillPuzzle()
        {
            int rows = 3, cols = 4;
            var cells = new ShikakuCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cells[r, c] = new ShikakuCell(r, c);
            cells[1, 1] = new ShikakuCell(1, 1) { Number = 6, AssignedRectId = 0, IsPlayable = true };
            cells[2, 2] = new ShikakuCell(2, 2) { Number = 3, AssignedRectId = 1, IsPlayable = true };
            cells[1, 3] = new ShikakuCell(1, 3) { Number = 3, AssignedRectId = 2, IsPlayable = true };

            var solution = new List<ShikakuRect>
            {
                new ShikakuRect { Id = 0, Row = 0, Col = 0, Height = 3, Width = 2, Number = 6 },
                new ShikakuRect { Id = 1, Row = 0, Col = 2, Height = 3, Width = 1, Number = 3 },
                new ShikakuRect { Id = 2, Row = 0, Col = 3, Height = 3, Width = 1, Number = 3 },
            };

            return new ShikakuPuzzle(cells, solution.AsReadOnly());
        }

        private void OnFillSolved()
        {
            tutorialController.OnSolved -= OnFillSolved;
            ShowTemporary("Tutorial complete!", 1.5f, Finish);
        }

        private void BuildGrid()
        {
            int rows = tutorialController.Puzzle.Rows;
            int cols = tutorialController.Puzzle.Cols;

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = cols;
            gridLayout.cellSize = cellSize;

            var spacing = Mathf.RoundToInt(Mathf.Min(cellSize.x, cellSize.y) * 0.06f);
            gridLayout.spacing = new Vector2(spacing, spacing);

            if (selectionOverlay != null)
                selectionOverlay.gameObject.SetActive(false);

            cells = new ShikakuCellItem[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Init(r, c, tutorialController.Puzzle.Cells[r, c].Number);
                    cell.Image.color = cellColor;
                    cell.transform.localScale = Vector3.zero;
                    cell.transform.DOScale(Vector3.one, 0.25f).SetDelay((r * cols + c) * 0.02f).SetEase(Ease.OutBack);
                    int rr = r, cc = c;
                    cell.OnPointerDownEvent += (row, col) => OnCellDown(row, col);
                    cell.OnPointerEnterEvent += (row, col) => OnCellEnter(row, col);
                    cell.OnPointerUpEvent += OnCellUp;
                    cells[r, c] = cell;
                }
            }
        }

        private void OnCellDown(int row, int col)
        {
            currentDragColor = Color.HSVToRGB(UnityEngine.Random.value, 0.28f, 0.92f);
            tutorialController.StartDrag(row, col);
            tutorialController.RemoveOverlappingDuringDrag(removedBuffer);
            for (int i = 0; i < removedBuffer.Count; i++)
                RemovePlacedOverlayAt(removedBuffer[i]);
            UpdateOverlay();
        }
        private void OnCellEnter(int row, int col)
        {
            if (!tutorialController.IsDragging) return;
            tutorialController.UpdateDrag(row, col);
            tutorialController.RemoveOverlappingDuringDrag(removedBuffer);
            for (int i = 0; i < removedBuffer.Count; i++)
                RemovePlacedOverlayAt(removedBuffer[i]);
            UpdateOverlay();
        }
        private void OnCellUp()
        {
            if (!tutorialController.IsDragging) return;
            bool placed = tutorialController.EndDrag(currentDragColor);
            if (placed)
            {
                var rects = tutorialController.Puzzle.PlayerRects;
                AddPlacedOverlay(rects[rects.Count - 1]);
            }
            HideOverlay();
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

        private void OverlayRect(int r1, int c1, int r2, int c2, out Vector2 pos, out Vector2 size)
        {
            var topLeft = cells[r1, c1].GetComponent<RectTransform>();
            var bottomRight = cells[r2, c2].GetComponent<RectTransform>();
            var parent = selectionOverlay.parent as RectTransform;
            var canvas = topLeft.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            topLeft.GetWorldCorners(corners);
            var worldTL = corners[1];
            bottomRight.GetWorldCorners(corners);
            var worldBR = corners[3];
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(cam, worldTL), cam, out var localTL);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(cam, worldBR), cam, out var localBR);
            float w = localBR.x - localTL.x;
            float h = localTL.y - localBR.y;
            pos = new Vector2(localTL.x + w * selectionOverlay.pivot.x, localTL.y - h * (1f - selectionOverlay.pivot.y));
            size = new Vector2(w, h);
        }

        private void UpdateOverlay()
        {
            if (selectionOverlay == null || !tutorialController.IsDragging) return;
            var overlayImg = selectionOverlay.GetComponent<Image>();

            int r1 = Mathf.Min(tutorialController.DragStartRow, tutorialController.DragEndRow);
            int c1 = Mathf.Min(tutorialController.DragStartCol, tutorialController.DragEndCol);
            int r2 = Mathf.Max(tutorialController.DragStartRow, tutorialController.DragEndRow);
            int c2 = Mathf.Max(tutorialController.DragStartCol, tutorialController.DragEndCol);

            var dragColor = currentDragColor;
            dragColor.a = 0.75f;

            if (!selectionOverlay.gameObject.activeSelf)
            {
                OverlayRect(r1, c1, r2, c2, out var pos, out var size);
                selectionOverlay.anchoredPosition = pos;
                selectionOverlay.sizeDelta = size;
                selectionOverlay.gameObject.SetActive(true);
                selectionOverlay.localScale = Vector3.one * 0.5f;
                selectionOverlay.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                if (overlayImg != null)
                {
                    overlayImg.color = dragColor;
                    overlayImg.DOFade(0.75f, 0.15f).From(0f);
                }
            }
            else
            {
                selectionOverlay.DOKill();
                OverlayRect(r1, c1, r2, c2, out var pos, out var size);
                selectionOverlay.DOSizeDelta(size, 0.15f).SetEase(Ease.OutQuad);
                selectionOverlay.DOAnchorPos(pos, 0.15f).SetEase(Ease.OutQuad);
                if (overlayImg != null)
                    overlayImg.DOColor(dragColor, 0.15f);
            }
        }

        private void HideOverlay()
        {
            if (selectionOverlay == null) return;
            selectionOverlay.DOKill();
            var img = selectionOverlay.GetComponent<Image>();
            img?.DOFade(0f, 0.12f);
            selectionOverlay.DOScale(Vector3.one * 0.8f, 0.12f).OnComplete(() =>
            {
            if (selectionOverlay != null)
            {
                selectionOverlay.DOKill();
                selectionOverlay.gameObject.SetActive(false);
            }
            });
        }

        public override void Finish()
        {
            isRunning = false;
            CleanupPhase();
            FadeOutPanel(panel, 0.4f, () =>
            {
                new TutorialService().MarkCompleted(GameId);
                if (!isReplay)
                    ShikakuGame.Instance.StartGame();
                isReplay = false;
            });
        }

        private void CleanupPhase()
        {
            if (tutorialController != null)
            {
                tutorialController.OnRectPlaced -= OnDragRectPlaced;
                tutorialController.OnRectPlaced -= OnNumberRectPlaced;
                tutorialController.OnSolved -= OnFillSolved;
                tutorialController.Dispose();
                tutorialController = null;
            }
            if (placedOverlays != null)
            {
                foreach (var o in placedOverlays)
                    if (o != null) Destroy(o.gameObject);
                placedOverlays.Clear();
            }
            if (selectionOverlay != null)
                selectionOverlay.gameObject.SetActive(false);
            if (cells != null)
            {
                for (int r = 0; r < cells.GetLength(0); r++)
                    for (int c = 0; c < cells.GetLength(1); c++)
                        if (cells[r, c] != null)
                            Destroy(cells[r, c].gameObject);
                cells = null;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupPhase();
        }
    }
}
