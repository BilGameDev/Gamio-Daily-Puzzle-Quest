using System.Collections;
using System.Collections.Generic;
using Gamio.Features;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using DG.Tweening;
using TMPro;
using Gamio.Core;

namespace Gamio.Games.WordGrid
{
    public class WordGridUI : GameUI
    {
        [Header("References")]
        [SerializeField] private RectTransform wordTilesParent;
        [SerializeField] private RectTransform alphabetParent;
        [SerializeField] private WordGridCellItem cellPrefab;
        [SerializeField] private WordGridLetterTile letterPrefab;
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI attemptsText;

        private WordGridController grid;
        private WordGridCellItem[] wordCells;
        private List<WordGridLetterTile> letterTiles;
        private int wordLength;
        private bool isAnimating;

        public event System.Action OnSolved;

        protected override void OnEnable()
        {
            base.OnEnable();
            WordGridGame.OnControllerCreated += OnControllerCreated;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            WordGridGame.OnControllerCreated -= OnControllerCreated;
        }

        protected override void Start()
        {
            base.Start();

            if (launchOnStart)
            {
                LaunchGame(new WordGridGame());
            }
        }

        public void Setup(WordGridController controller)
        {
            CleanupGrid();

            grid = controller;
            wordLength = grid.Puzzle.WordLength;
            grid.OnSolved += HandleSolved;
            grid.OnAttemptComplete += HandleAttemptComplete;

            BuildWordTiles();
            BuildAlphabet();
            UpdateAttempts();

            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmit);
        }

        private void BuildWordTiles()
        {
            wordCells = new WordGridCellItem[wordLength];
            for (int i = 0; i < wordLength; i++)
            {
                var cell = Instantiate(cellPrefab, wordTilesParent);
                cell.Init(i, grid.Puzzle.Cells[i].Letter);
                cell.OnClicked += OnCellClicked;
                wordCells[i] = cell;
            }
        }

        private void BuildAlphabet()
        {
            letterTiles = new List<WordGridLetterTile>();
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            foreach (char c in letters)
            {
                var tile = Instantiate(letterPrefab, alphabetParent);
                tile.Init(c);
                tile.OnClicked += OnLetterClicked;
                tile.transform.localScale = Vector3.zero;
                tile.transform.DOScale(Vector3.one, 0.25f).SetDelay(letterTiles.Count * 0.025f).SetEase(Ease.OutBack);
                letterTiles.Add(tile);
            }
        }

        private void OnLetterClicked(char letter)
        {
            if (isAnimating || grid == null || grid.IsSolved) return;
            HapticsHelper.PlaySoftImpact();
            for (int i = 0; i < wordLength; i++)
            {
                if (!wordCells[i].HasLetter && !wordCells[i].IsLocked)
                {
                    wordCells[i].SetLetter(letter);
                    grid.PlaceLetter(i, letter);
                    return;
                }
            }
        }

        private void OnCellClicked(int index)
        {
            if (isAnimating || grid == null || grid.IsSolved) return;
            if (!wordCells[index].HasLetter || wordCells[index].IsLocked) return;
            HapticsHelper.PlaySoftImpact();
            wordCells[index].ClearLetter();
            grid.RemoveLetter(index);
        }

        private void OnSubmit()
        {
            if (isAnimating) return;
            if (!grid.Puzzle.IsFullyFilled())
            {
                ShakeEmptyCells();
                return;
            }

            isAnimating = true;
            if (submitButton != null)
                submitButton.interactable = false;
            StartCoroutine(CheckWordCoroutine());
        }

        private void ShakeEmptyCells()
        {
            float delay = 0;
            for (int i = 0; i < wordLength; i++)
            {
                if (!wordCells[i].HasLetter)
                {
                    var cell = wordCells[i];
                    DOVirtual.DelayedCall(delay, () =>
                    {
                        cell.transform.DOShakePosition(0.3f, new Vector3(5, 0, 0), 10, 90, false, false);
                    });
                    delay += 0.05f;
                }
            }
        }

        private IEnumerator CheckWordCoroutine()
        {
            string guess = grid.Puzzle.GetCurrentGuess();
            string url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{guess.ToLowerInvariant()}";

            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                float delay = 0;
                for (int i = 0; i < wordLength; i++)
                {
                    var cell = wordCells[i];
                    DOVirtual.DelayedCall(delay, () =>
                    {
                        cell.transform.DOShakePosition(0.3f, new Vector3(5, 0, 0), 10, 90, false, false);
                    });
                    delay += 0.05f;
                }
                isAnimating = false;
                if (submitButton != null)
                    submitButton.interactable = true;
                yield break;
            }

            grid.Submit();
        }

        private void HandleAttemptComplete(int attempt, TileState[] results)
        {
            float delay = 0;
            for (int i = 0; i < wordLength; i++)
            {
                int idx = i;
                var cell = wordCells[i];
                var state = results[i];

                DOVirtual.DelayedCall(delay, () =>
                {
                    cell.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 2, 0.5f);
                    cell.SetState(state);
                });
                delay += 0.15f;
            }

            DOVirtual.DelayedCall(delay + 0.5f, () =>
            {
                isAnimating = false;
                if (submitButton != null)
                    submitButton.interactable = true;
                UpdateAttempts();
                foreach (var tile in letterTiles)
                {
                    if (grid.IsLetterWrongInAlphabet(tile.Letter))
                        tile.SetWrong(WordGridGame.ActiveSettings.WrongColor);
                }
            });
        }

        private void HandleSolved()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            OnSolved?.Invoke();

            float delay = 0;
            for (int i = 0; i < wordLength; i++)
            {
                int idx = i;
                var cell = wordCells[i];
                DOVirtual.DelayedCall(delay, () =>
                {
                    cell.transform.DOKill();
                    cell.transform.localScale = Vector3.one;
                    cell.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.5f)
                        .SetEase(Ease.OutQuad);
                });
                delay += 0.1f;
            }

            DOVirtual.DelayedCall(delay + 0.5f, () =>
            {
                if (submitButton != null)
                    submitButton.gameObject.SetActive(false);
            });
        }

        private void UpdateAttempts()
        {
            if (attemptsText != null)
                attemptsText.text = $"Attempts: {grid.Attempts}";
        }

        protected override void ResetPuzzle()
        {
            if (grid == null) return;
            grid.ResetPuzzle();
            foreach (var cell in wordCells)
            {
                if (cell != null && !cell.IsLocked) cell.ResetCell();
            }
            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(true);
                submitButton.interactable = true;
            }

            UpdateAttempts();
        }

        private void OnControllerCreated(WordGridController controller)
        {
            Setup(controller);
        }

        private void CleanupGrid()
        {
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

            if (wordTilesParent != null)
            {
                for (int i = wordTilesParent.childCount - 1; i >= 0; i--)
                    Destroy(wordTilesParent.GetChild(i).gameObject);
            }

            if (alphabetParent != null)
            {
                for (int i = alphabetParent.childCount - 1; i >= 0; i--)
                    Destroy(alphabetParent.GetChild(i).gameObject);
            }

            if (grid != null)
            {
                grid.OnSolved -= HandleSolved;
                grid.OnAttemptComplete -= HandleAttemptComplete;
                grid = null;
            }

            if (submitButton != null)
                submitButton.onClick.RemoveListener(OnSubmit);

            isAnimating = false;
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            CleanupGrid();
            WordGridGame.OnControllerCreated -= OnControllerCreated;
        }
    }
}
