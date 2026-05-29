using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gamio.Games.Arrows
{
    [RequireComponent(typeof(Image))]
    public class ArrowsCellItem : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI arrowGraphic;

        [NonSerialized] public int Row;
        [NonSerialized] public int Col;
        [NonSerialized] public ArrowDirection Direction;
        [NonSerialized] public bool IsEmpty;
        [NonSerialized] public bool IsObstacle;

        private Image image;
        private RectTransform rectTransform;
        private Color tileColor = new Color(0.25f, 0.30f, 0.45f);
        private Color obstacleColor = new Color(0.12f, 0.12f, 0.14f);
        private Color arrowColor = Color.white;
        private Color flashColor = Color.red;
        private float flashDuration = 0.1f;
        private int flashLoops = 2;

        public Image Image
        {
            get
            {
                if (image == null) image = GetComponent<Image>();
                return image;
            }
        }

        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
                return rectTransform;
            }
        }

        public event Action<int, int> OnClick;

        private void Awake()
        {
            image = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void Init(int row, int col, ArrowDirection dir, bool isEmpty, bool isObstacle = false)
        {
            var settings = ArrowsGame.ActiveSettings;
            if (settings != null)
            {
                tileColor = settings.tileColor;
                obstacleColor = settings.obstacleColor;
                arrowColor = settings.tileArrowColor;
                flashColor = settings.flashColor;
                flashDuration = settings.flashDuration;
                flashLoops = settings.flashLoopCount;
            }

            Row = row;
            Col = col;
            Direction = dir;
            IsEmpty = isEmpty;
            IsObstacle = isObstacle;

            if (isEmpty && !isObstacle)
            {
                SetVisible(false);
                SetBlockRaycasts(false);
                return;
            }

            SetVisible(true);
            SetBlockRaycasts(!isObstacle);

            if (isObstacle)
            {
                Image.color = obstacleColor;
                if (arrowGraphic != null)
                {
                    arrowGraphic.color = Color.clear;
                    arrowGraphic.rectTransform.localRotation = Quaternion.identity;
                }
                return;
            }

            Image.color = tileColor;
            SetArrowDirection(dir);
        }

        public void SetVisible(bool visible)
        {
            if (IsObstacle)
            {
                Image.color = visible ? obstacleColor : Color.clear;
                return;
            }
            Image.color = visible ? tileColor : Color.clear;
            if (arrowGraphic != null)
                arrowGraphic.color = visible ? arrowColor : Color.clear;
        }

        public bool IsVisible()
        {
            return Image.color.a > 0.1f;
        }

        public void SetBlockRaycasts(bool block)
        {
            Image.raycastTarget = block;
        }

        public void SetArrowDirection(ArrowDirection dir)
        {
            if (arrowGraphic == null) return;
            arrowGraphic.rectTransform.localRotation = dir switch
            {
                ArrowDirection.Up => Quaternion.Euler(0, 0, 270),
                ArrowDirection.Right => Quaternion.Euler(0, 0, 180),
                ArrowDirection.Down => Quaternion.Euler(0, 0, 90),
                _ => Quaternion.Euler(0, 0, 0)
            };
        }

        public void Flash()
        {
            if (arrowGraphic == null || IsObstacle) return;
            arrowGraphic.DOKill(true);
            arrowGraphic.color = arrowColor;
            arrowGraphic.DOColor(flashColor, flashDuration)
                .SetLoops(flashLoops, LoopType.Yoyo).SetEase(Ease.InOutSine)
                .OnComplete(() => arrowGraphic.color = arrowColor);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsEmpty && IsVisible() && !IsObstacle)
                OnClick?.Invoke(Row, Col);
        }
    }
}
