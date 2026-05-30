using System.Collections.Generic;
using DG.Tweening;
using Gamio.Core;
using Gamio.Features;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Gamio.Games.WordSearch
{
    public class WordSearchGridUI : GameUI
    {
        [Header("References")]
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private WordSearchCellItem cellPrefab;
        [SerializeField] private RectTransform wordListContainer;
        [SerializeField] private TextMeshProUGUI wordListItemPrefab;
        [Header("Colors")]
        [SerializeField] private Color cellColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.3f, 0.8f, 1f, 0.6f);
        [SerializeField] private Color foundColor = new Color(0.6f, 0.9f, 0.6f, 0.5f);

        private WordSearchGridController grid;
        private WordSearchCellItem[,] cells;
        private int size;
        private List<TextMeshProUGUI> wordLabels;
        private bool showSolution;
        private int hintRevealCount;

        public event System.Action OnSolved;

        protected override void OnEnable()
        {
            base.OnEnable();
            WordSearchGame.OnControllerCreated += OnControllerCreated;
            if (WordSearchGame.CurrentController != null)
                Setup(WordSearchGame.CurrentController);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            WordSearchGame.OnControllerCreated -= OnControllerCreated;
        }

        protected override void Start()
        {
            base.Start();

            if (launchOnStart)
            {
                LaunchGame(new WordSearchGame());
            }
        }

        public void Setup(WordSearchGridController controller)
        {
            CleanupGrid();

            grid = controller;
            size = grid.Puzzle.GridSize;
            grid.OnSolved += HandleSolved;
            grid.OnWordFound += HandleWordFound;
            BuildGrid();
            BuildWordList();
        }

        private void BuildGrid()
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = size;
            UpdateCellSize();

            cells = new WordSearchCellItem[size, size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Init(r, c, grid.Puzzle.Cells[r, c].Letter);
                    cell.SetTileColor(cellColor);
                    cell.transform.localScale = Vector3.zero;
                    float delay = (r * size + c) * 0.015f;
                    cell.transform.DOScale(Vector3.one, 0.25f).SetDelay(delay).SetEase(Ease.OutBack);
                    cell.OnPointerDownEvent += (row, col) => OnCellDown(row, col);
                    cell.OnPointerEnterEvent += (row, col) => OnCellEnter(row, col);
                    cell.OnPointerUpEvent += OnCellUp;
                    cells[r, c] = cell;
                }
            }
        }

        private void BuildWordList()
        {
            wordLabels = new List<TextMeshProUGUI>();
            foreach (var word in grid.Puzzle.Placements)
            {
                var label = Instantiate(wordListItemPrefab, wordListContainer);
                label.text = word.Word.ToUpperInvariant();
                label.gameObject.SetActive(true);
                wordLabels.Add(label);
            }
        }

        private void OnCellDown(int row, int col)
        {
            HapticsHelper.PlaySoftImpact();
            HapticsHelper.PlayConstant(0.1f, 0.1f, 10f);
            grid.StartDrag(row, col);
            RefreshHighlights();
        }

        private void OnCellEnter(int row, int col)
        {
            if (!grid.IsDragging) return;
            grid.UpdateDrag(row, col);
            float dr = Mathf.Abs(grid.DragEndRow - grid.DragStartRow);
            float dc = Mathf.Abs(grid.DragEndCol - grid.DragStartCol);
            float t = Mathf.Clamp01(Mathf.Max(dr, dc) / size);
            HapticsHelper.UpdateContinuous(0.1f + t * 0.1f, t * 0.35f);
            RefreshHighlights();
        }

        private void OnCellUp()
        {
            if (!grid.IsDragging) return;
            HapticsHelper.StopContinuous();
            grid.EndDrag();
            RefreshHighlights();
        }

        private void HandleWordFound(string word)
        {
            HapticsHelper.PlayEmphasis(0.6f, 0.7f);
            for (int i = 0; i < grid.Puzzle.Placements.Count; i++)
            {
                if (grid.Puzzle.Placements[i].Word == word)
                {
                    var label = wordLabels[i];
                    label.fontStyle = FontStyles.Strikethrough;
                    label.DOColor(new Color(0.5f, 0.5f, 0.5f), 0.3f);
                    break;
                }
            }

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    if (grid.Puzzle.IsCellFound(r, c))
                        cells[r, c].SetFound(foundColor);
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
                    var cell = cells[r, c];
                    cell.transform.DOKill();
                    cell.transform.localScale = Vector3.one;
                    cell.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 4, 0.5f)
                        .SetDelay(delay).SetEase(Ease.OutQuad)
                        .OnPlay(() => HapticsHelper.PlayEmphasis(0.15f + (r + c) % 3 * 0.1f, 0.3f));
                    delay += 0.02f;
                }
        }

        private void OnControllerCreated(WordSearchGridController controller)
        {
            Setup(controller);
        }

        private void RefreshHighlights()
        {
            var hintWordIds = new HashSet<int>();
            if (hintRevealCount > 0)
            {
                for (int i = 0; i < grid.Puzzle.Placements.Count && hintWordIds.Count < hintRevealCount; i++)
                {
                    if (!grid.Puzzle.IsWordFound(grid.Puzzle.Placements[i].Word))
                        hintWordIds.Add(i);
                }
            }

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int idx = grid.Puzzle.Cells[r, c].WordIndex;
                    bool isHintCell = idx >= 0 && hintWordIds.Contains(idx);

                    if (isHintCell)
                    {
                        cells[r, c].SetHighlight(true, new Color(1f, 0.8f, 0.2f, 0.5f));
                    }
                    else if (showSolution)
                    {
                        bool inPlacement = idx >= 0;
                        cells[r, c].SetHighlight(true, new Color(0.3f, 1f, 0.3f, 0.4f));
                    }
                    else if (grid.Puzzle.IsCellFound(r, c))
                    {
                        cells[r, c].SetFound(foundColor);
                    }
                    else if (grid.IsDragging && IsCellInDragPath(r, c))
                    {
                        bool valid = IsValidDragLine();
                        cells[r, c].SetHighlight(true, valid ? highlightColor : new Color(1f, 0.3f, 0.3f, 0.5f));
                    }
                    else
                    {
                        cells[r, c].SetHighlight(false, cellColor);
                    }
                }
            }
        }

        private bool IsCellInDragPath(int row, int col)
        {
            int minR = Mathf.Min(grid.DragStartRow, grid.DragEndRow);
            int maxR = Mathf.Max(grid.DragStartRow, grid.DragEndRow);
            int minC = Mathf.Min(grid.DragStartCol, grid.DragEndCol);
            int maxC = Mathf.Max(grid.DragStartCol, grid.DragEndCol);

            int dr = grid.DragEndRow - grid.DragStartRow;
            int dc = grid.DragEndCol - grid.DragStartCol;

            if (dr == 0 && dc == 0)
                return row == grid.DragStartRow && col == grid.DragStartCol;
            if (dr == 0)
                return row == grid.DragStartRow && col >= minC && col <= maxC;
            if (dc == 0)
                return col == grid.DragStartCol && row >= minR && row <= maxR;
            if (Mathf.Abs(dr) == Mathf.Abs(dc))
            {
                int drn = dr / Mathf.Abs(dr);
                int dcn = dc / Mathf.Abs(dc);
                int r = grid.DragStartRow;
                int c = grid.DragStartCol;
                int len = Mathf.Abs(dr);
                for (int i = 0; i <= len; i++)
                {
                    if (r == row && c == col) return true;
                    r += drn;
                    c += dcn;
                }
            }
            return false;
        }

        private bool IsValidDragLine()
        {
            int dr = grid.DragEndRow - grid.DragStartRow;
            int dc = grid.DragEndCol - grid.DragStartCol;
            return dr == 0 || dc == 0 || Mathf.Abs(dr) == Mathf.Abs(dc);
        }

        private void UpdateCellSize()
        {
            var target = WordSearchGame.ActiveSettings.GetConfig(WordSearchGame.CurrentDifficulty).cellSize;
            gridLayout.cellSize = new Vector2(Mathf.Max(0, target.x), Mathf.Max(0, target.y));
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

            if (wordLabels != null)
            {
                foreach (var label in wordLabels)
                    if (label != null) Destroy(label.gameObject);
                wordLabels = null;
            }

            if (grid != null)
            {
                grid.OnSolved -= HandleSolved;
                grid.OnWordFound -= HandleWordFound;
                grid = null;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            CleanupGrid();
            WordSearchGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}
