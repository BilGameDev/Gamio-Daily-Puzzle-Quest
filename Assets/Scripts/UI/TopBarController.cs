using System.Collections;
using Gamio.Core;
using Gamio.Features.Popup;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Gamio.Features.UI
{
    public class TopBarController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button avatarButton;
        [SerializeField] private RawImage avatarPreview;

        [Header("Popup Strings")]
        [SerializeField] private string backPopupTitle = "Leave Game";
        [SerializeField] private string backPopupMessage = "Are you sure you want to return to the hub?";
        [SerializeField] private string tutorialPopupTitle = "Tutorial";
        [SerializeField] private string tutorialPopupMessage = "Replay the tutorial?";

        [SerializeField] private string confirmLabel = "Yes";
        [SerializeField] private string cancelLabel = "No";

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (tutorialButton != null)
                tutorialButton.onClick.AddListener(OnTutorialClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (avatarButton == null || avatarPreview == null)
            {
                Debug.LogWarning("[TopBar] Avatar button or preview not assigned in inspector");
            }
            else
            {
                avatarButton.onClick.AddListener(OnAvatarClicked);
                LoadAvatarPreview();
            }

            AvatarService.OnAvatarSeedChanged += LoadAvatarPreview;
        }

        private void OnBackClicked()
        {
            string title = GamioEvents.IsChallengeActive ? "End Challenge?" : backPopupTitle;
            string message = GamioEvents.IsChallengeActive
                ? "Are you sure you wish to end the challenge?"
                : backPopupMessage;

            PopupUI.Show(title, message,
                onConfirm: GamioEvents.RequestBack,
                onCancel: null,
                confirmLabel: confirmLabel,
                cancelLabel: cancelLabel);
        }

        private void OnTutorialClicked()
        {
            if (GamioEvents.IsChallengeActive)
                return;

            PopupUI.Show(tutorialPopupTitle, tutorialPopupMessage,
                onConfirm: GamioEvents.RequestTutorial,
                onCancel: null,
                confirmLabel: confirmLabel,
                cancelLabel: cancelLabel);
        }

        private void OnSettingsClicked()
        {
            SettingsPopupUI.Show();
        }

        private void OnAvatarClicked()
        {
            ProfilePopupUI.Show();
        }

        private void LoadAvatarPreview()
        {
            if (avatarPreview == null) return;
            StartCoroutine(LoadPreviewTexture());
        }

        private IEnumerator LoadPreviewTexture()
        {
            var seed = AvatarService.GetSavedSeed();
            var url = AvatarService.GetAvatarUrl(seed, 512);
            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success && avatarPreview != null)
            {
                var texture = DownloadHandlerTexture.GetContent(request);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                avatarPreview.texture = texture;
            }
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);

            if (tutorialButton != null)
                tutorialButton.onClick.RemoveListener(OnTutorialClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);

            if (avatarButton != null)
                avatarButton.onClick.RemoveListener(OnAvatarClicked);

            AvatarService.OnAvatarSeedChanged -= LoadAvatarPreview;
        }
    }
}