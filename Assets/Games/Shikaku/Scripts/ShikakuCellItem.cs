using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Gamio.Games.Shikaku
{
    [RequireComponent(typeof(Image))]
    public class ShikakuCellItem : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        [NonSerialized] public int Row;
        [NonSerialized] public int Col;
        [NonSerialized] public int? Number;

        private Image image;
        private TextMeshProUGUI numberText;

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
            numberText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Init(int row, int col, int? number)
        {
            Row = row;
            Col = col;
            Number = number;

            if (numberText != null)
            {
                if (number.HasValue)
                {
                    numberText.text = number.Value.ToString();
                    numberText.gameObject.SetActive(true);
                }
                else
                {
                    numberText.gameObject.SetActive(false);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData) => OnPointerDownEvent?.Invoke(Row, Col);
        public void OnPointerEnter(PointerEventData eventData) => OnPointerEnterEvent?.Invoke(Row, Col);
        public void OnPointerUp(PointerEventData eventData) => OnPointerUpEvent?.Invoke();
    }
}
