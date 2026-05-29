using System;
using System.Collections;
using DG.Tweening;
using Gamio.Core;
using Gamio.Core.Services;
using Gamio.Features.UI;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.DailyChallenge
{
    public class ChallengePopupUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI gameTypeText;
        [SerializeField] private Button beginButton;
        [SerializeField] private TextMeshProUGUI beginButtonLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private GameObject gameIcon;
        [SerializeField] private CanvasGroup compactGroup;
        [SerializeField] private TextMeshProUGUI compactTimerText;

        public static Action<string, float> OnBeginRequested;

        private string gameType;
        private float totalTime;
        private bool isCompact;
        private Func<float> timeProvider;
        private string lastTimeText;

        public static ChallengePopupUI Create(Transform parent, string gameType, float totalTime, Func<float> timeProvider = null)
        {
            var prefab = Resources.Load<ChallengePopupUI>("Popups/ChallengePopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("ChallengePopupUI prefab not found at Resources/Popups/ChallengePopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab, parent);
            popup.gameType = gameType;
            popup.totalTime = totalTime;
            popup.timeProvider = timeProvider;
            popup.Initialize();
            return popup;
        }

        public void Refresh(string newGameType, float newTotalTime, bool animateIn, Func<float> timeProvider = null)
        {
            gameType = newGameType;
            totalTime = newTotalTime;
            if (timeProvider != null) this.timeProvider = timeProvider;
            UpdateGameInfo();
            UpdateTimer();
            if (streakText != null)
            {
                streakText.text = $"Streak: {GamioAppContext.Get<GamioManager>().StreakCount}";
            }
            if (animateIn)
                StartCoroutine(ShowAnimation());
        }

        private void Awake()
        {
            if (overlayGroup != null)
                overlayGroup.alpha = 0f;
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.transform.localScale = Vector3.one * 0.92f;
            }
            if (compactGroup != null)
                compactGroup.alpha = 0f;
            if (beginButton != null)
            {
                beginButton.gameObject.SetActive(false);
                beginButton.onClick.AddListener(OnBeginClicked);
            }
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void Initialize()
        {
            UpdateGameInfo();
            UpdateTimer();
            if (streakText != null)
            {
                streakText.text = $"Streak: {GamioAppContext.Get<GamioManager>().StreakCount}";
            }
        }

        private void UpdateGameInfo()
        {
            if (gameTypeText != null)
                gameTypeText.text = gameType;
            if (gameIcon != null)
            {
                var iconImage = gameIcon.GetComponent<Image>();
                if (iconImage != null)
                {
                    if (GamioAppContext.Get<GamioManager>().DailyCompleted)
                        iconImage.color = Color.white;
                    else
                        iconImage.color = new Color(0.6f, 0.6f, 0.6f);
                }
            }
        }

        private void Update()
        {
            if (!isCompact || compactTimerText == null) return;

            if (timeProvider != null)
            {
                var text = FormatTime(timeProvider());
                if (text != lastTimeText)
                {
                    compactTimerText.text = text;
                    lastTimeText = text;
                }
            }
        }

        private void UpdateTimer()
        {
            if (timerText != null)
                timerText.text = FormatTime(totalTime);
        }

        private IEnumerator ShowAnimation()
        {
            if (overlayGroup != null)
            {
                overlayGroup.DOFade(1f, 0.3f);
                yield return new WaitForSeconds(0.15f);
            }

            if (panelGroup != null)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(panelGroup.DOFade(1f, 0.35f));
                seq.Join(panelGroup.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
                yield return seq.WaitForCompletion();
            }

            yield return new WaitForSeconds(0.1f);

            yield return new WaitForSeconds(0.15f);

            if (beginButton != null)
            {
                beginButton.gameObject.SetActive(true);
                beginButton.transform.localScale = Vector3.zero;
                beginButton.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
                beginButton.interactable = true;

                if (beginButtonLabel != null)
                    beginButtonLabel.text = "Begin Challenge";
            }
        }

        public void AnimateToCompact()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Selection);
            isCompact = true;
            if (overlayGroup != null)
            {
                overlayGroup.interactable = false;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.DOFade(0f, 0.2f);
            }
            if (panelGroup != null)
            {
                panelGroup.interactable = false;
                panelGroup.blocksRaycasts = false;
                panelGroup.DOFade(0f, 0.2f);
            }
            if (compactGroup != null)
            {
                compactGroup.alpha = 0f;
                compactGroup.gameObject.SetActive(true);
                compactGroup.DOFade(1f, 0.3f);
            }
        }

        public void AnimateToFull()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.LightImpact);
            isCompact = false;
            if (compactGroup != null)
                compactGroup.DOFade(0f, 0.15f);

            if (overlayGroup != null)
            {
                overlayGroup.DOFade(1f, 0.25f);
                overlayGroup.interactable = true;
                overlayGroup.blocksRaycasts = true;
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.transform.localScale = Vector3.one * 0.92f;
                panelGroup.DOFade(1f, 0.3f).SetDelay(0.1f);
                panelGroup.transform.DOScale(1f, 0.35f).SetDelay(0.1f).SetEase(Ease.OutBack);
                panelGroup.interactable = true;
                panelGroup.blocksRaycasts = true;
            }

            if (beginButton != null)
            {
                beginButton.gameObject.SetActive(true);
                beginButton.transform.localScale = Vector3.zero;
                beginButton.transform.DOScale(1f, 0.4f).SetDelay(0.2f).SetEase(Ease.OutBack);
                beginButton.interactable = true;

                if (beginButtonLabel != null)
                    beginButtonLabel.text = "Begin Challenge";
            }
        }

        public event Action OnCloseRequested;

        public void Close()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Selection);
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (beginButton != null) beginButton.onClick.RemoveAllListeners();
            var seq = DOTween.Sequence();
            if (overlayGroup != null)
                seq.Join(overlayGroup.DOFade(0f, 0.15f));
            if (panelGroup != null)
                seq.Join(panelGroup.DOFade(0f, 0.15f));
            if (compactGroup != null)
                seq.Join(compactGroup.DOFade(0f, 0.15f));
            seq.OnComplete(() =>
            {
                OnCloseRequested?.Invoke();
                if (this != null)
                    Destroy(gameObject);
            });
        }

        private void OnBeginClicked()
        {
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Selection);
            if (beginButton != null) beginButton.interactable = false;

            OnBeginRequested?.Invoke(gameType, totalTime);
            AnimateToCompact();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            StopAllCoroutines();
        }

        public static string FormatTime(float seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.Hours > 0)
                return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
