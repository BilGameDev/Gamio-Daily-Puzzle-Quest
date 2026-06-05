using System;
using System.Collections;
using DG.Tweening;
using Gamio.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.DailyChallenge
{
    public class ChallengePopupUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private TextMeshProUGUI gameTypeText;
        [SerializeField] private Button beginButton;
        [SerializeField] private TextMeshProUGUI beginButtonLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform gameIconHolder;

        private string gameType;

        public event Action OnBeginRequested;
        public event Action OnCloseRequested;

        public static ChallengePopupUI Show(Transform parent, string gameType)
        {
            var prefab = Resources.Load<ChallengePopupUI>("Popups/ChallengePopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("ChallengePopupUI prefab not found at Resources/Popups/ChallengePopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab, parent);
            popup.gameType = gameType;
            popup.Initialize();
            popup.AnimateIn();
            return popup;
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

            if (gameIconHolder != null)
                gameIconHolder.localScale = Vector3.zero;

            if (beginButton != null)
            {
                beginButton.gameObject.SetActive(false);
                beginButton.onClick.AddListener(OnBeginClicked);
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(Dismiss);
        }

        private void Initialize()
        {
            // if (gameTypeText != null)
            //     gameTypeText.text = gameType;

            // var gameIcon = GamioAppContext.Get<GamesLibrary>()?.GetGameIcon(gameType);
            // if (gameIcon != null && gameIconHolder != null)
            //     Instantiate(gameIcon, gameIconHolder);
        }

        public void AnimateIn()
        {
            if (overlayGroup != null)
            {
                overlayGroup.DOFade(1f, 0.3f);
                StartCoroutine(ShowPanel());
            }
            else
            {
                ShowBeginButton();
            }
        }

        private IEnumerator ShowPanel()
        {
            yield return new WaitForSeconds(0.15f);

            if (panelGroup != null)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(panelGroup.DOFade(1f, 0.35f));
                seq.Join(panelGroup.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
                seq.Join(gameIconHolder.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetDelay(.3f));
                yield return seq.WaitForCompletion();
            }

            yield return new WaitForSeconds(0.1f);

            ShowBeginButton();
        }

        private void ShowBeginButton()
        {
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

        private bool dismissing;

        public void Dismiss()
        {
            if (dismissing) return;
            dismissing = true;

            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (beginButton != null) beginButton.onClick.RemoveAllListeners();

            var seq = DOTween.Sequence();
            if (overlayGroup != null)
                seq.Join(overlayGroup.DOFade(0f, 0.15f));
            if (panelGroup != null)
                seq.Join(panelGroup.DOFade(0f, 0.15f));
            seq.OnComplete(() =>
            {
                OnCloseRequested?.Invoke();
                if (this != null)
                    Destroy(gameObject);
            });
        }

        private void OnBeginClicked()
        {
            if (beginButton != null) beginButton.interactable = false;
            OnBeginRequested?.Invoke();
            Dismiss();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            StopAllCoroutines();
        }
    }
}
