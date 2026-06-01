using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Gamio.Games.Kings
{
    [RequireComponent(typeof(Image))]
    public class KingsCellItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [NonSerialized] public int Row;
        [NonSerialized] public int Col;
        [NonSerialized] public int SectionIndex;

        [Header("References")]
        [SerializeField] private GameObject kingIcon;
        [SerializeField] private GameObject nullIcon;

        [Header("Colors")]
        [SerializeField] private Color nullColor = new Color(0.25f, 0.25f, 0.3f, 0.8f);
        [SerializeField] private Color kingColor = new Color(0.95f, 0.75f, 0.15f, 1f);
        [Header("Input")]
        [SerializeField] private float holdDuration = 0.35f;

        private Image image;
        private Sequence tapSeq;
        private Color sectionColor;
        private Coroutine holdRoutine;
        private bool isHolding;
        private int pointerId = -1;

        public event Action<int, int> OnTap;
        public event Action<int, int> OnHold;

        public Image Image
        {
            get
            {
                if (image == null) image = GetComponent<Image>();
                return image;
            }
        }

        public Color SectionColor
        {
            get => sectionColor;
            set
            {
                sectionColor = value;
                Image.color = value;
            }
        }

        public void Init(int row, int col, int sectionIndex, Color sectionColor)
        {
            Row = row;
            Col = col;
            SectionIndex = sectionIndex;
            SectionColor = sectionColor;
        }

        public void SetState(KingsCellState state)
        {
            switch (state)
            {
                case KingsCellState.Empty:
                    if (nullIcon != null)
                        nullIcon.SetActive(false);
                    if (kingIcon != null)
                        kingIcon.SetActive(false);
                    break;

                case KingsCellState.Null:
                    if (nullIcon != null)
                        nullIcon.SetActive(true);
                    if (kingIcon != null)
                        kingIcon.SetActive(false);
                    break;

                case KingsCellState.King:
                    if (kingIcon != null)
                        kingIcon.SetActive(true);
                    if (nullIcon != null)
                        nullIcon.SetActive(false);
                    break;
            }
        }

        public void PlayTapAnimation()
        {
            tapSeq?.Kill();
            tapSeq = DOTween.Sequence();
            tapSeq.Append(transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 4, 0.5f));
        }

        public void PlayAutoFillAnimation()
        {
            tapSeq?.Kill();
            tapSeq = DOTween.Sequence();
            tapSeq.Append(transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 2, 0.5f));
        }

        public void PlayInvalidAnimation()
        {
            tapSeq?.Kill();
            tapSeq = DOTween.Sequence();
            tapSeq.Append(Image.DOColor(Color.red, 0.15f));
            tapSeq.Append(Image.DOColor(sectionColor, 0.3f));
        }

        public void PlayHintAnimation()
        {
            tapSeq?.Kill();
            tapSeq = DOTween.Sequence();
            tapSeq.Append(Image.DOColor(Color.green, 0.2f));
            tapSeq.Append(Image.DOColor(sectionColor, 0.4f));
        }

        public void PlaySolvedAnimation(float delay)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5, 0.5f)
                .SetDelay(delay).SetEase(Ease.OutQuad);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerId = eventData.pointerId;
            isHolding = false;
            if (holdRoutine != null)
                StopCoroutine(holdRoutine);
            holdRoutine = StartCoroutine(HoldRoutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            if (holdRoutine != null)
            {
                StopCoroutine(holdRoutine);
                holdRoutine = null;
            }
            if (!isHolding)
                OnTap?.Invoke(Row, Col);
            isHolding = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (holdRoutine != null)
            {
                StopCoroutine(holdRoutine);
                holdRoutine = null;
            }
            isHolding = false;
        }

        private IEnumerator HoldRoutine()
        {
            yield return new WaitForSeconds(holdDuration);
            isHolding = true;
            OnHold?.Invoke(Row, Col);
        }

        private void OnDestroy()
        {
            if (holdRoutine != null)
                StopCoroutine(holdRoutine);
        }
    }
}