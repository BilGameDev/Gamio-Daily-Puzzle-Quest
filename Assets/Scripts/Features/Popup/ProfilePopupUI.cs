using System;
using System.Collections;
using System.Text;
using Cysharp.Threading.Tasks;
using Gamio.Core;
using Gamio.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using DG.Tweening;

namespace Gamio.Features.Popup
{
    [Serializable]
    class ProfanityResponse
    {
        public bool isProfanity;
        public float score;
    }

    public class ProfilePopupUI : MonoBehaviour
    {
        [Header("Avatar")]
        [SerializeField] private RawImage avatarImage;
        [SerializeField] private Button newRobotButton;
        [SerializeField] private TextMeshProUGUI newRobotButtonText;

        [Header("Username")]
        [SerializeField] private TMP_InputField usernameField;
        [SerializeField] private Button updateUsernameButton;
        [SerializeField] private TextMeshProUGUI usernameFeedbackText;

        [Header("Actions")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private TextMeshProUGUI logoutButtonText;
        [SerializeField] private Button closeButton;

        [Header("Animation")]
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

            if (updateUsernameButton != null)
                updateUsernameButton.onClick.AddListener(OnUpdateUsernameClicked);

            currentSeed = AvatarService.GetSavedSeed();
            StartCoroutine(LoadAvatarTexture(currentSeed));

            usernameField.text = GamioAppContext.Get<AuthService>().Username;

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
            GamioAppContext.Get<ILoginEvents>()?.RequestLogout();
        }

        private async void OnUpdateUsernameClicked()
        {
            var username = usernameField.text?.Trim();
            if (string.IsNullOrEmpty(username)) return;

            updateUsernameButton.interactable = false;

            try
            {
                bool isProfane = await CheckProfanity(username);
                if (isProfane)
                {
                    ShowFeedback("Username contains inappropriate language", true);
                    return;
                }

                var result = await GamioAppContext.Get<CloudAPIService>().UpdateUsername(username);
                if (result.success)
                    ShowFeedback("Username updated!", false);
                else
                    ShowFeedback("Failed to update username", true);
            }
            catch (Exception e)
            {
                ShowFeedback($"Error: {e.Message}", true);
            }
            finally
            {
                updateUsernameButton.interactable = true;
            }
        }

        private async UniTask<bool> CheckProfanity(string text)
        {
            try
            {
                var json = $"{{\"message\":\"{EscapeJson(text)}\"}}";
                using var req = new UnityWebRequest("https://www.profanity.dev/api", "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                    return false;

                var response = JsonUtility.FromJson<ProfanityResponse>(req.downloadHandler.text);
                return response.isProfanity;
            }
            catch
            {
                return false;
            }
        }

        private void ShowFeedback(string message, bool isError)
        {
            if (usernameFeedbackText == null) return;
            usernameFeedbackText.text = message;
            usernameFeedbackText.color = isError ? Color.red : Color.green;
            usernameFeedbackText.DOFade(1f, 0.2f);
            DOVirtual.DelayedCall(3f, () => usernameFeedbackText.DOFade(0f, 0.3f));
        }

        private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        public void Close()
        {
            newRobotButton.onClick.RemoveAllListeners();
            logoutButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();
            updateUsernameButton?.onClick.RemoveAllListeners();

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
