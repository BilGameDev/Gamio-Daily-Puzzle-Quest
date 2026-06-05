using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CanvasFader : MonoBehaviour
{
    public static CanvasFader instance { get; private set; }
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

    private string fadeId;

    void Awake()
    {
        instance = this;
        fadeId = "CanvasFader_" + GetInstanceID();

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
        DOTween.Kill(fadeId);
        for (int i = 0; i < targets.Count; i++)
        {
            var cg = targets[i];
            if (cg == null) continue;
            var delay = i * staggerDelay;

            cg.gameObject.SetActive(true);
            cg.blocksRaycasts = false;

            cg.DOFade(1f, inDuration).SetDelay(delay).SetEase(inEase).SetId(fadeId)
                .OnComplete(() => cg.blocksRaycasts = true);

            if (useScale && cg.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one * inStartScale;
                rt.DOScale(1f, inDuration).SetDelay(delay).SetEase(scaleInEase).SetId(fadeId);
            }
        }
    }

    public void PlayOut(Action onComplete = null)
    {
        DOTween.Kill(fadeId);

        if (targets.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence().SetId(fadeId);
        int remaining = targets.Count;

        for (int i = 0; i < targets.Count; i++)
        {
            var cg = targets[i];
            if (cg == null) { remaining--; continue; }

            cg.blocksRaycasts = false;

            seq.Insert(i * staggerDelay, cg.DOFade(0f, outDuration).SetEase(outEase));

            if (useScale && cg.transform is RectTransform rt)
                seq.Insert(i * staggerDelay, rt.DOScale(Vector3.one * outEndScale, outDuration).SetEase(scaleOutEase));
        }

        seq.OnComplete(() =>
        {
            foreach (var cg in targets)
                if (cg != null) cg.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void Stop()
    {
        DOTween.Kill(fadeId);
    }

    void OnDisable()
    {
        instance = null;
        DOTween.Kill(fadeId);
    }
}
