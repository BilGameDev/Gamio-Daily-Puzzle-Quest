using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.Hitori
{
    public class HitoriTutorialUI : TutorialBase
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private HitoriCellItem cellPrefab;
        [SerializeField] private Color cellColor = Color.white;
        [SerializeField] private float maxCellSize = 120f;

        private HitoriGridController tutorialController;
        private HitoriCellItem[,] cells;
        private bool isRunning;
        private bool isReplay;
        private bool phaseAdvancing;

        private const string GameId = "hitori";

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                HitoriGame.TutorialDeferred = true;
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
            if (HitoriGame.Instance != null && HitoriGame.TutorialDeferred)
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
            phaseAdvancing = false;

            tutorialController = new HitoriGridController(CreateTapPuzzle());
            tutorialController.OnCellTapped += OnTapPhaseTapped;
            BuildGrid();
            SetCurrentStep(0);
            ShowInstruction("Tap a cell to cycle its state between white, black, and shaded.");
        }

        private static HitoriPuzzle CreateTapPuzzle()
        {
            int size = 2;
            var cells = new HitoriCell[size, size];
            cells[0, 0] = new HitoriCell { Row = 0, Col = 0, Number = 1 };
            cells[0, 1] = new HitoriCell { Row = 0, Col = 1, Number = 2 };
            cells[1, 0] = new HitoriCell { Row = 1, Col = 0, Number = 3 };
            cells[1, 1] = new HitoriCell { Row = 1, Col = 1, Number = 4 };
            return new HitoriPuzzle(size, cells);
        }

        private void OnTapPhaseTapped(int r, int c)
        {
            tutorialController.OnCellTapped -= OnTapPhaseTapped;
            DOVirtual.DelayedCall(0.5f, StartViolationPhase);
        }

        private void StartViolationPhase()
        {
            CleanupPhase();
            phaseAdvancing = false;

            tutorialController = new HitoriGridController(CreateViolationPuzzle());
            tutorialController.OnCellTapped += OnViolationTapped;
            tutorialController.OnSolved += OnViolationSolved;
            BuildGrid();
            SetCurrentStep(1);
            ShowInstruction("If two same numbers share a row or column, at least one must be black. Tap to black one out, then tap Check.");
        }

        private static HitoriPuzzle CreateViolationPuzzle()
        {
            int size = 2;
            var cells = new HitoriCell[size, size];
            cells[0, 0] = new HitoriCell { Row = 0, Col = 0, Number = 1 };
            cells[0, 1] = new HitoriCell { Row = 0, Col = 1, Number = 1 };
            cells[1, 0] = new HitoriCell { Row = 1, Col = 0, Number = 2 };
            cells[1, 1] = new HitoriCell { Row = 1, Col = 1, Number = 3 };
            return new HitoriPuzzle(size, cells);
        }

        private void OnViolationTapped(int r, int c)
        {
            if (tutorialController.Puzzle.GetState(r, c) == HitoriCellState.Black)
                ShowInstruction("Good! Now tap Check to verify the solution.");
        }

        private void OnViolationSolved()
        {
            if (phaseAdvancing) return;
            phaseAdvancing = true;
            tutorialController.OnCellTapped -= OnViolationTapped;
            tutorialController.OnSolved -= OnViolationSolved;
            DOVirtual.DelayedCall(0.8f, StartClearPhase);
        }

        private void StartClearPhase()
        {
            CleanupPhase();
            phaseAdvancing = false;

            tutorialController = new HitoriGridController(CreateClearPuzzle());
            tutorialController.OnSolved += OnClearSolved;
            BuildGrid();
            SetCurrentStep(2);
            ShowInstruction("Now solve this puzzle on your own! Black out one of the duplicate numbers, then tap Check.");
        }

        private static HitoriPuzzle CreateClearPuzzle()
        {
            int size = 2;
            var cells = new HitoriCell[size, size];
            cells[0, 0] = new HitoriCell { Row = 0, Col = 0, Number = 1 };
            cells[0, 1] = new HitoriCell { Row = 0, Col = 1, Number = 2 };
            cells[1, 0] = new HitoriCell { Row = 1, Col = 0, Number = 1 };
            cells[1, 1] = new HitoriCell { Row = 1, Col = 1, Number = 3 };
            return new HitoriPuzzle(size, cells);
        }

        private void OnClearSolved()
        {
            if (phaseAdvancing) return;
            phaseAdvancing = true;
            tutorialController.OnSolved -= OnClearSolved;
            ShowTemporary("Tutorial complete!", 1.5f, Finish);
        }

        private void BuildGrid()
        {
            int size = tutorialController.Puzzle.GridSize;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = size;

            var rect = gridLayout.GetComponent<RectTransform>().rect;
            float availW = Mathf.Max(0, rect.width - gridLayout.padding.left - gridLayout.padding.right);
            float availH = Mathf.Max(0, rect.height - gridLayout.padding.top - gridLayout.padding.bottom);
            float spacing = Mathf.Min(availW, availH) * 0.01f;
            gridLayout.spacing = new Vector2(spacing, spacing);
            float totalSpacingX = (size - 1) * spacing;
            float totalSpacingY = (size - 1) * spacing;
            float cellSize = Mathf.Min((availW - totalSpacingX) / size, (availH - totalSpacingY) / size, maxCellSize);
            gridLayout.cellSize = new Vector2(cellSize, cellSize);

            cells = new HitoriCellItem[size, size];
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Init(r, c, tutorialController.Puzzle.Cells[r, c].Number);
                    cell.Image.color = cellColor;
                    cell.transform.localScale = Vector3.zero;
                    cell.transform.DOScale(Vector3.one, 0.25f).SetDelay((r * size + c) * 0.02f).SetEase(Ease.OutBack);
                    cell.OnClick += (row, col) =>
                    {
                        tutorialController.TapCell(row, col);
                        RefreshCell(row, col);
                        cell.PlayTapAnimation();
                        foreach (var (vr, vc) in tutorialController.Puzzle.GetViolations())
                            if (vr >= 0 && vr < size && vc >= 0 && vc < size)
                                cells[vr, vc].PlayViolationAnimation();
                    };
                    cells[r, c] = cell;
                }
            }
        }

        private void RefreshCell(int r, int c)
        {
            var state = tutorialController.Puzzle.GetState(r, c);
            cells[r, c].SetVisual(state, Color.white);
        }

        private void Finish()
        {
            isRunning = false;
            CleanupPhase();
            FadeOutPanel(panel, 0.4f, () =>
            {
                new TutorialService().MarkCompleted(GameId);
                if (!isReplay)
                    HitoriGame.Instance.StartGame();
                isReplay = false;
            });
        }

        private void CleanupPhase()
        {
            if (tutorialController != null)
            {
                tutorialController.OnCellTapped -= OnTapPhaseTapped;
                tutorialController.OnCellTapped -= OnViolationTapped;
                tutorialController.OnSolved -= OnViolationSolved;
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
