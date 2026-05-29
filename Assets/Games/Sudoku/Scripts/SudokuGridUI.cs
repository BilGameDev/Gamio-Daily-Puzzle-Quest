using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using Gamio.Core;

namespace Gamio.Games.Sudoku
{
    public class SudokuGridUI : GameUI
    {
        [Header("References")]
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private SudokuCellItem cellPrefab;
        [SerializeField] private GameObject boxPrefab;

        [Header("Colors")]
        [SerializeField] private Color selectedColor = new Color(0.7f, 0.85f, 1f);
        [SerializeField] private Color normalCellColor = Color.white;

        [Header("UI")]
        [SerializeField] private Button[] numberButtons;

        private SudokuGridController grid;
        private SudokuCellItem[,] cells;
        private int size;
        private int boxCount;
        private bool showSolution;
        private int hintRevealCount;

        private void OnEnable()
        {
            GamioEvents.OnResetRequested += ResetPuzzle;
            GamioEvents.OnHintRequested += RevealHint;
            SudokuGame.OnControllerCreated += OnControllerCreated;
            if (SudokuGame.CurrentController != null)
                Setup(SudokuGame.CurrentController);
        }

        private void OnDisable()
        {
            GamioEvents.OnResetRequested -= ResetPuzzle;
            GamioEvents.OnHintRequested -= RevealHint;
            SudokuGame.OnControllerCreated -= OnControllerCreated;
        }

        void Start()
        {
            if (launchOnStart)
            {
                TestGame(new SudokuGame());
            }
        }

        public void Setup(SudokuGridController controller)
        {
            CleanupGrid();

            grid = controller;
            size = grid.Puzzle.GridRows;
            boxCount = size / grid.Puzzle.BoxSize;
            grid.OnSolved += HandleSolved;
            grid.OnWrongNumber += HandleWrongNumber;
            grid.OnSelectionChanged += RefreshVisuals;
            grid.OnCellChanged += RefreshVisuals;

            SetupInputButtons();
            BuildGrid();
        }

        private void SetupInputButtons()
        {
            for (int i = 0; i < numberButtons.Length; i++)
                numberButtons[i].onClick.RemoveAllListeners();

            numberButtons[0].GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "";
            numberButtons[0].onClick.AddListener(() => grid?.EnterNumber(0));

            int count = Mathf.Min(numberButtons.Length - 1, size);
            for (int i = 0; i < count; i++)
            {
                int num = i + 1;
                var btn = numberButtons[i + 1];
                var label = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null) label.text = num.ToString();
                btn.onClick.AddListener(() => grid?.EnterNumber(num));
            }
        }

        private void BuildGrid()
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = boxCount;
            cells = new SudokuCellItem[size, size];

            for (int br = 0; br < boxCount; br++)
            {
                for (int bc = 0; bc < boxCount; bc++)
                {
                    var boxObj = Instantiate(boxPrefab, gridLayout.transform);
                    boxObj.name = $"Box_{br}_{bc}";
                    var boxLayout = boxObj.GetComponent<GridLayoutGroup>();
                    boxLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    boxLayout.constraintCount = grid.Puzzle.BoxSize;

                    for (int r = 0; r < grid.Puzzle.BoxSize; r++)
                    {
                        for (int c = 0; c < grid.Puzzle.BoxSize; c++)
                        {
                            int globalR = br * grid.Puzzle.BoxSize + r;
                            int globalC = bc * grid.Puzzle.BoxSize + c;

                            var cell = Instantiate(cellPrefab, boxLayout.transform);
                            var cellData = grid.Puzzle.Cells[globalR, globalC];
                            cell.Init(globalR, globalC, cellData.IsGiven);
                            cell.SetValue(cellData.Value, cellData.IsGiven);
                            cell.transform.localScale = Vector3.zero;
                            float delay = (globalR * size + globalC) * 0.015f;
                            cell.transform.DOScale(Vector3.one, 0.25f).SetDelay(delay).SetEase(Ease.OutBack);
                            cell.OnClickEvent += (row, col) => OnCellClicked(row, col);
                            cells[globalR, globalC] = cell;
                        }
                    }
                }
            }

            RefreshVisuals();
        }

        private void OnCellClicked(int row, int col)
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Selection);
            grid?.SelectCell(row, col);
            cells?[row, col]?.StopViolationAnimation();
        }

        private void HandleSolved()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            RefreshVisuals();
            float delay = 0.3f;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (cells[r, c] != null)
                    {
                        cells[r, c].transform.DOPunchScale(Vector3.one * 0.08f, 0.4f, 2, 0.3f)
                            .SetDelay(delay).SetEase(Ease.OutQuad);
                    }
                    delay += 0.02f;
                }
            }
        }

        private void HandleWrongNumber(int row, int col)
        {
            if (cells != null && row >= 0 && row < size && col >= 0 && col < size)
                cells[row, col].PlayViolationAnimation();
        }

        private void Update()
        {
            if (grid == null) return;
            if (Keyboard.current.hKey.wasPressedThisFrame)
                RevealHint();
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                showSolution = !showSolution;
                RefreshVisuals();
            }
        }

        private void OnControllerCreated(SudokuGridController controller)
        {
            Setup(controller);
        }

        public void ResetPuzzle()
        {
            grid?.ResetPuzzle();
            RefreshVisuals();
        }

        private void RevealHint()
        {
            showSolution = false;
            hintRevealCount++;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (cells == null) return;

            int selR = grid.SelectedRow;
            int selC = grid.SelectedCol;

            int hintCount = 0;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    if (hintRevealCount > 0 && !grid.Puzzle.Cells[r, c].IsGiven &&
                        grid.Puzzle.Cells[r, c].Value != grid.Puzzle.Solution[r, c])
                        hintCount++;
                }

            int hintIdx = 0;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = cells[r, c];
                    if (cell == null) continue;

                    var cellData = grid.Puzzle.Cells[r, c];
                    bool showHint = hintRevealCount > 0 && !cellData.IsGiven &&
                        cellData.Value != grid.Puzzle.Solution[r, c] && hintIdx++ < hintRevealCount;

                    if (showHint)
                    {
                        cell.SetValue(grid.Puzzle.Solution[r, c], false);
                        cell.Image.color = new Color(0.3f, 0.8f, 0.3f);
                    }
                    else if (showSolution && !cellData.IsGiven)
                    {
                        cell.SetValue(grid.Puzzle.Solution[r, c], false);
                        cell.Image.color = r == selR && c == selC ? selectedColor : normalCellColor;
                    }
                    else
                    {
                        cell.SetValue(cellData.Value, cellData.IsGiven);
                        cell.Image.color = r == selR && c == selC ? selectedColor : normalCellColor;
                    }
                }
            }
        }

        private void CleanupGrid()
        {
            if (cells != null)
            {
                for (int r = 0; r < cells.GetLength(0); r++)
                    for (int c = 0; c < cells.GetLength(1); c++)
                        cells[r, c] = null;
                cells = null;
            }

            if (gridLayout != null)
                for (int i = gridLayout.transform.childCount - 1; i >= 0; i--)
                    Destroy(gridLayout.transform.GetChild(i).gameObject);

            if (grid != null)
            {
                grid.OnSolved -= HandleSolved;
                grid.OnWrongNumber -= HandleWrongNumber;
                grid.OnSelectionChanged -= RefreshVisuals;
                grid.OnCellChanged -= RefreshVisuals;
                grid = null;
            }

            for (int i = 0; i < numberButtons.Length; i++)
                if (numberButtons[i] != null) numberButtons[i].onClick.RemoveAllListeners();

            showSolution = false;
            hintRevealCount = 0;
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            CleanupGrid();
            SudokuGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}
