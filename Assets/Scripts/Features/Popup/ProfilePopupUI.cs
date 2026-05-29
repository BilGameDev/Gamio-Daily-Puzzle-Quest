using System.Collections;
using Gamio.Core;
using Gamio.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using DG.Tweening;

namespace Gamio.Features.Popup
{
    public class ProfilePopupUI : MonoBehaviour
    {
        [SerializeField] private RawImage avatarImage;
        [SerializeField] private Button newRobotButton;
        [SerializeField] private TextMeshProUGUI newRobotButtonText;
        [SerializeField] private Button logoutButton;
        [SerializeField] private TextMeshProUGUI logoutButtonText;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CanvasGroup overlayGroup;

        private string currentSeed;
        private bool isLoading;

        public static ProfilePopupUI Show()
        {
            var prefab = Resources.Load<ProfilePopupUI>("Popups/ProfilePopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("ProfilePopupUI prefab not found at Resources/Popups/ProfilePopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab);
            popup.Setup();
            return popup;
        }

        private void Setup()
        {
            if (newRobotButton == null) Debug.LogError("[ProfilePopup] newRobotButton not assigned in prefab");
            else newRobotButton.onClick.AddListener(OnNewRobotClicked);

            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);

            if (closeButton == null) Debug.LogError("[ProfilePopup] closeButton not assigned in prefab");
            else closeButton.onClick.AddListener(Close);

            currentSeed = AvatarService.GetSavedSeed();
            StartCoroutine(LoadAvatarTexture(currentSeed));

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

        private void OnNewRobotClicked()
        {
            if (isLoading) return;
            currentSeed = AvatarService.GenerateRandomSeed();
            AvatarService.SaveSeed(currentSeed);
            StartCoroutine(LoadAvatarTexture(currentSeed));
        }

        private IEnumerator LoadAvatarTexture(string seed)
        {
            isLoading = true;
            newRobotButton.interactable = false;
            newRobotButtonText.text = "Loading...";

            var url = AvatarService.GetAvatarUrl(seed, 512);
            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var texture = DownloadHandlerTexture.GetContent(request);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                avatarImage.texture = texture;
            }
            else
            {
                Debug.LogWarning($"[ProfilePopup] Avatar load failed: {request.error}");
            }

            if (this != null)
            {
                newRobotButtonText.text = "Random";
                newRobotButton.interactable = true;
            }
            isLoading = false;
        }

        private void OnLogoutClicked()
        {
            GamioEvents.RequestLogout();
        }

        public void Close()
        {
            newRobotButton.onClick.RemoveAllListeners();
            logoutButton.onClick.RemoveAllListeners();
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
            StopAllCoroutines();
        }
    }
}
