using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.Pipes
{
    public class PipesTutorialUI : TutorialBase
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private PipesCellItem cellPrefab;
        [Header("Visual")]
        [SerializeField] private Vector2 cellSize = new Vector2(100, 110);
        [SerializeField] private Color cellBackground = new Color(0.12f, 0.12f, 0.14f);

        private PipesGridController tutorialController;
        private PipesCellItem[,] cells;
        private bool isRunning;
        private bool isReplay;

        private const string GameId = "pipes";

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                PipesGame.TutorialDeferred = true;
        }

        private void OnEnable()
        {
            GamioEvents.OnTutorialRequested += Replay;
            GamioEvents.OnSkipTutorialRequested += SkipTutorial;
        }

        private void OnDisable()
        {
            GamioEvents.OnTutorialRequested -= Replay;
            GamioEvents.OnSkipTutorialRequested -= SkipTutorial;
        }

       protected override void Start()
        {
            base.Start();
            if (PipesGame.Instance != null && PipesGame.TutorialDeferred)
                Begin();
        }

        public void Replay()
        {
            isReplay = true;
            Begin();
        }

        private void SkipTutorial() => Finish();

        public override void Begin()
        {
            if (isRunning) return;
            isRunning = true;

            panel.SetActive(true);
            Show();
            SetTotalSteps(3);
            StartTapPhase();
        }

        private void StartTapPhase()
        {
            CleanupPhase();

            tutorialController = new PipesGridController(CreateTapPuzzle());
            tutorialController.OnCellTapped += OnTapPhaseTapped;
            tutorialController.OnSolved += OnTapPhaseSolved;
            BuildGrid();
            SetCurrentStep(0);
            ShowInstruction("Tap the pipe to rotate it and connect the two nodes.");
        }

        private static PipesPuzzle CreateTapPuzzle()
        {
            int size = 2;
            var cells = new PipesCell[size, size];
            var targetRotations = new int[size, size];
            var initialRotations = new int[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    cells[r, c] = new PipesCell { Row = r, Col = c, Type = PipeType.Empty, IsFixed = false, IsPort = false, PortDirection = 0 };
                    targetRotations[r, c] = 0;
                    initialRotations[r, c] = 0;
                }

            cells[0, 0] = new PipesCell { Row = 0, Col = 0, Type = PipeType.Straight, IsFixed = true, IsPort = true, PortDirection = 1 };
            cells[0, 1] = new PipesCell { Row = 0, Col = 1, Type = PipeType.Bend, IsFixed = false, IsPort = false, PortDirection = 0 };
            cells[1, 1] = new PipesCell { Row = 1, Col = 1, Type = PipeType.Straight, IsFixed = true, IsPort = true, PortDirection = 0 };

            targetRotations[0, 1] = 2;
            initialRotations[0, 1] = 0;

            return new PipesPuzzle(size, cells, targetRotations, initialRotations);
        }

        private void OnTapPhaseSolved()
        {
            tutorialController.OnSolved -= OnTapPhaseSolved;
            tutorialController.OnCellTapped -= OnTapPhaseTapped;
            for (int r = 0; r < cells.GetLength(0); r++)
                for (int c = 0; c < cells.GetLength(1); c++)
                    if (tutorialController.Puzzle.Cells[r, c].IsPort)
                        cells[r, c].SetPortConnected(tutorialController.Puzzle.Cells[r, c].PortDirection);
            ShowInstruction("Good! Now connect two pipes together.");
            DOVirtual.DelayedCall(1.2f, StartConnectionPhase);
        }

        private void OnTapPhaseTapped(int r, int c)
        {
            if (tutorialController.Puzzle.IsRotationCorrect(0, 1))
                tutorialController.Check();
        }

        private void StartConnectionPhase()
        {
            CleanupPhase();

            tutorialController = new PipesGridController(CreateConnectionPuzzle());
            tutorialController.OnCellTapped += OnConnectionPhaseTapped;
            tutorialController.OnSolved += OnConnectionPhaseSolved;
            BuildGrid();
            SetCurrentStep(1);
            ShowInstruction("Rotate both pipes to form a path between the nodes.");
        }

        private static PipesPuzzle CreateConnectionPuzzle()
        {
            int size = 3;
            var cells = new PipesCell[size, size];
            var targetRotations = new int[size, size];
            var initialRotations = new int[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    cells[r, c] = new PipesCell { Row = r, Col = c, Type = PipeType.Empty, IsFixed = false, IsPort = false, PortDirection = 0 };
                    targetRotations[r, c] = 0;
                    initialRotations[r, c] = 0;
                }

            cells[0, 0] = new PipesCell { Row = 0, Col = 0, Type = PipeType.Straight, IsFixed = true, IsPort = true, PortDirection = 1 };
            cells[0, 1] = new PipesCell { Row = 0, Col = 1, Type = PipeType.Bend, IsFixed = false, IsPort = false, PortDirection = 0 };
            cells[1, 1] = new PipesCell { Row = 1, Col = 1, Type = PipeType.Bend, IsFixed = false, IsPort = false, PortDirection = 0 };
            cells[1, 2] = new PipesCell { Row = 1, Col = 2, Type = PipeType.Straight, IsFixed = true, IsPort = true, PortDirection = 3 };

            targetRotations[0, 1] = 2;
            targetRotations[1, 1] = 0;
            initialRotations[0, 1] = 0;
            initialRotations[1, 1] = 2;

            return new PipesPuzzle(size, cells, targetRotations, initialRotations);
        }

        private void OnConnectionPhaseSolved()
        {
            tutorialController.OnSolved -= OnConnectionPhaseSolved;
            tutorialController.OnCellTapped -= OnConnectionPhaseTapped;
            for (int r = 0; r < cells.GetLength(0); r++)
                for (int c = 0; c < cells.GetLength(1); c++)
                    if (tutorialController.Puzzle.Cells[r, c].IsPort)
                        cells[r, c].SetPortConnected(tutorialController.Puzzle.Cells[r, c].PortDirection);
            ShowTemporary("You connected the path!", 1.2f, StartClearPhase);
        }

        private void OnConnectionPhaseTapped(int r, int c)
        {
            if (tutorialController.Puzzle.IsRotationCorrect(0, 1) && tutorialController.Puzzle.IsRotationCorrect(1, 1))
                tutorialController.Check();
        }

        private void StartClearPhase()
        {
            CleanupPhase();

            tutorialController = new PipesGridController(CreateClearPuzzle());
            tutorialController.OnSolved += OnClearSolved;
            BuildGrid();
            SetCurrentStep(2);
            ShowInstruction("Now solve this puzzle to finish the tutorial!");
        }

        private static PipesPuzzle CreateClearPuzzle()
        {
            int size = 4;
            var cells = new PipesCell[size, size];
            var targetRotations = new int[size, size];
            var initialRotations = new int[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    cells[r, c] = new PipesCell { Row = r, Col = c, Type = PipeType.Empty, IsFixed = false, IsPort = false, PortDirection = 0 };
                    targetRotations[r, c] = 0;
                    initialRotations[r, c] = 0;
                }

            cells[0, 0] = new PipesCell { Row = 0, Col = 0, Type = PipeType.Straight, IsFixed = true, IsPort = true, PortDirection = 1 };
            cells[0, 1] = new PipesCell { Row = 0, Col = 1, Type = PipeType.Bend, IsFixed = false, IsPort = false, PortDirection = 0 };
            cells[1, 1] = new PipesCell { Row = 1, Col = 1, Type = PipeType.TJunction, IsFixed = false, IsPort = false, PortDirection = 0 };
            cells[1, 3] = new PipesCell { Row = 1, Col = 3, Type = PipeType.Bend, IsFixed = false, IsPort = false, PortDirection = 0 };
            cells[1, 2] = new PipesCell { Row = 1, Col = 2, Type = PipeType.Straight, IsFixed = false, IsPort = false, PortDirection = 0 };
            cells[0, 3] = new PipesCell { Row = 0, Col = 3, Type = PipeType.Straight, IsFixed = true, IsPort = true, PortDirection = 2 };
            cells[2, 1] = new PipesCell { Row = 2, Col = 1, Type = PipeType.Straight, IsFixed = false, IsPort = false, PortDirection = 0 };
            cells[3, 1] = new PipesCell { Row = 3, Col = 1, Type = PipeType.Straight, IsFixed = true, IsPort = true, PortDirection = 0 };

            targetRotations[0, 1] = 2;
            targetRotations[1, 1] = 1;
            targetRotations[1, 3] = 3;
            targetRotations[1, 2] = 1;
            targetRotations[2, 1] = 0;
            initialRotations[0, 1] = 0;
            initialRotations[1, 1] = 0;
            initialRotations[1, 3] = 1;
            initialRotations[1, 2] = 0;
            initialRotations[2, 1] = 1;

            return new PipesPuzzle(size, cells, targetRotations, initialRotations);
        }

        private void OnClearSolved()
        {
            tutorialController.OnSolved -= OnClearSolved;
            for (int r = 0; r < cells.GetLength(0); r++)
                for (int c = 0; c < cells.GetLength(1); c++)
                    if (tutorialController.Puzzle.Cells[r, c].IsPort)
                        cells[r, c].SetPortConnected(tutorialController.Puzzle.Cells[r, c].PortDirection);
            ShowTemporary("Tutorial complete!", 1.5f, Finish);
        }

        private void BuildGrid()
        {
            int size = tutorialController.Puzzle.GridSize;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = size;
            gridLayout.cellSize = cellSize;

            var spacing = Mathf.RoundToInt(Mathf.Min(cellSize.x, cellSize.y) * 0.06f);
            gridLayout.spacing = new Vector2(spacing, spacing);

            cells = new PipesCellItem[size, size];
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Row = r;
                    cell.Col = c;
                    cell.GetComponent<Image>().color = cellBackground;
                    cell.transform.localScale = Vector3.zero;
                    cell.transform.DOScale(Vector3.one, 0.3f).SetDelay((r * size + c) * 0.025f).SetEase(Ease.OutBack);
                    cell.OnClick += (row, col) =>
                    {
                        tutorialController.TapCell(row, col);
                        cell.PlayTapAnimation();
                        RefreshAll();
                        if (tutorialController != null)
                            tutorialController.Check();
                    };
                    cells[r, c] = cell;
                }
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            var puzzle = tutorialController.Puzzle;
            int size = puzzle.GridSize;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = puzzle.Cells[r, c];
                    int currentRot = puzzle.GetRotation(r, c);
                    cells[r, c].SetVisual(cell.Type, currentRot, cell.IsPort, cell.PortDirection);
                }
            }
        }

        private void Finish()
        {
            isRunning = false;
            CleanupPhase();
            FadeOutPanel(panel, 0.4f, () =>
            {
                new TutorialService().MarkCompleted(GameId);
                if (!isReplay)
                    PipesGame.Instance.StartGame();
                isReplay = false;
            });
        }

        private void CleanupPhase()
        {
            if (tutorialController != null)
            {
                tutorialController.OnCellTapped -= OnTapPhaseTapped;
                tutorialController.OnCellTapped -= OnConnectionPhaseTapped;
                tutorialController.OnSolved -= OnTapPhaseSolved;
                tutorialController.OnSolved -= OnConnectionPhaseSolved;
                tutorialController.OnSolved -= OnClearSolved;
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupPhase();
        }
    }
}
