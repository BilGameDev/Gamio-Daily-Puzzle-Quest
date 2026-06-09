using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Gamio.Games.Sudoku
{
    [RequireComponent(typeof(Image))]
    public class SudokuCellItem : MonoBehaviour, IPointerClickHandler
    {
        [NonSerialized] public int Row;
        [NonSerialized] public int Col;

        private Image image;
        private TextMeshProUGUI valueText;
        private Sequence violationSeq;

        public event Action<int, int> OnClickEvent;

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
            valueText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Init(int row, int col, bool isGiven)
        {
            Row = row;
            Col = col;
            SetValue(0, isGiven);
        }

        public void SetValue(int value, bool isGiven)
        {
            if (valueText != null)
            {
                if (value > 0)
                {
                    valueText.text = value.ToString();
                    valueText.gameObject.SetActive(true);
                    valueText.fontStyle = isGiven
                        ? FontStyles.Bold
                        : FontStyles.Normal;
                }
                else
                {
                    valueText.gameObject.SetActive(false);
                }
            }
        }

        public void PlayViolationAnimation()
        {
            StopViolationAnimation();
            var originalColor = Image.color;
            Image.color = Color.red;
            violationSeq = DOTween.Sequence();
            violationSeq.Append(Image.DOColor(new Color(0.8f, 0.2f, 0.2f), 0.15f));
            violationSeq.Append(Image.DOColor(Color.red, 0.15f));
            violationSeq.SetLoops(3);
            violationSeq.OnComplete(() => Image.color = originalColor);
        }

        public void StopViolationAnimation()
        {
            if (violationSeq != null)
            {
                violationSeq.Kill();
                violationSeq = null;
            }
        }

        public void FlashText()
        {
            if (valueText == null || !valueText.gameObject.activeSelf) return;
            var defaultColor = valueText.color;
            valueText.color = Color.red;
            DOVirtual.DelayedCall(0.4f, () => { if (valueText != null) valueText.color = defaultColor; });
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickEvent?.Invoke(Row, Col);
        }
    }
}
