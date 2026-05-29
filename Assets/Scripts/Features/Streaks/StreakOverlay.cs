using System;
using System.Collections;
using DG.Tweening;
using Gamio.Features.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.Streaks
{
    public class StreakOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CanvasGroup carouselGroup;
        [SerializeField] private RectTransform fireImage;
        [SerializeField] private TextMeshProUGUI streakCountText;
        [SerializeField] private TextMeshProUGUI streakLabelText;
        [SerializeField] private Carousel daysCarousel;
        [SerializeField] private Button proceedButton;

        private const int DaysBefore = 3;

        public static StreakOverlay Show(int streakCount, Action onProceed = null)
        {
            var prefab = Resources.Load<StreakOverlay>("Popups/StreakOverlayCanvas");
            if (prefab == null)
            {
                Debug.LogError("StreakOverlay prefab not found at Resources/Popups/StreakOverlayCanvas");
                return null;
            }
            var overlay = Instantiate(prefab);
            overlay.Setup(streakCount, onProceed);
            return overlay;
        }

        private void Setup(int streakCount, Action onProceed)
        {
            var isNew = streakCount == 1;
            fireImage.localScale = isNew ? Vector3.zero : Vector3.one;
            streakCountText.transform.localScale = Vector3.zero;
            carouselGroup.alpha = 0;

            var today = DateTime.Today;
            var startOfStreak = today.AddDays(-(streakCount - 1));

            for (int i = 0; i < daysCarousel.transform.childCount; i++)
            {
                var child = daysCarousel.transform.GetChild(i);
                if (child.TryGetComponent(out StreakTile tile))
                {
                    var date = today.AddDays(i - DaysBefore);
                    var isCompleted = date >= startOfStreak && date <= today;
                    var isToday = i == DaysBefore;
                    tile.Setup(date, isCompleted, isToday);
                }
            }

            proceedButton.onClick.AddListener(() =>
            {
                onProceed?.Invoke();
                Dismiss();
            });
            proceedButton.gameObject.SetActive(false);
            streakLabelText.alpha = 0f;

            StartCoroutine(PlaySequence(streakCount, isNew));
        }

        private IEnumerator PlaySequence(int streakCount, bool isNew)
        {
            overlayGroup.alpha = 0f;
            panelGroup.alpha = 0f;
            panelGroup.transform.localScale = Vector3.one * 0.95f;

            overlayGroup.DOFade(0.5f, 0.4f);
            panelGroup.DOFade(1f, 0.5f);
            panelGroup.transform.DOScale(1f, 0.5f).SetEase(Ease.OutCubic);

            yield return new WaitForSeconds(0.6f);

            if (isNew)
            {
                fireImage.DOScale(new Vector3(1.3f, 0.3f, 1f), 0.3f).SetEase(Ease.OutSine)
                    .OnComplete(() => fireImage.DOScale(new Vector3(0.5f, 2.0f, 1f), 0.25f).SetEase(Ease.OutSine)
                        .OnComplete(() => fireImage.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.15f).SetEase(Ease.OutQuad)
                            .OnComplete(() => fireImage.DOScale(new Vector3(0.8f, 1.5f, 1f), 0.15f).SetEase(Ease.OutQuad)
                                .OnComplete(() => fireImage.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack, 3f)))));
            }
            else
            {
                fireImage.DOScale(new Vector3(1.5f, 0.4f, 1f), 0.25f).SetEase(Ease.OutSine)
                    .OnComplete(() => fireImage.DOScale(new Vector3(0.6f, 1.8f, 1f), 0.2f).SetEase(Ease.OutSine)
                        .OnComplete(() => fireImage.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack, 3f)));
            }

            yield return new WaitForSeconds(0.4f);

            streakCountText.text = streakCount.ToString();
            streakCountText.color = new Color(1f, 0.55f, 0f, 1f);
            streakCountText.transform.DOScale(1f, 1f).SetEase(Ease.OutBounce, 2.5f);
            streakCountText.DOColor(Color.white, 0.5f).SetEase(Ease.OutCubic).SetDelay(0.1f);

            streakLabelText.DOFade(1f, 0.5f);

            yield return new WaitForSeconds(1.3f);

            carouselGroup.DOFade(1, .5f);

            if (daysCarousel != null && daysCarousel.ItemCount > 0)
            {
                daysCarousel.CancelInvoke();
                daysCarousel.GoTo(DaysBefore);
            }

            yield return new WaitForSeconds(0.7f);

            var todayTile = daysCarousel?.transform.GetChild(DaysBefore)?.GetComponent<StreakTile>();
            if (todayTile != null)
                todayTile.AnimateFire();

            yield return new WaitForSeconds(0.7f);

            proceedButton.gameObject.SetActive(true);
            proceedButton.transform.localScale = Vector3.zero;
            proceedButton.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack, 1.3f);
        }

        private void Dismiss()
        {
            proceedButton.onClick.RemoveAllListeners();
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
            StopAllCoroutines();
        }
    }
}
