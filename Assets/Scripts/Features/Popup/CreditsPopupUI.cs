using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Gamio.Features.Popup
{
    public class CreditsPopupUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private Button closeButton;

        public static CreditsPopupUI Show()
        {
            var prefab = Resources.Load<CreditsPopupUI>("Popups/CreditsPopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("CreditsPopupUI prefab not found at Resources/Popups/CreditsPopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab);
            popup.Setup();
            return popup;
        }

        private void Setup()
        {
            closeButton.onClick.AddListener(Close);
            AnimateIn();
        }

        private void AnimateIn()
        {
            if (overlayGroup != null)
                overlayGroup.DOFade(1f, 0.2f).From(0f);

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.transform.localScale = Vector3.one * 0.8f;
                panelGroup.DOFade(1f, 0.2f);
                panelGroup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
        }

        public void Close()
        {
            closeButton.onClick.RemoveAllListeners();

            var seq = DOTween.Sequence();
            if (overlayGroup != null)
                seq.Join(overlayGroup.DOFade(0f, 0.15f));
            if (panelGroup != null)
                seq.Join(panelGroup.DOFade(0f, 0.15f));
            seq.OnComplete(() =>
            {
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
