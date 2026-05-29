using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Gamio.Games.WordSearch
{
    [RequireComponent(typeof(Image))]
    public class WordSearchCellItem : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        [NonSerialized] public int Row;
        [NonSerialized] public int Col;
        [NonSerialized] public char Letter;

        private Image image;
        private TextMeshProUGUI letterText;
        private Color defaultTileColor;
        private Tweener colorTween;

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
            letterText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Init(int row, int col, char letter)
        {
            Row = row;
            Col = col;
            Letter = letter;

            if (letterText != null)
            {
                letterText.text = char.ToUpperInvariant(letter).ToString();
                letterText.gameObject.SetActive(true);
            }
        }

        public void SetTileColor(Color color)
        {
            defaultTileColor = color;
            if (image != null)
                image.color = color;
        }

        public void SetHighlight(bool highlighted, Color color)
        {
            if (image == null) return;
            colorTween?.Kill();
            colorTween = image.DOColor(highlighted ? color : defaultTileColor, 0.15f);
        }

        public void SetFound(Color color)
        {
            colorTween?.Kill();
            if (image != null)
                colorTween = image.DOColor(color, 0.2f);
        }

        public void OnPointerDown(PointerEventData eventData) => OnPointerDownEvent?.Invoke(Row, Col);
        public void OnPointerEnter(PointerEventData eventData) => OnPointerEnterEvent?.Invoke(Row, Col);
        public void OnPointerUp(PointerEventData eventData) => OnPointerUpEvent?.Invoke();
    }
}
