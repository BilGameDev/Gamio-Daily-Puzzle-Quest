using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Gamio.Games.Hitori
{
    [RequireComponent(typeof(Image))]
    public class HitoriCellItem : MonoBehaviour, IPointerDownHandler
    {
        [NonSerialized] public int Row;
        [NonSerialized] public int Col;
        [NonSerialized] public int Number;

        private Image image;
        private TextMeshProUGUI numberText;
        private Sequence tapSeq;
        private Sequence violationSeq;
        private Tweener imageColorTween;
        private Tweener textColorTween;

        public event Action<int, int> OnClick;

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
            numberText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Init(int row, int col, int number)
        {
            Row = row;
            Col = col;
            Number = number;

            if (numberText != null)
            {
                numberText.text = number.ToString();
                numberText.gameObject.SetActive(true);
            }
        }

        public void SetVisual(HitoriCellState state, Color defaultColor)
        {
            StopViolationAnimation();
            imageColorTween?.Kill();
            textColorTween?.Kill();

            switch (state)
            {
                case HitoriCellState.None:
                    imageColorTween = Image.DOColor(defaultColor, 0.2f);
                    if (numberText != null)
                    {
                        textColorTween = numberText.DOColor(Color.white, 0.2f);
                        numberText.gameObject.SetActive(true);
                    }
                    break;
                case HitoriCellState.Black:
                    imageColorTween = Image.DOColor(Color.black, 0.2f);
                    if (numberText != null)
                    {
                        textColorTween = numberText.DOColor(Color.white, 0.2f);
                        numberText.gameObject.SetActive(true);
                    }
                    break;
                case HitoriCellState.White:
                    imageColorTween = Image.DOColor(Color.white, 0.2f);
                    if (numberText != null)
                    {
                        textColorTween = numberText.DOColor(Color.black, 0.2f);
                        numberText.gameObject.SetActive(true);
                    }
                    break;
            }

        }

        public void PlayTapAnimation()
        {
            tapSeq?.Kill();
            tapSeq = DOTween.Sequence();
            tapSeq.Append(transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 0.5f));
        }

        public void PlayViolationAnimation()
        {
            if (numberText == null) return;
            textColorTween?.Kill();
            StopViolationAnimation();
            numberText.color = Color.red;
            violationSeq = DOTween.Sequence();
            violationSeq.Append(numberText.DOColor(new Color(0.4f, 0f, 0f), 0.5f).SetEase(Ease.InOutSine));
            violationSeq.Append(numberText.DOColor(Color.red, 0.5f).SetEase(Ease.InOutSine));
            violationSeq.SetLoops(-1);
        }

        private void StopViolationAnimation()
        {
            if (violationSeq != null)
            {
                violationSeq.Kill();
                violationSeq = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData) => OnClick?.Invoke(Row, Col);
    }
}
