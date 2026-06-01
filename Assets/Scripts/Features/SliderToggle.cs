using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Gamio.UI
{
    public class SliderToggle : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RectTransform knob;
        [SerializeField] private Image background;
        [SerializeField] private Color onColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color offColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private float duration = 0.15f;

        public System.Action<bool> OnValueChanged;
        public bool isOn { get; private set; }

        private float _knobRange;
        private Tweener _tween;

        private void Awake()
        {
            CalculateRange();
        }

        private void CalculateRange()
        {
            var rect = (RectTransform)transform;
            float knobWidth = knob.rect.width * knob.localScale.x;
            _knobRange = (rect.rect.width - knobWidth) * 0.5f;
        }

        public void SetIsOn(bool value, bool instant = false)
        {
            isOn = value;
            _tween?.Kill();

            float targetX = value ? _knobRange : -_knobRange;
            if (instant)
            {
                knob.anchoredPosition = new Vector2(targetX, 0f);
                background.color = value ? onColor : offColor;
            }
            else
            {
                _tween = knob.DOAnchorPosX(targetX, duration).SetEase(Ease.OutCubic);
                background.DOColor(value ? onColor : offColor, duration);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SetIsOn(!isOn);
            OnValueChanged?.Invoke(isOn);
        }
    }
}
