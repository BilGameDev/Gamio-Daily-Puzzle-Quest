using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace Gamio.Games.LineConnect
{
    [RequireComponent(typeof(Image))]
    public class LineConnectCellItem : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        [NonSerialized] public int Row;
        [NonSerialized] public int Col;
        [NonSerialized] public int ColorId;
        [NonSerialized] public bool IsEndpoint;

        private Image image;

        public event Action<int, int> OnPointerDownEvent;
        public event Action<int, int> OnPointerEnterEvent;
        public event Action OnPointerUpEvent;

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
        }

        public void Init(int row, int col, int colorId, bool isEndpoint)
        {
            Row = row;
            Col = col;
            ColorId = colorId;
            IsEndpoint = isEndpoint;
            Image.color = new Color(0.15f, 0.15f, 0.17f);
            transform.localScale = Vector3.zero;
            float delay = (row * 10 + col) * 0.02f;
            transform.DOScale(Vector3.one, 0.3f).SetDelay(delay).SetEase(Ease.OutBack);
        }

        private Tweener colorTween;

        public void SetColor(Color color)
        {
            colorTween?.Kill();
            colorTween = Image.DOColor(color, 0.2f);
        }

        public void OnPointerDown(PointerEventData eventData) => OnPointerDownEvent?.Invoke(Row, Col);
        public void OnPointerEnter(PointerEventData eventData) => OnPointerEnterEvent?.Invoke(Row, Col);
        public void OnPointerUp(PointerEventData eventData) => OnPointerUpEvent?.Invoke();
    }
}
