using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Gamio.Features.Popup
{
    public class ErrorPopupUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;

        private System.Action onConfirm;

        public static ErrorPopupUI Show(string message, string buttonText = "OK", System.Action onConfirmAction = null)
        {
            var prefab = Resources.Load<ErrorPopupUI>("Popups/ErrorPopupCanvas");
            ErrorPopupUI popup;

            if (prefab != null)
            {
                popup = Instantiate(prefab);
            }
            else
            {
                popup = new GameObject("ErrorPopupCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                    .AddComponent<ErrorPopupUI>();
                popup.CreateDefaultUI();
            }

            popup.messageText.text = message;
            popup.confirmButtonText.text = buttonText;
            popup.onConfirm = onConfirmAction;
            popup.AnimateIn();
            return popup;
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

            confirmButton.onClick.AddListener(OnConfirm);
        }

        private void OnConfirm()
        {
            var action = onConfirm;
            Close();
            action?.Invoke();
        }

        public void Close()
        {
            confirmButton.onClick.RemoveAllListeners();

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

        private void CreateDefaultUI()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var overlay = new GameObject("Overlay", typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(transform, false);
            var rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var img = overlay.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.5f);
            overlayGroup = overlay.GetComponent<CanvasGroup>();

            var panelObj = new GameObject("Panel", typeof(Image), typeof(CanvasGroup));
            panelObj.transform.SetParent(transform, false);
            var prt = panelObj.GetComponent<RectTransform>();
            prt.sizeDelta = new Vector2(600, 300);
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            var pimg = panelObj.GetComponent<Image>();
            pimg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            panelGroup = panelObj.GetComponent<CanvasGroup>();

            var msgObj = new GameObject("Message", typeof(TextMeshProUGUI));
            msgObj.transform.SetParent(panelObj.transform, false);
            var mrt = msgObj.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0, 0.3f);
            mrt.anchorMax = new Vector2(1, 0.85f);
            mrt.offsetMin = new Vector2(20, 0);
            mrt.offsetMax = new Vector2(-20, 0);
            messageText = msgObj.GetComponent<TextMeshProUGUI>();
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.fontSize = 28;
            messageText.color = Color.white;

            var btnObj = new GameObject("ConfirmButton", typeof(Image), typeof(Button));
            btnObj.transform.SetParent(panelObj.transform, false);
            var brt = btnObj.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(200, 50);
            brt.anchorMin = new Vector2(0.5f, 0.1f);
            brt.anchorMax = new Vector2(0.5f, 0.1f);
            brt.anchoredPosition = Vector2.zero;
            var bimg = btnObj.GetComponent<Image>();
            bimg.color = new Color(0.2f, 0.5f, 0.9f, 1f);
            confirmButton = btnObj.GetComponent<Button>();

            var btnTextObj = new GameObject("Text", typeof(TextMeshProUGUI));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var trt = btnTextObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            confirmButtonText = btnTextObj.GetComponent<TextMeshProUGUI>();
            confirmButtonText.alignment = TextAlignmentOptions.Center;
            confirmButtonText.fontSize = 22;
            confirmButtonText.color = Color.white;
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
