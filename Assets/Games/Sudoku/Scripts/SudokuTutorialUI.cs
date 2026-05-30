using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.Sudoku
{
    public class SudokuTutorialUI : TutorialBase
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private GameObject boxPrefab;
        [SerializeField] private SudokuCellItem cellPrefab;

        [Header("UI")]
        [SerializeField] private Button[] numberButtons;

        [Header("Colors")]
        [SerializeField] private Color selectedColor = new Color(0.7f, 0.85f, 1f);
        [SerializeField] private Color normalCellColor = Color.white;

        [Header("Layout")]
        [SerializeField] private Vector2 cellSize = new Vector2(60, 60);

        private SudokuGridController tutorialController;
        private SudokuCellItem[,] cells;
        private const string GameId = "sudoku";

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                SudokuGame.TutorialDeferred = true;
        }

        protected override void Start()
        {
            base.Start();

            if (SudokuGame.Instance != null && SudokuGame.TutorialDeferred)
                Begin();
        }

        public override void Begin()
        {
            if (isRunning) return;
            isRunning = true;
            panel.SetActive(true);
            Show();
            SetTotalSteps(3);
            StartSingleBoxPhase();
        }

        private void StartSingleBoxPhase()
        {
            CleanupPhase();

            int size = 3, boxSize = 3;
            int[,] solution = new int[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
            var localCells = new SudokuCell[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    localCells[r, c] = new SudokuCell(r, c);
            localCells[0, 0] = new SudokuCell(0, 0) { Value = 1, IsGiven = true };
            localCells[2, 2] = new SudokuCell(2, 2) { Value = 9, IsGiven = true };

            tutorialController = new SudokuGridController(new SudokuPuzzle(size, size, boxSize, localCells, solution, maxNumber: 9));
            tutorialController.OnCellChanged += OnPhaseCellChanged;
            SetupNumberButtons();
            BuildGrid();
            SetCurrentStep(0);
            ShowInstruction("Tap an empty cell, then tap a number. This is a single box \u2014 each number 1\u20139 appears once.");
        }

        private void StartColumnPhase()
        {
            CleanupPhase();

            int boxSize = 3;
            int[,] solution = new int[6, 3]
            {
                { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 },
                { 5, 3, 7 }, { 6, 1, 8 }, { 9, 4, 2 }
            };
            int rows = solution.GetLength(0), cols = solution.GetLength(1);
            var localCells = new SudokuCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    localCells[r, c] = new SudokuCell(r, c);
            localCells[0, 0] = new SudokuCell(0, 0) { Value = 1, IsGiven = true };
            localCells[1, 0] = new SudokuCell(1, 0) { Value = 4, IsGiven = true };
            localCells[3, 0] = new SudokuCell(3, 0) { Value = 5, IsGiven = true };
            localCells[4, 0] = new SudokuCell(4, 0) { Value = 6, IsGiven = true };
            localCells[0, 1] = new SudokuCell(0, 1) { Value = 2, IsGiven = true };
            localCells[0, 2] = new SudokuCell(0, 2) { Value = 3, IsGiven = true };

            tutorialController = new SudokuGridController(new SudokuPuzzle(rows, cols, boxSize, localCells, solution, maxNumber: 9));
            tutorialController.OnCellChanged += OnPhaseCellChanged;
            SetupNumberButtons();
            BuildGrid();
            SetCurrentStep(1);
            ShowInstruction("Two boxes stacked \u2014 numbers must be unique in each column across both boxes. Column 0 already has 1,4,5,6.");
        }

        private void StartRowPhase()
        {
            CleanupPhase();

            int boxSize = 3;
            int[,] solution = new int[3, 6]
            {
                { 1, 2, 3, 4, 5, 6 },
                { 4, 5, 6, 7, 8, 9 },
                { 7, 8, 9, 1, 2, 3 }
            };
            int rows = solution.GetLength(0), cols = solution.GetLength(1);
            var localCells = new SudokuCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    localCells[r, c] = new SudokuCell(r, c);
            localCells[0, 0] = new SudokuCell(0, 0) { Value = 1, IsGiven = true };
            localCells[0, 1] = new SudokuCell(0, 1) { Value = 2, IsGiven = true };
            localCells[0, 3] = new SudokuCell(0, 3) { Value = 4, IsGiven = true };
            localCells[1, 3] = new SudokuCell(1, 3) { Value = 7, IsGiven = true };
            localCells[1, 4] = new SudokuCell(1, 4) { Value = 8, IsGiven = true };
            localCells[2, 0] = new SudokuCell(2, 0) { Value = 7, IsGiven = true };
            localCells[2, 1] = new SudokuCell(2, 1) { Value = 8, IsGiven = true };

            tutorialController = new SudokuGridController(new SudokuPuzzle(rows, cols, boxSize, localCells, solution, maxNumber: 9));
            tutorialController.OnCellChanged += OnPhaseCellChanged;
            SetupNumberButtons();
            BuildGrid();
            SetCurrentStep(2);
            ShowInstruction("Two boxes side by side \u2014 numbers must be unique in each row across both boxes. Row 0 already has 1,2,4.");
        }

        private bool IsValidCompletion()
        {
            var puzzle = tutorialController.Puzzle;
            int rows = puzzle.GridRows;
            int cols = puzzle.GridCols;
            int boxSize = puzzle.BoxSize;

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (puzzle.Cells[r, c].Value == 0)
                        return false;

            for (int r = 0; r < rows; r++)
            {
                var seen = new System.Collections.Generic.HashSet<int>();
                for (int c = 0; c < cols; c++)
                    if (!seen.Add(puzzle.Cells[r, c].Value))
                        return false;
            }

            for (int c = 0; c < cols; c++)
            {
                var seen = new System.Collections.Generic.HashSet<int>();
                for (int r = 0; r < rows; r++)
                    if (!seen.Add(puzzle.Cells[r, c].Value))
                        return false;
            }

            int boxCols = cols / boxSize;
            int boxRows = rows / boxSize;
            for (int br = 0; br < boxRows; br++)
                for (int bc = 0; bc < boxCols; bc++)
                {
                    var seen = new System.Collections.Generic.HashSet<int>();
                    for (int r = 0; r < boxSize; r++)
                        for (int c = 0; c < boxSize; c++)
                            if (!seen.Add(puzzle.Cells[br * boxSize + r, bc * boxSize + c].Value))
                                return false;
                }

            return true;
        }

        private void OnPhaseCellChanged()
        {
            bool valid = IsValidCompletion();
            if (!valid) return;
            tutorialController.OnCellChanged -= OnPhaseCellChanged;

            int rows = tutorialController.Puzzle.GridRows;
            int cols = tutorialController.Puzzle.GridCols;
            bool isBox = rows == 3 && cols == 3;
            bool isColumn = rows > cols;

            string msg = isBox ? "Great! Now let's look at columns."
                : isColumn ? "Perfect! Now let's look at rows."
                : "Tutorial complete!";
            bool isLast = !isBox && !isColumn;
            System.Action next = isBox ? StartColumnPhase : isColumn ? StartRowPhase : Finish;
            ShowTemporary(msg, 1.2f, next);
        }

        private (int r, int c)? FindConflict(int row, int col, int num)
        {
            var puzzle = tutorialController.Puzzle;
            for (int c = 0; c < puzzle.GridCols; c++)
                if (c != col && puzzle.Cells[row, c].Value == num)
                    return (row, c);
            for (int r = 0; r < puzzle.GridRows; r++)
                if (r != row && puzzle.Cells[r, col].Value == num)
                    return (r, col);
            int boxR = row / puzzle.BoxSize * puzzle.BoxSize;
            int boxC = col / puzzle.BoxSize * puzzle.BoxSize;
            for (int r = boxR; r < boxR + puzzle.BoxSize; r++)
                for (int c = boxC; c < boxC + puzzle.BoxSize; c++)
                    if ((r != row || c != col) && puzzle.Cells[r, c].Value == num)
                        return (r, c);
            return null;
        }

        private void SetupNumberButtons()
        {
            int maxNumber = tutorialController.Puzzle.MaxNumber;

            for (int i = 0; i < numberButtons.Length; i++)
                numberButtons[i].onClick.RemoveAllListeners();

            numberButtons[0].GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "";
            numberButtons[0].onClick.AddListener(() =>
            {
                int selR = tutorialController.SelectedRow;
                int selC = tutorialController.SelectedCol;
                if (selR < 0 || selC < 0) return;
                tutorialController.EnterNumber(0);
                RefreshVisuals();
            });

            int numberCount = Mathf.Min(numberButtons.Length - 1, maxNumber);
            for (int i = 0; i < numberCount; i++)
            {
                int numV = i + 1;
                var btn = numberButtons[i + 1];
                var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.text = numV.ToString();
                int captured = numV;
                btn.onClick.AddListener(() =>
                {
                    int selR = tutorialController.SelectedRow;
                    int selC = tutorialController.SelectedCol;
                    if (selR < 0 || selC < 0) return;
                    var conflict = FindConflict(selR, selC, captured);
                    if (conflict.HasValue)
                    {
                        cells[selR, selC]?.FlashText();
                        cells[conflict.Value.r, conflict.Value.c]?.FlashText();
                    }
                    else
                    {
                        tutorialController.EnterNumber(captured);
                        RefreshVisuals();
                    }
                });
            }
        }

        private void BuildGrid()
        {
            int rows = tutorialController.Puzzle.GridRows;
            int cols = tutorialController.Puzzle.GridCols;
            int boxSize = tutorialController.Puzzle.BoxSize;
            int boxCols = cols / boxSize;
            int boxRows = rows / boxSize;

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = boxCols;
            gridLayout.cellSize = cellSize;

            cells = new SudokuCellItem[rows, cols];

            for (int br = 0; br < boxRows; br++)
            {
                for (int bc = 0; bc < boxCols; bc++)
                {
                    var boxObj = Instantiate(boxPrefab, gridLayout.transform);
                    boxObj.name = $"Box_{br}_{bc}";
                    var boxLayout = boxObj.GetComponent<GridLayoutGroup>();
                    boxLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    boxLayout.constraintCount = boxSize;

                    for (int r = 0; r < boxSize; r++)
                    {
                        for (int c = 0; c < boxSize; c++)
                        {
                            int globalR = br * boxSize + r;
                            int globalC = bc * boxSize + c;
                            if (globalR >= rows || globalC >= cols) continue;

                            var cell = Instantiate(cellPrefab, boxLayout.transform);
                            var cellData = tutorialController.Puzzle.Cells[globalR, globalC];
                            cell.Init(globalR, globalC, cellData.IsGiven);
                            cell.SetValue(cellData.Value, cellData.IsGiven);
                            cell.transform.localScale = Vector3.zero;
                            cell.transform.DOScale(Vector3.one, 0.25f).SetDelay((globalR * cols + globalC) * 0.01f).SetEase(Ease.OutBack);
                            cell.OnClickEvent += (row, col) =>
                            {
                                tutorialController.SelectCell(row, col);
                                cells?[row, col]?.StopViolationAnimation();
                                RefreshVisuals();
                            };
                            cells[globalR, globalC] = cell;
                        }
                    }
                }
            }

            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (cells == null) return;
            int rows = tutorialController.Puzzle.GridRows;
            int cols = tutorialController.Puzzle.GridCols;
            int selR = tutorialController.SelectedRow;
            int selC = tutorialController.SelectedCol;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = cells[r, c];
                    if (cell == null) continue;

                    var cellData = tutorialController.Puzzle.Cells[r, c];
                    cell.SetValue(cellData.Value, cellData.IsGiven);
                    cell.Image.color = r == selR && c == selC
                        ? selectedColor
                        : normalCellColor;
                }
            }
        }

        public override void Finish()
        {
            isRunning = false;
            CleanupPhase();
            FadeOutPanel(panel, 0.4f, () =>
            {
                new TutorialService().MarkCompleted(GameId);
                if (!isReplay)
                    SudokuGame.Instance.StartGame();
                isReplay = false;
            });
        }

        private void CleanupPhase()
        {
            if (tutorialController != null)
            {
                tutorialController.OnCellChanged -= OnPhaseCellChanged;
                tutorialController.Dispose();
                tutorialController = null;
            }
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
            for (int i = 0; i < numberButtons.Length; i++)
                numberButtons[i].onClick.RemoveAllListeners();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupPhase();
        }
    }
}
