using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.UI
{
    public class CountdownUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private CanvasGroup _group;

        private const float Interval = 1f;

        public static IEnumerator Show(Transform parent)
        {
            var obj = new GameObject("ChallengeCountdown", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
                obj.transform.SetParent(parent, false);

            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var overlay = new GameObject("Overlay", typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(obj.transform, false);
            var rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var img = overlay.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            var overlayGroup = overlay.GetComponent<CanvasGroup>();
            overlayGroup.alpha = 0f;
            overlayGroup.DOFade(1f, 0.15f);

            var numberObj = new GameObject("Number", typeof(TextMeshProUGUI));
            numberObj.transform.SetParent(obj.transform, false);
            var nrt = numberObj.GetComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero;
            nrt.anchorMax = Vector2.one;
            nrt.sizeDelta = Vector2.zero;
            var numberText = numberObj.GetComponent<TextMeshProUGUI>();
            numberText.text = "3";
            numberText.fontSize = 160;
            numberText.alignment = TextAlignmentOptions.Center;
            numberText.color = Color.white;
            numberText.fontStyle = FontStyles.Bold;

            var countdown = obj.AddComponent<CountdownUI>();
            countdown._numberText = numberText;
            countdown._group = overlayGroup;

            yield return countdown.PlayRoutine();

            overlayGroup.DOFade(0f, 0.2f);
            yield return new WaitForSeconds(0.2f);
            Destroy(obj);
        }

        private IEnumerator PlayRoutine()
        {
            string[] steps = { "3", "2", "1", "GO!" };
            for (int i = 0; i < steps.Length; i++)
            {
                _numberText.text = steps[i];
                _numberText.transform.localScale = Vector3.one * 1.5f;
                _numberText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                _numberText.DOColor(i == steps.Length - 1 ? new Color(1f, 0.7f, 0.2f, 1f) : Color.white, 0.15f);

                yield return new WaitForSeconds(Interval);
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
