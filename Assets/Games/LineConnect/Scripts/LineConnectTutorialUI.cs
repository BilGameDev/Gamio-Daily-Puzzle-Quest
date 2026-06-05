using System.Collections.Generic;
using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.LineConnect
{
    public class LineConnectTutorialUI : TutorialBase
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private LineConnectCellItem cellPrefab;
        [SerializeField] private LineConnectCellItem endpointPrefab;
        [Header("Layout")]
        [SerializeField] private Vector2 cellSize = new Vector2(100, 110);

        private LineConnectGridController tutorialController;
        private LineConnectCellItem[,] cells;

        private const string GameId = "lineconnect";

        private static readonly Color[] palette = new[]
        {
            new Color(0.3f, 0.6f, 0.9f),
            new Color(0.9f, 0.4f, 0.3f)
        };

        private static readonly Color emptyColor = new Color(0.15f, 0.15f, 0.17f);

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                LineConnectGame.TutorialDeferred = true;
        }

        protected override void Start()
        {
            base.Start();
            if (LineConnectGame.TutorialDeferred)
                StartCoroutine(BeginWhenReady(() => LineConnectGame.Instance != null));
        }

        public override void Begin()
        {
            if (isRunning) return;
            isRunning = true;
            panel.SetActive(true);
            Show();
            SetTotalSteps(3);
            StartLinePhase();
        }

        private void StartLinePhase()
        {
            CleanupPhase();

            tutorialController = new LineConnectGridController(CreateLinePuzzle());
            tutorialController.OnVisualsChanged += RefreshAll;
            tutorialController.OnVisualsChanged += OnLinePhaseChanged;
            BuildGrid();
            SetCurrentStep(0);
            ShowInstruction("Drag from one colored tile to the matching tile to connect");
        }

        private void OnLinePhaseChanged()
        {
            if (tutorialController.IsDragging) return;
            if (tutorialController.Puzzle.GetPathId(0, 3) < 0) return;

            tutorialController.OnVisualsChanged -= OnLinePhaseChanged;
            DOVirtual.DelayedCall(1.2f, StartSnakePhase);
        }

        private void StartSnakePhase()
        {
            CleanupPhase();

            tutorialController = new LineConnectGridController(CreateSnakePuzzle());
            tutorialController.OnVisualsChanged += RefreshAll;
            tutorialController.OnVisualsChanged += OnSnakePhaseChanged;
            BuildGrid();
            SetCurrentStep(1);
            ShowInstruction("Every empty cell must be filled — snake through the grid and connect");
        }

        private void OnSnakePhaseChanged()
        {
            if (tutorialController.IsDragging) return;
            if (!tutorialController.Puzzle.IsSolved()) return;

            tutorialController.OnVisualsChanged -= OnSnakePhaseChanged;
            ShowTemporary("All cells filled! Now connect two pairs.", 1.2f, StartFillPhase);
        }

        private void StartFillPhase()
        {
            CleanupPhase();

            tutorialController = new LineConnectGridController(CreateFillPuzzle());
            tutorialController.OnVisualsChanged += RefreshAll;
            tutorialController.OnVisualsChanged += OnFillPhaseChanged;
            BuildGrid();
            SetCurrentStep(2);
            ShowInstruction("Connect both pairs without overlapping paths.");
        }

        private void OnFillPhaseChanged()
        {
            if (tutorialController.IsDragging) return;
            if (!tutorialController.Puzzle.IsSolved()) return;

            tutorialController.OnVisualsChanged -= OnFillPhaseChanged;
            ShowTemporary("You're ready!", 1.5f, Finish);
        }

        public override void Finish()
        {
            isRunning = false;
            CleanupPhase();
            FadeOutPanel(panel, 0.4f, () =>
            {
                new TutorialService().MarkCompleted(GameId);
                if (!isReplay)
                    LineConnectGame.Instance.StartGame();
                isReplay = false;
            });
        }

        private void BuildGrid()
        {
            int rows = tutorialController.Puzzle.Cells.GetLength(0);
            int cols = tutorialController.Puzzle.Cells.GetLength(1);

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = cols;
            gridLayout.cellSize = cellSize;

            var spacing = Mathf.RoundToInt(Mathf.Min(cellSize.x, cellSize.y) * 0.06f);
            gridLayout.spacing = new Vector2(spacing, spacing);

            cells = new LineConnectCellItem[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cellData = tutorialController.Puzzle.Cells[r, c];
                    var prefab = cellData.IsEndpoint && endpointPrefab != null ? endpointPrefab : cellPrefab;
                    var cell = Instantiate(prefab, gridLayout.transform);
                    cell.Init(r, c, cellData.ColorId, cellData.IsEndpoint);
                    cell.OnPointerDownEvent += OnCellDown;
                    cell.OnPointerEnterEvent += OnCellEnter;
                    cell.OnPointerUpEvent += OnCellUp;
                    cells[r, c] = cell;
                }
            }

            RefreshAll();
        }

        private void OnCellDown(int row, int col)
        {
            if (tutorialController != null)
                tutorialController.StartDrag(row, col);
        }

        private void OnCellEnter(int row, int col)
        {
            if (tutorialController != null && tutorialController.IsDragging)
                tutorialController.UpdateDrag(row, col);
        }

        private void OnCellUp()
        {
            if (tutorialController != null)
                tutorialController.EndDrag();
        }

        private void RefreshAll()
        {
            var puzzle = tutorialController.Puzzle;

            int rows = cells.GetLength(0);
            int cols = cells.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int pathId = puzzle.GetPathId(r, c);
                    var cellData = puzzle.Cells[r, c];

                    if (pathId >= 0)
                    {
                        cells[r, c].SetColor(puzzle.ColorPalette[pathId % puzzle.ColorPalette.Length]);
                    }
                    else if (cellData.IsEndpoint)
                    {
                        cells[r, c].SetColor(puzzle.ColorPalette[cellData.ColorId % puzzle.ColorPalette.Length]);
                    }
                    else
                    {
                        cells[r, c].SetColor(emptyColor);
                    }
                }
            }

            if (tutorialController.IsDragging)
            {
                var activeColor = puzzle.ColorPalette[tutorialController.ActiveColorId % puzzle.ColorPalette.Length];
                foreach (var (r, c) in tutorialController.ActivePath)
                {
                    var cd = puzzle.Cells[r, c];
                    Color ac = activeColor;
                    ac.a = cd.IsEndpoint ? 0.8f : 0.55f;
                    cells[r, c].SetColor(ac);
                }
            }
        }

        private void CleanupPhase()
        {
            if (tutorialController != null)
            {
                tutorialController.OnVisualsChanged -= RefreshAll;
                tutorialController.OnVisualsChanged -= OnLinePhaseChanged;
                tutorialController.OnVisualsChanged -= OnSnakePhaseChanged;
                tutorialController.OnVisualsChanged -= OnFillPhaseChanged;
                tutorialController.Dispose();
                tutorialController = null;
            }
            if (cells != null)
            {
                for (int r = 0; r < cells.GetLength(0); r++)
                    for (int c = 0; c < cells.GetLength(1); c++)
                        if (cells[r, c] != null)
                            Destroy(cells[r, c].gameObject);
                cells = null;
            }
        }

        private static LineConnectPuzzle CreateLinePuzzle()
        {
            int gridSize = 4;
            var cells = new LineConnectCell[1, 4];

            cells[0, 0] = new LineConnectCell { Row = 0, Col = 0, ColorId = 0, IsEndpoint = true };
            cells[0, 1] = new LineConnectCell { Row = 0, Col = 1, ColorId = -1, IsEndpoint = false };
            cells[0, 2] = new LineConnectCell { Row = 0, Col = 2, ColorId = -1, IsEndpoint = false };
            cells[0, 3] = new LineConnectCell { Row = 0, Col = 3, ColorId = 0, IsEndpoint = true };

            var solutions = new List<List<(int, int)>> { new List<(int, int)> { (0, 0), (0, 1), (0, 2), (0, 3) } };

            return new LineConnectPuzzle(gridSize, cells, 1, solutions, palette);
        }

        private static LineConnectPuzzle CreateSnakePuzzle()
        {
            int size = 3;
            var cells = new LineConnectCell[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c] = new LineConnectCell { Row = r, Col = c, ColorId = -1, IsEndpoint = false };

            cells[0, 0] = new LineConnectCell { Row = 0, Col = 0, ColorId = 0, IsEndpoint = true };
            cells[2, 2] = new LineConnectCell { Row = 2, Col = 2, ColorId = 0, IsEndpoint = true };

            var solutions = new List<List<(int, int)>> { new List<(int, int)> { (0, 0), (0, 1), (0, 2), (1, 2), (1, 1), (1, 0), (2, 0), (2, 1), (2, 2) } };

            return new LineConnectPuzzle(size, cells, 1, solutions, palette);
        }

        private static LineConnectPuzzle CreateFillPuzzle()
        {
            int size = 3;
            var cells = new LineConnectCell[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c] = new LineConnectCell { Row = r, Col = c, ColorId = -1, IsEndpoint = false };

            cells[0, 0] = new LineConnectCell { Row = 0, Col = 0, ColorId = 0, IsEndpoint = true };
            cells[1, 0] = new LineConnectCell { Row = 1, Col = 0, ColorId = 0, IsEndpoint = true };
            cells[2, 0] = new LineConnectCell { Row = 2, Col = 0, ColorId = 1, IsEndpoint = true };
            cells[2, 2] = new LineConnectCell { Row = 2, Col = 2, ColorId = 1, IsEndpoint = true };

            var solutions = new List<List<(int, int)>>();
            solutions.Add(new List<(int, int)> { (0, 0), (0, 1), (0, 2), (1, 2), (1, 1), (1, 0) });
            solutions.Add(new List<(int, int)> { (2, 0), (2, 1), (2, 2) });

            return new LineConnectPuzzle(size, cells, 2, solutions, palette);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupPhase();
        }
    }
}