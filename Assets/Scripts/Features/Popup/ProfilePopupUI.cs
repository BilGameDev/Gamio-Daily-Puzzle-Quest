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
using Lean.Gui;
using DG.Tweening;

namespace Gamio.Features.Popup
{
    [Serializable]
    class ProfanityResponse
    {
        public bool isProfanity;
        public float score;
    }

    public class ProfilePopupUI : SlideUpPopup
    {
        [Header("Avatar")]
        [SerializeField] private RawImage avatarImage;
        [SerializeField] private Button newRobotButton;

        [Header("Username")]
        [SerializeField] private TMP_InputField usernameField;
        [SerializeField] private Button updateUsernameButton;
        [SerializeField] private TextMeshProUGUI usernameFeedbackText;

        [Header("Actions")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button privacyButton;
        [SerializeField] private LeanToggle hapticsToggle;

        private string currentSeed;
        private bool isLoading;

        private const string AudioPrefKey = "Gamio_AudioEnabled";
        private const string HapticsPrefKey = "Gamio_HapticsEnabled";

        void OnEnable()
        {
            hapticsToggle.OnOn.AddListener(() => {
                PlayerPrefs.SetInt(HapticsPrefKey, 1);
                PlayerPrefs.Save();
            });
            hapticsToggle.OnOff.AddListener(() => {
                PlayerPrefs.SetInt(HapticsPrefKey, 0);
                PlayerPrefs.Save();
            });
        }

        void OnDisable()
        {
            hapticsToggle.OnOn.RemoveAllListeners();
            hapticsToggle.OnOff.RemoveAllListeners();
        }

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
            
            creditsButton.onClick.AddListener(OnCreditsClicked);

            if (privacyButton != null)
                privacyButton.onClick.AddListener(OnPrivacyClicked);

            currentSeed = AvatarService.GetSavedSeed();
            StartCoroutine(LoadAvatarTexture(currentSeed));

            var auth = GamioAppContext.Get<AuthService>();
            usernameField.text = !string.IsNullOrEmpty(auth.Username) ? auth.Username : auth.DisplayName;

            Open();
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
                newRobotButton.interactable = true;
            }
            isLoading = false;
        }

        private void OnLogoutClicked()
        {
            PopupUI.Show("Logout", "Are you sure you want to log out?",
                onConfirm: () => GamioAppContext.Get<ILoginEvents>()?.RequestLogout(),
                confirmLabel: "Logout");
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

                await GamioAppContext.Get<AuthService>().UpdateUsername(username);
                ShowFeedback("Username updated!", false);
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

        private void OnPrivacyClicked()
        {
            GamioAppContext.Get<IUIEvents>()?.RequestPrivacy();
        }

         private void OnCreditsClicked()
        {
            CreditsPopupUI.Show();
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

        public override void Close()
        {
            newRobotButton.onClick.RemoveAllListeners();
            logoutButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();
            updateUsernameButton?.onClick.RemoveAllListeners();
            creditsButton.onClick.RemoveAllListeners();
            if (privacyButton != null)
                privacyButton.onClick.RemoveAllListeners();

            base.Close();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopAllCoroutines();
        }
    }
}
