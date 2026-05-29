using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.Popup
{
    public class PopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private TextMeshProUGUI cancelButtonText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CanvasGroup overlayGroup;

        private Action onConfirm;
        private Action onCancel;

        public static PopupUI Show(string title, string message,
            Action onConfirm = null, Action onCancel = null,
            string confirmLabel = "Confirm", string cancelLabel = "Cancel")
        {
            var prefab = Resources.Load<PopupUI>("Popups/PopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("PopupUI prefab not found at Resources/Popups/PopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab);
            popup.Setup(title, message, onConfirm, onCancel, confirmLabel, cancelLabel);
            return popup;
        }

        private void Setup(string title, string message,
            Action onConfirmAction, Action onCancelAction,
            string confirmLabel, string cancelLabel)
        {
            if (titleText != null)
                titleText.text = title;

            if (messageText != null)
                messageText.text = message;

            if (confirmButtonText != null)
                confirmButtonText.text = confirmLabel;

            if (cancelButtonText != null)
                cancelButtonText.text = cancelLabel;

            onConfirm = onConfirmAction;
            onCancel = onCancelAction;

            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.DOFade(0.5f, 0.2f);
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.transform.localScale = Vector3.one * 0.8f;
                panelGroup.DOFade(1f, 0.2f);
                panelGroup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
        }

        public void Confirm()
        {
            Dismiss(onConfirm);
        }

        public void Cancel()
        {
            Dismiss(onCancel);
        }

        public void Dismiss(Action action)
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveAllListeners();
            if (cancelButton != null)
                cancelButton.onClick.RemoveAllListeners();

            var seq = DOTween.Sequence();
            if (overlayGroup != null)
                seq.Join(overlayGroup.DOFade(0f, 0.15f));
            if (panelGroup != null)
                seq.Join(panelGroup.DOFade(0f, 0.15f));
            seq.OnComplete(() =>
            {
                action?.Invoke();
                if (this != null)
                    Destroy(gameObject);
            });
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}