using DG.Tweening;
using UnityEngine;

namespace Gamio.Features.Popup
{
    public class SlideUpPopup : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup overlayGroup;
        [SerializeField] protected CanvasGroup panelGroup;

        protected Vector2 mainCurrentPosition;

        protected void Open()
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.DOFade(0.5f, 0.2f);
            }

            if (panelGroup != null)
            {
                var rect = (RectTransform)panelGroup.transform;
                mainCurrentPosition = rect.anchoredPosition;
                rect.gameObject.SetActive(true);
                rect.anchoredPosition = new Vector2(mainCurrentPosition.x, -3000);
                rect.DOAnchorPos(mainCurrentPosition, 0.2f).SetEase(Ease.OutCubic);
            }
        }

        public virtual void Close()
        {
            var seq = DOTween.Sequence();
            if (overlayGroup != null)
                seq.Join(overlayGroup.DOFade(0f, 0.15f));
            if (panelGroup != null)
                seq.Join(((RectTransform)panelGroup.transform).DOAnchorPosY(-3000, 0.2f).SetEase(Ease.InCubic));
            seq.OnComplete(() =>
            {
                if (this != null)
                    Destroy(gameObject);
            });
        }

        protected virtual void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
