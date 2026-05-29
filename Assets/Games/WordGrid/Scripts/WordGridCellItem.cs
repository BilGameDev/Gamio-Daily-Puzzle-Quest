using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Gamio.Games.WordGrid
{
    [RequireComponent(typeof(Image))]
    public class WordGridCellItem : MonoBehaviour, IPointerClickHandler
    {
        [NonSerialized] public int Index;
        [NonSerialized] public char TargetLetter;

        private Image image;
        private TextMeshProUGUI letterText;
        private char currentLetter;

        public char CurrentLetter => currentLetter;
        public bool HasLetter => currentLetter != '\0';
        public bool IsLocked { get; private set; }

        public event Action<int> OnClicked;

        public Image Image
        {
            get
            {
                if (image == null) image = GetComponent<Image>();
                return image;
            }
        }

        private void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = true;

            letterText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (letterText == null)
            {
                var textGO = new GameObject("Letter", typeof(RectTransform));
                textGO.transform.SetParent(transform, false);
                var textRT = textGO.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
                letterText = textGO.AddComponent<TextMeshProUGUI>();
                letterText.fontSize = 40;
                letterText.alignment = TextAlignmentOptions.Center;
                letterText.color = Color.white;
                if (TMPro.TMP_Settings.defaultFontAsset != null)
                    letterText.font = TMPro.TMP_Settings.defaultFontAsset;
                letterText.raycastTarget = false;
            }
            else
            {
                letterText.raycastTarget = false;
            }
        }

        public void Init(int index, char targetLetter)
        {
            Index = index;
            TargetLetter = targetLetter;
            currentLetter = '\0';
            IsLocked = false;
            if (letterText != null)
                letterText.gameObject.SetActive(false);
            Image.color = WordGridGame.ActiveSettings.CellDefaultColor;
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.3f).SetDelay(index * 0.1f).SetEase(Ease.OutBack);
        }

        public void SetLetter(char letter)
        {
            if (IsLocked) return;
            currentLetter = letter;
            if (letterText != null)
            {
                letterText.text = letter.ToString();
                letterText.gameObject.SetActive(true);
                letterText.color = Color.white;
            }
            Image.color = WordGridGame.ActiveSettings.CellFilledColor;
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 2, 0.5f);
        }

        public void ClearLetter()
        {
            if (IsLocked) return;
            if (letterText != null)
            {
                letterText.DOFade(0f, 0.25f).OnComplete(() =>
                {
                    letterText.gameObject.SetActive(false);
                    letterText.color = Color.white;
                });
            }
            currentLetter = '\0';
            Image.color = WordGridGame.ActiveSettings.CellDefaultColor;
        }

        public void SetState(TileState state)
        {
            switch (state)
            {
                case TileState.Correct:
                    IsLocked = true;
                    Image.DOColor(WordGridGame.ActiveSettings.CorrectColor, 0.3f);
                    if (letterText != null)
                        letterText.DOColor(Color.white, 0.3f);
                    break;
                case TileState.WrongPosition:
                    Image.DOColor(WordGridGame.ActiveSettings.WrongPositionColor, 0.3f);
                    if (letterText != null)
                        letterText.DOColor(Color.white, 0.3f);
                    break;
                case TileState.Wrong:
                    Image.DOColor(WordGridGame.ActiveSettings.WrongColor, 0.3f);
                    if (letterText != null)
                        letterText.DOColor(Color.white, 0.3f);
                    break;
            }
        }

        public void SetHint(char letter)
        {
            if (letterText != null)
            {
                letterText.text = letter.ToString();
                letterText.gameObject.SetActive(true);
                letterText.color = new Color(0.2f, 0.8f, 0.2f);
                letterText.fontStyle = FontStyles.Bold;
            }
            Image.color = new Color(0.15f, 0.4f, 0.15f);
        }

        public void ClearHint()
        {
            if (letterText != null)
            {
                letterText.text = "";
                letterText.gameObject.SetActive(false);
                letterText.color = Color.white;
                letterText.fontStyle = FontStyles.Normal;
            }
            Image.color = WordGridGame.ActiveSettings.CellDefaultColor;
        }

        public void ResetCell()
        {
            if (letterText != null)
            {
                letterText.DOKill();
                letterText.gameObject.SetActive(false);
                letterText.color = Color.white;
            }
            currentLetter = '\0';
            IsLocked = false;
            Image.color = WordGridGame.ActiveSettings.CellDefaultColor;
            transform.DOKill();
            transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke(Index);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
