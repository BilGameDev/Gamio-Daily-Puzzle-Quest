using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CanvasFader : MonoBehaviour
{
    [SerializeField] private bool playOnStart;

    [Header("Fade Settings")]
    [SerializeField] private List<CanvasGroup> targets = new();
    [SerializeField] private float inDuration = 0.5f;
    [SerializeField] private float outDuration = 0.35f;
    [SerializeField] private float staggerDelay = 0.15f;
    [SerializeField] private Ease inEase = Ease.OutCubic;
    [SerializeField] private Ease outEase = Ease.InCubic;
    [SerializeField] private bool startHidden = true;

    [Header("Scale")]
    [SerializeField] private bool useScale;
    [SerializeField] private float inStartScale = 0.92f;
    [SerializeField] private float outEndScale = 0.92f;
    [SerializeField] private Ease scaleInEase = Ease.OutBack;
    [SerializeField] private Ease scaleOutEase = Ease.InCubic;

    void Awake()
    {
        if (targets.Count == 0)
            GetComponentsInChildren(true, targets);

        if (startHidden)
        {
            foreach (var cg in targets)
            {
                if (cg == null) continue;
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                if (useScale && cg.transform is RectTransform rt)
                    rt.localScale = Vector3.one * inStartScale;
            }
        }
    }

    void Start()
    {
        if (playOnStart)
            PlayIn();
    }

    public void PlayIn()
    {
        DOTween.Kill(this);
        for (int i = 0; i < targets.Count; i++)
        {
            var cg = targets[i];
            if (cg == null) continue;
            var delay = i * staggerDelay;

            cg.gameObject.SetActive(true);
            cg.blocksRaycasts = false;

            cg.DOFade(1f, inDuration).SetDelay(delay).SetEase(inEase).SetId(this)
                .OnComplete(() => cg.blocksRaycasts = true);

            if (useScale && cg.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one * inStartScale;
                rt.DOScale(1f, inDuration).SetDelay(delay).SetEase(scaleInEase).SetId(this);
            }
        }
    }

    public void PlayOut()
    {
        DOTween.Kill(this);
        for (int i = 0; i < targets.Count; i++)
        {
            var cg = targets[i];
            if (cg == null) continue;
            var delay = i * staggerDelay;

            cg.blocksRaycasts = false;

            cg.DOFade(0f, outDuration).SetDelay(delay).SetEase(outEase).SetId(this)
                .OnComplete(() => cg.gameObject.SetActive(false));

            if (useScale && cg.transform is RectTransform rt)
                rt.DOScale(Vector3.one * outEndScale, outDuration).SetDelay(delay).SetEase(scaleOutEase).SetId(this);
        }
    }

    public void Stop()
    {
        DOTween.Kill(this);
    }

    void OnDisable()
    {
        DOTween.Kill(this);
    }
}
