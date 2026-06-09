using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gamio.Features.UI
{
    public class CarouselItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public int Index { get; set; }
        public float SwipeThreshold { get; set; } = 50f;
        public bool SwipeToChange { get; set; }

        [SerializeField] private List<GameObject> _enabledObjects = new();
        public List<GameObject> EnabledObjects => _enabledObjects;

        public event Action OnFocus;
        public event Action OnLoseFocus;
        public event Action OnAppear;
        public event Action OnDisappear;

        public event Action OnSwipeNext;
        public event Action OnSwipePrevious;

        private bool _isFocused;
        private bool _isVisible;
        private Vector2 _dragStartPos;
        private bool _isDragging;

        public void SetFocused(bool focused)
        {
            if (focused == _isFocused) return;
            _isFocused = focused;

            foreach (var obj in _enabledObjects)
            {
                if (obj != null)
                    obj.SetActive(focused);
            }

            if (focused)
                OnFocus?.Invoke();
            else
                OnLoseFocus?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (visible == _isVisible) return;
            _isVisible = visible;
            if (visible)
                OnAppear?.Invoke();
            else
                OnDisappear?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!SwipeToChange || !isActiveAndEnabled) return;
            _dragStartPos = eventData.position;
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!SwipeToChange || !_isDragging) return;
            _isDragging = false;

            var delta = eventData.position.x - _dragStartPos.x;
            if (Mathf.Abs(delta) >= SwipeThreshold)
            {
                if (delta < 0)
                    OnSwipeNext?.Invoke();
                else
                    OnSwipePrevious?.Invoke();
            }
        }
    }
}
