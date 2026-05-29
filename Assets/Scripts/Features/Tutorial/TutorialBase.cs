using System;
using System.Collections.Generic;
using DG.Tweening;
using Gamio.Core;
using Gamio.Features.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.Tutorial
{
    public abstract class TutorialBase : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private RectTransform dotContainer;
        [SerializeField] private GameObject dotPrefab;
        [SerializeField] private Button skipButton;

        [Header("Strings")]
        [SerializeField] private string skipPopupTitle = "Skip Tutorial";
        [SerializeField] private string skipPopupMessage = "Skip the tutorial and start the game?";

        [Header("Colors")]
        [SerializeField] private Color activeDotColor = Color.white;
        [SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.3f);

        private Tween textTween;
        private readonly List<Image> dots = new List<Image>();
        private int currentStep;

        protected virtual void Start()
        {
            if (skipButton == null)
            {
                Debug.LogWarning($"[TutorialBase] skipButton not assigned on {name}");
                return;
            }
            skipButton.onClick.AddListener(SkipTutorial);
        }

        public void ShowInstruction(string text)
        {
            if (instructionText == null) return;

            textTween?.Kill();
            instructionText.text = text;
            instructionText.alpha = 1f;

            if (overlayGroup != null)
                overlayGroup.alpha = 1f;
        }

        public void ShowTemporary(string text, float duration, Action onComplete = null)
        {
            ShowInstruction(text);
            textTween = DOVirtual.DelayedCall(duration, () => onComplete?.Invoke());
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

        public void Show()
        {
            gameObject.SetActive(true);
        }

        void SkipTutorial()
        {
            PopupUI.Show(skipPopupTitle, skipPopupMessage,
                onConfirm: GamioEvents.RequestSkipTutorial,
                onCancel: null,
                confirmLabel: "Skip",
                cancelLabel: "Cancel");
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

        public virtual void Begin() { }

        protected void End()
        {
            Hide();
        }

        protected void FadeOutPanel(GameObject panel, float duration, Action onComplete)
        {
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.DOFade(0f, duration).OnComplete(() =>
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                panel.SetActive(false);
                onComplete?.Invoke();
            });
        }

        protected virtual void OnDestroy()
        {
            textTween?.Kill();
            skipButton.onClick.RemoveAllListeners();
        }
    }
}