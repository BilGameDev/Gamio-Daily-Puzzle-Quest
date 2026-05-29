using System.Collections.Generic;
using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.WordSearch
{
    public class WordSearchTutorialUI : TutorialBase
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private WordSearchCellItem cellPrefab;
        [Header("Colors")]
        [SerializeField] private Color cellColor = Color.white;
        [Header("Layout")]
        [SerializeField] private float maxCellSize = 120f;
        [SerializeField] private RectTransform wordListContainer;
        [SerializeField] private TextMeshProUGUI wordListItemPrefab;

        private WordSearchGridController tutorialController;
        private WordSearchCellItem[,] cells;
        private readonly List<TextMeshProUGUI> wordLabels = new List<TextMeshProUGUI>();
        private bool isRunning;
        private bool isReplay;

        private const string GameId = "wordsearch";

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                WordSearchGame.TutorialDeferred = true;
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
            if (WordSearchGame.Instance != null && WordSearchGame.TutorialDeferred)
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
            StartDragPhase();
        }

        private void StartDragPhase()
        {
            CleanupPhase();

            tutorialController = new WordSearchGridController(CreateDragPuzzle());
            tutorialController.OnWordFound += OnDragWordFound;
            BuildGrid();
            SetCurrentStep(0);
            ShowInstruction("Drag across the letters to find hidden words. Start at the first letter and drag to the last.");
        }

        private static WordSearchPuzzle CreateDragPuzzle()
        {
            int size = 3;
            var placements = new List<WordPlacement>
            {
                new WordPlacement { Word = "cat", StartRow = 1, StartCol = 0, DirRow = 0, DirCol = 1 }
            };

            char[,] grid = new char[size, size];
            int[,] wordIndex = new int[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    wordIndex[r, c] = -1;

            string word = "cat";
            for (int i = 0; i < word.Length; i++)
            {
                grid[1, i] = word[i];
                wordIndex[1, i] = 0;
            }

            char[] fill = { 'q', 'w', 'e', 'r', 't', 'y', 'u' };
            int fi = 0;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    if (wordIndex[r, c] == -1)
                        grid[r, c] = fill[fi++];

            var cells = new WordSearchCell[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c] = new WordSearchCell { Row = r, Col = c, Letter = grid[r, c], WordIndex = wordIndex[r, c] };

            var wordList = new List<string> { "cat" };
            return new WordSearchPuzzle(size, cells, placements, wordList);
        }

        private void OnDragWordFound(string word)
        {
            tutorialController.OnWordFound -= OnDragWordFound;
            ShowTemporary("Great! You found a word!", 1.2f, StartDirectionPhase);
        }

        private void StartDirectionPhase()
        {
            CleanupPhase();

            tutorialController = new WordSearchGridController(CreateDirectionPuzzle());
            tutorialController.OnWordFound += OnDirectionWordFound;
            BuildGrid();
            SetCurrentStep(1);
            ShowInstruction("Words can be horizontal, vertical, or diagonal. Try finding this vertical word.");
        }

        private static WordSearchPuzzle CreateDirectionPuzzle()
        {
            int size = 3;
            var placements = new List<WordPlacement>
            {
                new WordPlacement { Word = "dog", StartRow = 0, StartCol = 1, DirRow = 1, DirCol = 0 }
            };

            char[,] grid = new char[size, size];
            int[,] wordIndex = new int[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    wordIndex[r, c] = -1;

            string word = "dog";
            for (int i = 0; i < word.Length; i++)
            {
                grid[i, 1] = word[i];
                wordIndex[i, 1] = 0;
            }

            char[] fill = { 'p', 'a', 's', 'f', 'h', 'l' };
            int fi = 0;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    if (wordIndex[r, c] == -1)
                        grid[r, c] = fill[fi++];

            var cells = new WordSearchCell[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c] = new WordSearchCell { Row = r, Col = c, Letter = grid[r, c], WordIndex = wordIndex[r, c] };

            var wordList = new List<string> { "dog" };
            return new WordSearchPuzzle(size, cells, placements, wordList);
        }

        private void OnDirectionWordFound(string word)
        {
            tutorialController.OnWordFound -= OnDirectionWordFound;
            ShowTemporary("Correct! Words can go in any direction.", 1.2f, StartClearPhase);
        }

        private void StartClearPhase()
        {
            CleanupPhase();

            tutorialController = new WordSearchGridController(CreateClearPuzzle());
            tutorialController.OnSolved += OnClearSolved;
            BuildGrid();
            SetCurrentStep(2);
            ShowInstruction("Now find all the words to finish the tutorial!");
        }

        private static WordSearchPuzzle CreateClearPuzzle()
        {
            int size = 4;
            var placements = new List<WordPlacement>
            {
                new WordPlacement { Word = "sun", StartRow = 0, StartCol = 0, DirRow = 0, DirCol = 1 },
                new WordPlacement { Word = "fun", StartRow = 0, StartCol = 3, DirRow = 1, DirCol = 0 }
            };

            char[,] grid = new char[size, size];
            int[,] wordIndex = new int[size, size];

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    wordIndex[r, c] = -1;

            string sun = "sun";
            for (int i = 0; i < sun.Length; i++)
            {
                grid[0, i] = sun[i];
                wordIndex[0, i] = 0;
            }

            string fun = "fun";
            for (int i = 0; i < fun.Length; i++)
            {
                grid[i, 3] = fun[i];
                wordIndex[i, 3] = 1;
            }

            char[] fill = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            int fi = 0;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    if (wordIndex[r, c] == -1)
                        grid[r, c] = fill[fi++];

            var cells = new WordSearchCell[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    cells[r, c] = new WordSearchCell { Row = r, Col = c, Letter = grid[r, c], WordIndex = wordIndex[r, c] };

            var wordList = new List<string> { "sun", "fun" };
            return new WordSearchPuzzle(size, cells, placements, wordList);
        }

        private void OnClearSolved()
        {
            tutorialController.OnSolved -= OnClearSolved;
            ShowTemporary("Tutorial complete!", 1.5f, Finish);
        }

        private void BuildWordList()
        {
            wordLabels.Clear();
            foreach (var placement in tutorialController.Puzzle.Placements)
            {
                var label = Instantiate(wordListItemPrefab, wordListContainer);
                label.text = placement.Word.ToUpperInvariant();
                label.gameObject.SetActive(true);
                wordLabels.Add(label);
            }
        }

        private void BuildGrid()
        {
            int size = tutorialController.Puzzle.GridSize;
            BuildWordList();
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

            cells = new WordSearchCellItem[size, size];
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cell = Instantiate(cellPrefab, gridLayout.transform);
                    cell.Init(r, c, tutorialController.Puzzle.Cells[r, c].Letter);
                    cell.SetTileColor(cellColor);
                    cell.transform.localScale = Vector3.zero;
                    cell.transform.DOScale(Vector3.one, 0.25f).SetDelay((r * size + c) * 0.015f).SetEase(Ease.OutBack);
                    cell.OnPointerDownEvent += OnCellDown;
                    cell.OnPointerEnterEvent += OnCellEnter;
                    cell.OnPointerUpEvent += OnCellUp;
                    cells[r, c] = cell;
                }
            }
        }

        private void OnCellDown(int row, int col)
        {
            tutorialController.StartDrag(row, col);
            RefreshDragVisuals();
        }

        private void OnCellEnter(int row, int col)
        {
            if (!tutorialController.IsDragging) return;
            tutorialController.UpdateDrag(row, col);
            RefreshDragVisuals();
        }

        private void OnCellUp()
        {
            if (!tutorialController.IsDragging) return;
            tutorialController.EndDrag();
            RefreshDragVisuals();
        }

        private void RefreshDragVisuals()
        {
            if (cells == null) return;
            int size = tutorialController.Puzzle.GridSize;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    bool inDrag = tutorialController.IsDragging &&
                        (tutorialController.DragStartRow == r && tutorialController.DragStartCol == c &&
                         tutorialController.DragEndRow == r && tutorialController.DragEndCol == c) ||
                        IsCellInDragPath(r, c);

                    if (tutorialController.Puzzle.IsCellFound(r, c))
                    {
                        cells[r, c].SetFound(new Color(0.6f, 0.9f, 0.6f, 0.5f));
                    }
                    else if (inDrag)
                    {
                        bool valid = IsValidDragLine();
                        cells[r, c].SetHighlight(true, valid
                            ? new Color(0.3f, 0.8f, 1f, 0.6f)
                            : new Color(1f, 0.3f, 0.3f, 0.5f));
                    }
                    else
                    {
                        cells[r, c].SetHighlight(false, Color.white);
                    }
                }
            }
        }

        private bool IsCellInDragPath(int row, int col)
        {
            int dr = tutorialController.DragEndRow - tutorialController.DragStartRow;
            int dc = tutorialController.DragEndCol - tutorialController.DragStartCol;
            int minR = Mathf.Min(tutorialController.DragStartRow, tutorialController.DragEndRow);
            int maxR = Mathf.Max(tutorialController.DragStartRow, tutorialController.DragEndRow);
            int minC = Mathf.Min(tutorialController.DragStartCol, tutorialController.DragEndCol);
            int maxC = Mathf.Max(tutorialController.DragStartCol, tutorialController.DragEndCol);

            if (dr == 0) return row == tutorialController.DragStartRow && col >= minC && col <= maxC;
            if (dc == 0) return col == tutorialController.DragStartCol && row >= minR && row <= maxR;
            if (Mathf.Abs(dr) == Mathf.Abs(dc))
            {
                int drn = dr / Mathf.Abs(dr);
                int dcn = dc / Mathf.Abs(dc);
                int rr = tutorialController.DragStartRow;
                int cc = tutorialController.DragStartCol;
                int len = Mathf.Abs(dr);
                for (int i = 0; i <= len; i++)
                {
                    if (rr == row && cc == col) return true;
                    rr += drn;
                    cc += dcn;
                }
            }
            return false;
        }

        private bool IsValidDragLine()
        {
            int dr = tutorialController.DragEndRow - tutorialController.DragStartRow;
            int dc = tutorialController.DragEndCol - tutorialController.DragStartCol;
            return dr == 0 || dc == 0 || Mathf.Abs(dr) == Mathf.Abs(dc);
        }

        private void Finish()
        {
            isRunning = false;
            CleanupPhase();
            FadeOutPanel(panel, 0.4f, () =>
            {
                new TutorialService().MarkCompleted(GameId);
                if (!isReplay)
                    WordSearchGame.Instance.StartGame();
                isReplay = false;
            });
        }

        private void CleanupPhase()
        {
            foreach (var label in wordLabels)
                if (label != null) Destroy(label.gameObject);
            wordLabels.Clear();

            if (tutorialController != null)
            {
                tutorialController.OnWordFound -= OnDragWordFound;
                tutorialController.OnWordFound -= OnDirectionWordFound;
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
