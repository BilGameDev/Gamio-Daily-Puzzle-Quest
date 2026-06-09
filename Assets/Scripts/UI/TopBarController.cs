using System.Collections;
using System.Threading.Tasks;
using Gamio.Core;
using Gamio.Core.Services;
using Gamio.Features.Leaderboard;
using Gamio.Features.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gamio.Features.UI
{
    public class TopBarController : MonoBehaviour
    {
        [Header("Streak")]
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private GameObject hasStreakGraphic;
        [SerializeField] private GameObject noStreakGraphic;

        [Header("Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button avatarButton;
        [SerializeField] private Button leaderBoardButton;
        [SerializeField] private RawImage avatarPreview;

        [Header("Popup Strings")]
        [SerializeField] private string backPopupTitle = "Leave Game";
        [SerializeField] private string backPopupMessage = "Are you sure you want to return to the hub?";
        [SerializeField] private string tutorialPopupTitle = "Tutorial";
        [SerializeField] private string tutorialPopupMessage = "Replay the tutorial?";

        [SerializeField] private string confirmLabel = "Yes";
        [SerializeField] private string cancelLabel = "No";

        GamioManager gamioManager;
        GamioSceneManager gamioSceneManager;
        IUIEvents uIEvents;

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (tutorialButton != null)
                tutorialButton.onClick.AddListener(OnTutorialClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (leaderBoardButton != null)
                leaderBoardButton.onClick.AddListener(OnLeaderboardClicked);

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

        void Start()
        {
            gamioManager = GamioAppContext.Get<GamioManager>();
            uIEvents = GamioAppContext.Get<IUIEvents>();
            gamioSceneManager = GamioAppContext.Get<GamioSceneManager>();

            RefreshUI();
        }

        void RefreshUI()
        {
            var streak = gamioManager.StreakInfo.current;

            if (streakText != null)
            {
                ColorUtility.TryParseHtmlString(streak > 0 ? "#FF8C00" : "#202020", out var color);
                streakText.color = color;
                streakText.text = streak > 0 ? streak == 1 ? $"{streak} day" : $"{streak} days" : "No Streak";
            }

            if (hasStreakGraphic != null)
                hasStreakGraphic.SetActive(streak > 0);

            if (noStreakGraphic != null)
                noStreakGraphic.SetActive(streak == 0);

            if (gamioManager != null && gamioSceneManager != null && !gamioSceneManager.IsHomeScene())
            {
                tutorialButton.gameObject.SetActive(!gamioManager.ChallengeActive);
            }
        }

        private void OnBackClicked()
        {
            string title = gamioManager.ChallengeActive ? "End Challenge?" : backPopupTitle;
            string message = gamioManager.ChallengeActive
                ? "Are you sure you wish to end the challenge?"
                : backPopupMessage;

            PopupUI.Show(title, message,
                onConfirm: uIEvents.RequestBack,
                onCancel: null,
                confirmLabel: confirmLabel,
                cancelLabel: cancelLabel);
        }

        private void OnTutorialClicked()
        {
            if (gamioManager.ChallengeActive)
                return;

            PopupUI.Show(tutorialPopupTitle, tutorialPopupMessage,
                onConfirm: uIEvents.RequestTutorial,
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

        private void OnLeaderboardClicked()
        {
            _ = ShowLeaderboard();
        }

        async Task ShowLeaderboard()
        {
            await LeaderboardPopupUI.Show(
                new LeaderboardManager(GamioAppContext.Get<CloudAPIService>(),
                GamioAppContext.Get<AuthService>()),
                LeaderboardMode.Preview);
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

            if (leaderBoardButton != null)
                leaderBoardButton.onClick.RemoveListener(OnLeaderboardClicked);

            AvatarService.OnAvatarSeedChanged -= LoadAvatarPreview;
        }
    }
}