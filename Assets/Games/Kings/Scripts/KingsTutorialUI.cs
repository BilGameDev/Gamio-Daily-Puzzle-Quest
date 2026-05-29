using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.Kings
{
    public class KingsTutorialUI : TutorialBase
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private KingsCellItem cellPrefab;
        [SerializeField] private Vector2 cellSize = new Vector2(100, 110);

        private KingsGridController currentController;
        private KingsCellItem[,] cells;
        private int kingsPlaced;
        private bool advancing;
        private bool isRunning;
        private bool isReplay;

        private const string GameId = "kings";

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                KingsGame.TutorialDeferred = true;
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
            if (KingsGame.Instance != null && KingsGame.TutorialDeferred)
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
            SetTotalSteps(4);
            StartTapPhase();
        }

        private void OnCellChanged(int r, int c)
        {
            if (cells != null && currentController != null)
                cells[r, c].SetState(currentController.Puzzle.GetState(r, c));
        }

        private void OnCellTap(int r, int c)
        {
            if (currentController != null && !currentController.IsSolved &&
                currentController.TapCell(r, c))
                cells[r, c].PlayTapAnimation();
        }

        private void OnCellHold(int r, int c)
        {
            if (currentController != null && !currentController.IsSolved &&
                currentController.HoldCell(r, c))
                cells[r, c].PlayTapAnimation();
        }

        private void OnPlacementDenied(int r, int c, int conflictR, int conflictC)
        {
            cells[r, c].PlayInvalidAnimation();
            if (conflictR >= 0 && conflictC >= 0)
                cells[conflictR, conflictC].PlayInvalidAnimation();
        }

        private void StartTapPhase()
        {
            CleanupPhase();

            currentController = new KingsGridController(CreatePuzzle(1, 1, null, null));
            currentController.OnCellChanged += OnCellChanged;
            currentController.OnCellChanged += OnTapAction;
            currentController.OnPlacementDenied += OnPlacementDenied;
            BuildGrid(currentController.Puzzle);
            SetCurrentStep(0);
            ShowInstruction("Tap the cell to place a null marker.");
        }

        private void OnTapAction(int r, int c)
        {
            if (currentController.Puzzle.GetState(r, c) != KingsCellState.Null) return;
            currentController.OnCellChanged -= OnCellChanged;
            currentController.OnCellChanged -= OnTapAction;
            currentController.OnPlacementDenied -= OnPlacementDenied;
            ShowInstruction("Good!");
            DOVirtual.DelayedCall(1f, StartHoldPhase);
        }

        private void StartHoldPhase()
        {
            CleanupPhase();

            currentController = new KingsGridController(CreatePuzzle(1, 1, null, null));
            currentController.OnCellChanged += OnCellChanged;
            currentController.OnCellChanged += OnHoldAction;
            currentController.OnPlacementDenied += OnPlacementDenied;
            BuildGrid(currentController.Puzzle);
            SetCurrentStep(1);
            ShowInstruction("Hold the cell to place a king.");
        }

        private void OnHoldAction(int r, int c)
        {
            if (currentController.Puzzle.GetState(r, c) != KingsCellState.King) return;
            currentController.OnCellChanged -= OnCellChanged;
            currentController.OnCellChanged -= OnHoldAction;
            currentController.OnPlacementDenied -= OnPlacementDenied;
            DOVirtual.DelayedCall(1.5f, StartTwoKingPhase);
        }

        private void StartTwoKingPhase()
        {
            CleanupPhase();
            kingsPlaced = 0;

            int[,] regions = { { 0, 0, 0 }, { 0, 0, 1 }, { 1, 1, 1 } };
            currentController = new KingsGridController(CreatePuzzle(3, 2, regions, null));
            currentController.OnCellChanged += OnCellChanged;
            currentController.OnCellChanged += OnTwoKingAction;
            currentController.OnPlacementDenied += OnPlacementDenied;
            BuildGrid(currentController.Puzzle);
            SetCurrentStep(2);
            ShowInstruction("Place one king in each colored region.");
        }

        private void OnTwoKingAction(int r, int c)
        {
            if (currentController.Puzzle.GetState(r, c) != KingsCellState.King) return;
            kingsPlaced++;
            if (kingsPlaced < 2) return;
            currentController.OnCellChanged -= OnCellChanged;
            currentController.OnCellChanged -= OnTwoKingAction;
            currentController.OnPlacementDenied -= OnPlacementDenied;
            ShowInstruction("Perfect!");
            DOVirtual.DelayedCall(1f, StartSolvePhase);
        }

        private void StartSolvePhase()
        {
            CleanupPhase();
            advancing = false;

            int[,] regions =
            {
                { 0, 0, 0, 1 },
                { 2, 0, 1, 1 },
                { 2, 2, 3, 1 },
                { 2, 3, 3, 3 }
            };
            var solution = new bool[4, 4];
            solution[0, 1] = solution[1, 3] = solution[2, 0] = solution[3, 2] = true;
            currentController = new KingsGridController(CreatePuzzle(4, 4, regions, solution));
            currentController.OnCellChanged += OnCellChanged;
            currentController.OnPlacementDenied += OnPlacementDenied;
            currentController.OnSolved += OnSolveSolved;
            BuildGrid(currentController.Puzzle);
            SetCurrentStep(3);
            ShowInstruction("Solve this puzzle! One king per row, column, region. No touching.");
        }

        private void OnSolveSolved()
        {
            if (advancing) return;
            advancing = true;
            currentController.OnSolved -= OnSolveSolved;
            ShowTemporary("Tutorial complete!", 1.5f, Finish);
        }

        private void Finish()
        {
            isRunning = false;
            CleanupPhase();
            FadeOutPanel(panel, 0.4f, () =>
            {
                new TutorialService().MarkCompleted(GameId);
                if (!isReplay)
                    KingsGame.Instance.StartGame();
                isReplay = false;
            });
        }

        private void BuildGrid(KingsPuzzle puzzle)
        {
            int size = puzzle.GridSize;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = size;
            gridLayout.cellSize = cellSize;

            var spacing = Mathf.RoundToInt(Mathf.Min(cellSize.x, cellSize.y) * 0.06f);
            gridLayout.spacing = new Vector2(spacing, spacing);

            cells = new KingsCellItem[size, size];
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    var sectionIndex = puzzle.Cells[r, c].SectionIndex;
                    cell.Init(r, c, sectionIndex, PastelColors.GetDistinct(sectionIndex));
                    cell.transform.localScale = Vector3.zero;
                    cell.transform.DOScale(Vector3.one, 0.3f)
                        .SetDelay((r * size + c) * 0.015f).SetEase(Ease.OutBack);
                    cell.OnTap += OnCellTap;
                    cell.OnHold += OnCellHold;
                    cells[r, c] = cell;
                }
            }
        }

        private void CleanupPhase()
        {
            if (currentController != null)
            {
                currentController.OnCellChanged -= OnCellChanged;
                currentController.OnCellChanged -= OnTapAction;
                currentController.OnCellChanged -= OnHoldAction;
                currentController.OnCellChanged -= OnTwoKingAction;
                currentController.OnPlacementDenied -= OnPlacementDenied;
                currentController.OnSolved -= OnSolveSolved;
                currentController.Dispose();
                currentController = null;
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

        private static KingsPuzzle CreatePuzzle(int size, int regionCount, int[,] regions, bool[,] solution)
        {
            var cells = new KingsCell[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c] = new KingsCell { Row = r, Col = c, SectionIndex = regions?[r, c] ?? 0 };
            return new KingsPuzzle(size, cells, regionCount, solution ?? new bool[size, size]);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupPhase();
        }
    }
}