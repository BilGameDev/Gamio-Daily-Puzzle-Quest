using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Gamio.Games.Pipes
{
    public class PipesCellItem : MonoBehaviour, IPointerDownHandler
    {
        [NonSerialized] public int Row;
        [NonSerialized] public int Col;

        [Header("Prefabs")]
        [SerializeField] private GameObject straightPrefab;
        [SerializeField] private GameObject bendPrefab;
        [SerializeField] private GameObject crossPrefab;
        [SerializeField] private GameObject tJunctionPrefab;
        [SerializeField] private GameObject portPrefab;
        [SerializeField] private GameObject portConnectedPrefab;

        private GameObject pipeRoot;
        private PipeType currentType = PipeType.Empty;
        private bool currentIsPort;
        private float currentRotZ;

        public event Action<int, int> OnClick;

        public void SetVisual(PipeType type, int rotation, bool isPort, int portDirection)
        {
            float rotZ = CalcRotation(type, rotation, isPort, portDirection);

            if (type != currentType || isPort != currentIsPort)
            {
                if (pipeRoot != null)
                {
                    Destroy(pipeRoot);
                    pipeRoot = null;
                }
                currentType = type;
                currentIsPort = isPort;

                var prefab = GetPrefab(type, isPort);
                if (prefab != null)
                {
                    pipeRoot = Instantiate(prefab, transform);
                    var rt = pipeRoot.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.Euler(0, 0, rotZ);
                    currentRotZ = rotZ;
                }
                else
                    return;
            }
            else if (pipeRoot != null && Mathf.Abs(rotZ - currentRotZ) > 0.01f)
            {
                pipeRoot.transform.DOKill();
                pipeRoot.transform.DOLocalRotate(new Vector3(0, 0, rotZ), 0.35f).SetEase(Ease.OutBack);
                currentRotZ = rotZ;
            }

        }

        private static float CalcRotation(PipeType type, int rotation, bool isPort, int portDirection)
        {
            if (isPort)
                return -portDirection * 90;
            return -rotation * 90;
        }

        private GameObject GetPrefab(PipeType type, bool isPort)
        {
            if (isPort) return portPrefab;
            return type switch
            {
                PipeType.Straight => straightPrefab,
                PipeType.Bend => bendPrefab,
                PipeType.Cross => crossPrefab,
                PipeType.TJunction => tJunctionPrefab,
                _ => null
            };
        }

        public void SetPortConnected(int portDirection)
        {
            if (pipeRoot != null)
                Destroy(pipeRoot);
            currentType = PipeType.Empty;
            pipeRoot = Instantiate(portConnectedPrefab, transform);
            var rt = pipeRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0, 0, -portDirection * 90);
        }

        public void PlayTapAnimation()
        {
            tapSeq?.Kill();
            tapSeq = DOTween.Sequence();
            tapSeq.Append(transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 4, 0.5f));
        }

        private Sequence tapSeq;

        public void PlaySolvedAnimation(float delay)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5, 0.5f)
                .SetDelay(delay).SetEase(Ease.OutQuad);
        }

        public void OnPointerDown(PointerEventData eventData) => OnClick?.Invoke(Row, Col);
    }
}
