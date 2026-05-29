using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.Arrows
{
    public class ArrowsTutorialUI : TutorialBase
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private ArrowsCellItem cellPrefab;

        [Header("Grid")]
        [SerializeField] private Vector2 cellSize = new Vector2(110, 110);

        private ArrowsGridController tutorialController;
        private ArrowsCellItem[,] cells;
        private bool isRunning;
        private bool isReplay;
        private int currentPhase;

        private bool blockingDemoShown;
        private int blockingCleared;

        private const string GameId = "arrows";

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                ArrowsGame.TutorialDeferred = true;
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
            if (ArrowsGame.Instance != null && ArrowsGame.TutorialDeferred)
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
            if (ArrowsGame.ActiveSettings == null)
                ArrowsGame.ActiveSettings = Resources.Load<ArrowsGameSettingsSO>("ArrowsSettings");
            isRunning = true;
            panel.SetActive(true);
            Show();
            SetTotalSteps(3);
            StartSlidePhase();
        }

        private void StartSlidePhase()
        {
            CleanupPhase();

            currentPhase = 0;
            tutorialController = new ArrowsGridController(CreateSlidePuzzle());
            tutorialController.OnTileRemoved += OnTileRemoved;
            BuildGrid();
            SetCurrentStep(0);
            ShowInstruction("Tap the arrow tile to make it slide");
        }

        private void StartBlockingPhase()
        {
            CleanupPhase();

            currentPhase = 1;
            blockingDemoShown = false;
            blockingCleared = 0;

            tutorialController = new ArrowsGridController(CreateBlockingPuzzle());
            tutorialController.OnTileRemoved += OnTileRemoved;
            tutorialController.OnTileBlocked += OnTileBlocked;
            BuildGrid();
            SetCurrentStep(1);
            ShowInstruction("Some tiles are blocked by others");
        }

        private void StartClearPhase()
        {
            CleanupPhase();

            currentPhase = 2;
            tutorialController = new ArrowsGridController(CreateClearPuzzle());
            tutorialController.OnTileRemoved += OnTileRemoved;
            tutorialController.OnTileBlocked += OnClearBlocked;
            BuildGrid();
            SetCurrentStep(2);
            ShowInstruction("Now clear all tiles");
        }

        private void OnTileRemoved(int row, int col)
        {
            var cellItem = cells[row, col];
            var settings = ArrowsGame.ActiveSettings;
            float dur = settings != null ? settings.slideDuration : 0.35f;
            var ease = settings != null ? settings.slideEase : Ease.InBack;

            var dir = cellItem.Direction;
            Vector2 slideDir = dir switch
            {
                ArrowDirection.Up => Vector2.up,
                ArrowDirection.Down => Vector2.down,
                ArrowDirection.Left => Vector2.left,
                ArrowDirection.Right => Vector2.right,
                _ => Vector2.zero
            };

            int steps = tutorialController.Puzzle.SlideDistance(row, col);
            float stepSize = gridLayout.cellSize.x + gridLayout.spacing.x;
            float distance = steps * stepSize;

            cellItem.SetBlockRaycasts(false);
            cellItem.RectTransform.DOAnchorPos(cellItem.RectTransform.anchoredPosition + slideDir * distance, dur)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    cellItem.SetVisible(false);
                    cellItem.RectTransform.anchoredPosition = Vector2.zero;
                });

            switch (currentPhase)
            {
                case 0:
                    tutorialController.OnTileRemoved -= OnTileRemoved;
                    DOVirtual.DelayedCall(dur + 0.6f, StartBlockingPhase);
                    break;
                case 1:
                    HandleBlockingRemoved(row, col);
                    break;
                case 2:
                    HandleClearRemoved();
                    break;
            }
        }

        private void OnTileBlocked(int r, int c, int blockerRow, int blockerCol)
        {
            if (blockingDemoShown) return;
            blockingDemoShown = true;

            if (blockerRow >= 0 && blockerCol >= 0 && blockerRow < cells.GetLength(0) && blockerCol < cells.GetLength(1))
                cells[blockerRow, blockerCol]?.Flash();

            ShowInstruction("The path is blocked.\n\nTap the other tile to clear the path.");
        }

        private void HandleBlockingRemoved(int row, int col)
        {
            blockingCleared++;

            if (blockingCleared == 1)
            {
                if (blockingDemoShown)
                    ShowInstruction("Path is clear! Now tap the remaining tile.");
                else
                    ShowInstruction("That tile could slide freely. Now try the other one.");
            }
            else
            {
                tutorialController.OnTileRemoved -= OnTileRemoved;
                tutorialController.OnTileBlocked -= OnTileBlocked;
                blockingDemoShown = false;
                blockingCleared = 0;
                ShowTemporary("You understand the basics!", 1.2f, StartClearPhase);
            }
        }

        private void OnClearBlocked(int r, int c, int blockerRow, int blockerCol)
        {
            if (blockerRow >= 0 && blockerCol >= 0 && blockerRow < cells.GetLength(0) && blockerCol < cells.GetLength(1))
                cells[blockerRow, blockerCol]?.Flash();
        }

        private void HandleClearRemoved()
        {
            if (tutorialController.Puzzle.IsSolved())
            {
                tutorialController.OnTileRemoved -= OnTileRemoved;
                tutorialController.OnTileBlocked -= OnClearBlocked;
                ShowTemporary("You're ready!", 1.5f, Finish);
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
                    ArrowsGame.Instance.StartGame();
                isReplay = false;
            });
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

            cells = new ArrowsCellItem[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cellData = tutorialController.Puzzle.Cells[r, c];
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Init(r, c, cellData.Direction, cellData.IsEmpty, cellData.IsObstacle);
                    cell.OnClick += OnCellClicked;
                    cells[r, c] = cell;
                }
            }
        }

        private void OnCellClicked(int row, int col)
        {
            if (tutorialController == null || tutorialController.IsSolved) return;
            tutorialController.TrySlideTile(row, col);
        }

        private void CleanupPhase()
        {
            if (tutorialController != null)
            {
                tutorialController.OnTileRemoved -= OnTileRemoved;
                tutorialController.OnTileBlocked -= OnTileBlocked;
                tutorialController.OnTileBlocked -= OnClearBlocked;
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

        private static ArrowsPuzzle CreateSlidePuzzle()
        {
            int rows = 2, cols = 1;
            var cells = new ArrowsCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cells[r, c] = new ArrowsCell { Row = r, Col = c, Direction = ArrowDirection.None, IsEmpty = true };
            cells[0, 0] = new ArrowsCell { Row = 0, Col = 0, Direction = ArrowDirection.Right, IsEmpty = false };
            return new ArrowsPuzzle(rows, cols, cells);
        }

        private static ArrowsPuzzle CreateBlockingPuzzle()
        {
            int rows = 2, cols = 2;
            var cells = new ArrowsCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cells[r, c] = new ArrowsCell { Row = r, Col = c, Direction = ArrowDirection.None, IsEmpty = true };
            cells[0, 0] = new ArrowsCell { Row = 0, Col = 0, Direction = ArrowDirection.Right, IsEmpty = false };
            cells[0, 1] = new ArrowsCell { Row = 0, Col = 1, Direction = ArrowDirection.Right, IsEmpty = false };
            return new ArrowsPuzzle(rows, cols, cells);
        }

        private static ArrowsPuzzle CreateClearPuzzle()
        {
            int rows = 3, cols = 3;
            var cells = new ArrowsCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cells[r, c] = new ArrowsCell { Row = r, Col = c, Direction = ArrowDirection.None, IsEmpty = true };

            cells[0, 0] = new ArrowsCell { Row = 0, Col = 0, Direction = ArrowDirection.Right, IsEmpty = false };
            cells[0, 1] = new ArrowsCell { Row = 0, Col = 1, Direction = ArrowDirection.Right, IsEmpty = false };
            cells[0, 2] = new ArrowsCell { Row = 0, Col = 2, Direction = ArrowDirection.Right, IsEmpty = false };
            cells[1, 0] = new ArrowsCell { Row = 1, Col = 0, Direction = ArrowDirection.Down, IsEmpty = false };
            cells[1, 1] = new ArrowsCell { Row = 1, Col = 1, Direction = ArrowDirection.Left, IsEmpty = false };
            cells[1, 2] = new ArrowsCell { Row = 1, Col = 2, Direction = ArrowDirection.Up, IsEmpty = false };
            cells[2, 0] = new ArrowsCell { Row = 2, Col = 0, Direction = ArrowDirection.Down, IsEmpty = false };
            cells[2, 2] = new ArrowsCell { Row = 2, Col = 2, Direction = ArrowDirection.Down, IsEmpty = false };

            cells[2, 1] = new ArrowsCell { Row = 2, Col = 1, Direction = ArrowDirection.None, IsEmpty = false, IsObstacle = true };

            return new ArrowsPuzzle(rows, cols, cells);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupPhase();
        }
    }
}
