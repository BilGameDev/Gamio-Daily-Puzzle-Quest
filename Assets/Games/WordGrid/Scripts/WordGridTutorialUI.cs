using System.Collections.Generic;
using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Tutorial;
using Gamio.Features.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Games.WordGrid
{
    public class WordGridTutorialUI : TutorialBase
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform wordCellsParent;
        [SerializeField] private CenteredGridLayout alphabetGrid;
        [SerializeField] private WordGridCellItem cellPrefab;
        [SerializeField] private WordGridLetterTile letterPrefab;
        [SerializeField] private Button submitButton;

        private WordGridController tutorialController;
        private WordGridCellItem[] wordCells;
        private List<WordGridLetterTile> letterTiles;
        private bool isRunning;
        private bool isReplay;

        private const string GameId = "wordgrid";
        private static readonly string tutorialLetters = "ACDEINOST";

        private void Awake()
        {
            var tutorialService = new TutorialService();
            if (!tutorialService.IsCompleted(GameId))
                WordGridGame.TutorialDeferred = true;
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
            if (WordGridGame.Instance != null && WordGridGame.TutorialDeferred)
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

            if (WordGridGame.ActiveSettings == null)
                WordGridGame.ActiveSettings = Resources.Load<WordGridGameSettingsSO>("WordGridGameSettings");
            if (WordGridGame.ActiveSettings == null)
                WordGridGame.ActiveSettings = ScriptableObject.CreateInstance<WordGridGameSettingsSO>();

            panel.SetActive(true);
            Show();
            SetTotalSteps(3);
            StartPlacePhase();
        }

        private void StartPlacePhase()
        {
            CleanupPhase();

            tutorialController = new WordGridController(new WordGridPuzzle("CAT"));
            tutorialController.OnAttemptComplete += OnAttemptFeedback;
            tutorialController.OnWordSubmitted += OnPlaceSubmitted;
            BuildGrid();
            SetCurrentStep(0);
            ShowInstruction("Tap letters from the alphabet to place them in the word cells. Then press Submit.");
        }

        private void OnPlaceSubmitted()
        {
            tutorialController.OnWordSubmitted -= OnPlaceSubmitted;
            DOVirtual.DelayedCall(0.8f, () =>
                ShowTemporary("Good! Now you'll see feedback colors.", 1.2f, StartFeedbackPhase));
        }

        private void StartFeedbackPhase()
        {
            CleanupPhase();

            tutorialController = new WordGridController(new WordGridPuzzle("CAT"));
            tutorialController.OnAttemptComplete += OnAttemptFeedback;
            tutorialController.OnWordSubmitted += OnFeedbackSubmitted;
            BuildGrid();
            SetCurrentStep(1);
            ShowInstruction("After submitting, you'll see feedback: green is correct, yellow is wrong position, gray is wrong letter.");
        }

        private void OnFeedbackSubmitted()
        {
            tutorialController.OnWordSubmitted -= OnFeedbackSubmitted;
            DOVirtual.DelayedCall(0.8f, () =>
                ShowTemporary("You understand the feedback! Now solve it.", 1.2f, StartClearPhase));
        }

        private void StartClearPhase()
        {
            CleanupPhase();

            tutorialController = new WordGridController(new WordGridPuzzle("DONE"));
            tutorialController.OnAttemptComplete += OnAttemptFeedback;
            tutorialController.OnSolved += OnClearSolved;
            BuildGrid();
            SetCurrentStep(2);
            ShowInstruction("Now guess the word correctly to finish the tutorial!");
        }

        private void OnClearSolved()
        {
            tutorialController.OnSolved -= OnClearSolved;
            DOVirtual.DelayedCall(0.8f, () =>
                ShowTemporary("Tutorial complete!", 1.5f, Finish));
        }

        private void Finish()
        {
            isRunning = false;
            CleanupPhase();
            FadeOutPanel(panel, 0.4f, () =>
            {
                new TutorialService().MarkCompleted(GameId);
                if (!isReplay)
                    WordGridGame.Instance.StartGame();
                isReplay = false;
            });
        }

        private void BuildGrid()
        {
            int wordLen = tutorialController.Puzzle.WordLength;

            wordCells = new WordGridCellItem[wordLen];
            for (int i = 0; i < wordLen; i++)
            {
                var cell = Instantiate(cellPrefab, wordCellsParent);
                cell.Init(i, tutorialController.Puzzle.Cells[i].Letter);
                cell.OnClicked += OnCellClicked;
                wordCells[i] = cell;
            }

            letterTiles = new List<WordGridLetterTile>();
            foreach (char c in tutorialLetters)
            {
                var tile = Instantiate(letterPrefab, alphabetGrid.transform);
                tile.Init(c);
                tile.OnClicked += OnLetterClicked;
                tile.transform.localScale = Vector3.zero;
                tile.transform.DOScale(Vector3.one, 0.25f).SetDelay(letterTiles.Count * 0.025f).SetEase(Ease.OutBack);
                letterTiles.Add(tile);
            }

            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmit);
        }

        private void OnAttemptFeedback(int attempt, TileState[] results)
        {
            float delay = 0;
            for (int i = 0; i < results.Length; i++)
            {
                int idx = i;
                DOVirtual.DelayedCall(delay, () =>
                {
                    wordCells[idx].transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 2, 0.5f);
                    wordCells[idx].SetState(results[idx]);
                });
                delay += 0.15f;
            }
            DOVirtual.DelayedCall(delay + 0.2f, () => submitButton.interactable = true);
        }

        private void OnLetterClicked(char letter)
        {
            if (tutorialController == null || tutorialController.IsSolved) return;
            for (int i = 0; i < wordCells.Length; i++)
            {
                if (!wordCells[i].HasLetter && !wordCells[i].IsLocked)
                {
                    wordCells[i].SetLetter(letter);
                    tutorialController.PlaceLetter(i, letter);
                    return;
                }
            }
        }

        private void OnCellClicked(int index)
        {
            if (tutorialController == null || tutorialController.IsSolved) return;
            if (!wordCells[index].HasLetter || wordCells[index].IsLocked) return;
            wordCells[index].ClearLetter();
            tutorialController.RemoveLetter(index);
        }

        private void OnSubmit()
        {
            if (tutorialController == null || tutorialController.IsSolved) return;
            if (!tutorialController.Puzzle.IsFullyFilled())
            {
                float delay = 0;
                for (int i = 0; i < wordCells.Length; i++)
                {
                    if (!wordCells[i].HasLetter)
                    {
                        int idx = i;
                        DOVirtual.DelayedCall(delay, () =>
                        {
                            wordCells[idx].transform.DOShakePosition(0.3f, new Vector3(5, 0, 0), 10, 90, false, false);
                        });
                        delay += 0.05f;
                    }
                }
                return;
            }

            submitButton.interactable = false;
            tutorialController.Submit();
        }

        private void CleanupPhase()
        {
            if (tutorialController != null)
            {
                tutorialController.OnAttemptComplete -= OnAttemptFeedback;
                tutorialController.OnWordSubmitted -= OnPlaceSubmitted;
                tutorialController.OnWordSubmitted -= OnFeedbackSubmitted;
                tutorialController.OnSolved -= OnClearSolved;
                tutorialController.Dispose();
                tutorialController = null;
            }

            if (wordCells != null)
            {
                foreach (var cell in wordCells)
                {
                    if (cell != null)
                    {
                        cell.OnClicked -= OnCellClicked;
                        Destroy(cell.gameObject);
                    }
                }
                wordCells = null;
            }

            if (letterTiles != null)
            {
                foreach (var tile in letterTiles)
                {
                    if (tile != null)
                    {
                        tile.OnClicked -= OnLetterClicked;
                        Destroy(tile.gameObject);
                    }
                }
                letterTiles = null;
            }

            if (wordCellsParent != null)
            {
                for (int i = wordCellsParent.childCount - 1; i >= 0; i--)
                    Destroy(wordCellsParent.GetChild(i).gameObject);
            }

            if (alphabetGrid != null)
            {
                var t = alphabetGrid.transform;
                for (int i = t.childCount - 1; i >= 0; i--)
                    Destroy(t.GetChild(i).gameObject);
            }

            if (submitButton != null)
                submitButton.onClick.RemoveListener(OnSubmit);
            submitButton.interactable = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupPhase();
        }
    }
}
