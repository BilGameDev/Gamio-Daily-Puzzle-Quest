using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.Tutorial
{
    public class TutorialUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private RectTransform dotContainer;
        [SerializeField] private GameObject dotPrefab;

        [Header("Colors")]
        [SerializeField] private Color activeDotColor = Color.white;
        [SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.3f);

        private Tween textTween;
        private readonly List<Image> dots = new List<Image>();
        private int currentStep;

        public void ShowInstruction(string text, float autoHideDelay = -1)
        {
            if (instructionText == null) return;

            textTween?.Kill();
            instructionText.text = text;
            instructionText.alpha = 0f;
            instructionText.DOFade(1f, 0.3f);

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.DOFade(0.6f, 0.3f);
            }

            if (autoHideDelay > 0)
            {
                textTween = instructionText.DOFade(0f, 0.4f).SetDelay(autoHideDelay);
            }
        }

        public void ShowTemporary(string text, float duration, System.Action onComplete = null)
        {
            ShowInstruction(text);
            textTween = DOVirtual.DelayedCall(duration, () =>
            {
                if (instructionText != null)
                {
                    instructionText.DOFade(0f, 0.3f).OnComplete(() => onComplete?.Invoke());
                }
            });
        }

        public void SetTotalSteps(int total)
        {
            if (dotContainer == null || dotPrefab == null) return;
            foreach (var d in dots)
                if (d != null) Destroy(d.gameObject);
            dots.Clear();
            for (int i = 0; i < total; i++)
            {
                var dot = Instantiate(dotPrefab, dotContainer).GetComponent<Image>();
                if (dot != null)
                {
                    dot.color = inactiveDotColor;
                    dots.Add(dot);
                }
            }
            currentStep = 0;
            if (dots.Count > 0)
                dots[0].color = activeDotColor;
        }

        public void SetCurrentStep(int step)
        {
            if (step < 0 || step >= dots.Count) return;
            if (currentStep >= 0 && currentStep < dots.Count)
                dots[currentStep].color = inactiveDotColor;
            currentStep = step;
            dots[currentStep].color = activeDotColor;
        }

        public void Hide()
        {
            textTween?.Kill();
            if (instructionText != null)
                instructionText.alpha = 0f;
            if (overlayGroup != null)
                overlayGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            textTween?.Kill();
        }
    }
}
