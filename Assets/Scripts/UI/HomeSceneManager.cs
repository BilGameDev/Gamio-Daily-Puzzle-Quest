using System;
using System.Threading.Tasks;
using Gamio.Core;
using Gamio.Core.Services;
using Gamio.Features.Leaderboard;
using Gamio.Features.Popup;
using Gamio.Features.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.HomeHub
{
    public class HomeSceneManager : MonoBehaviour
    {
        [Header("Challenge")]
        [SerializeField] private Button challengeButton;
        [SerializeField] private TextMeshProUGUI challengeButtonLabel;
        [SerializeField] private Color challengeAvailableColor = new Color32(255, 128, 0, 255);
        [SerializeField] private Color challengeCompletedColor = new Color32(80, 80, 80, 255);
        [SerializeField] private Button leaderBoardButton;

        [Header("Carousel")]
        [SerializeField] private Carousel carousel;
        [SerializeField] private GameObject carouselCellPrefab;

        GamioManager gamioManager;
        GamesLibrary gamesLibrary;
        ICloudDataEvents cloudDataEvents;
        IUIEvents uiEvents;

        private void Start()
        {
            gamioManager = GamioAppContext.Get<GamioManager>();
            gamesLibrary = GamioAppContext.Get<GamesLibrary>();
            uiEvents = GamioAppContext.Get<IUIEvents>();
            challengeButton.onClick.AddListener(ShowChallengePopup);

            if (leaderBoardButton != null)
                leaderBoardButton.onClick.AddListener(OnLeaderboardClicked);

            UpdateButtonForFocusedChallenge();

            if (gamioManager.Challenges != null && gamioManager.Challenges.Length > 0)
                PopulateCarousel();
        }

        void OnEnable()
        {
            cloudDataEvents = GamioAppContext.Get<ICloudDataEvents>();
            if (cloudDataEvents != null)
                cloudDataEvents.OnSeedFetched += OnSeedFetched;

            uiEvents = GamioAppContext.Get<IUIEvents>();
            if (uiEvents != null)
                uiEvents.OnAdGateRequested += HandleAdGate;
        }

        void OnDisable()
        {
            if (cloudDataEvents != null)
                cloudDataEvents.OnSeedFetched -= OnSeedFetched;

            if (uiEvents != null)
                uiEvents.OnAdGateRequested -= HandleAdGate;
        }

        void HandleAdGate(Action onProceed, Action onCancel)
        {
            PopupUI.Show("Play", "Watch an ad to play?",
                onConfirm: onProceed,
                onCancel: onCancel,
                confirmLabel: "Watch Ad",
                cancelLabel: "Cancel");
        }

        void OnSeedFetched(SeedResponse response)
        {
            PopulateCarousel();
        }

        void PopulateCarousel()
        {
            if (carousel == null || carouselCellPrefab == null) return;
            var challenges = gamioManager.Challenges;
            if (challenges == null || challenges.Length == 0) return;

            foreach (Transform child in carousel.transform)
                Destroy(child.gameObject);

            foreach (var challenge in challenges)
            {
                var cell = Instantiate(carouselCellPrefab, carousel.transform);
                var iconPrefab = gamesLibrary?.GetGameIcon(challenge.gameType);
                if (iconPrefab != null)
                    Instantiate(iconPrefab, cell.transform);

                var label = cell.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = challenge.gameType;

                if (!cell.TryGetComponent(out CarouselItem item))
                    item = cell.AddComponent<CarouselItem>();
                if (label != null && !item.EnabledObjects.Contains(label.gameObject))
                    item.EnabledObjects.Add(label.gameObject);
            }

            carousel.OnIndexChanged -= OnCarouselIndexChanged;
            carousel.OnIndexChanged += OnCarouselIndexChanged;
            UpdateButtonForFocusedChallenge();
        }

        void OnCarouselIndexChanged(int index)
        {
            UpdateButtonForFocusedChallenge();
        }

        void ShowChallengePopup()
        {
            if (carousel == null) return;

            var index = carousel.CurrentIndex;
            var challenges = gamioManager.Challenges;
            if (challenges == null || index < 0 || index >= challenges.Length) return;
            if (challenges[index].completed) return;

            gamioManager.SelectChallenge(index);
            uiEvents?.RequestChallenge();
        }

        private void UpdateButtonForFocusedChallenge()
        {
            if (challengeButton == null || challengeButtonLabel == null) return;

            var challenges = gamioManager.Challenges;
            bool completed = challenges != null && carousel != null
                && carousel.CurrentIndex >= 0 && carousel.CurrentIndex < challenges.Length
                && challenges[carousel.CurrentIndex].completed;

            challengeButton.interactable = !completed;
            challengeButtonLabel.text = completed ? "Quest Completed" : "Begin Quest";
            var colors = challengeButton.colors;
            colors.normalColor = completed ? challengeCompletedColor : challengeAvailableColor;
            challengeButton.colors = colors;
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

        void OnDestroy()
        {
            if (carousel != null)
                carousel.OnIndexChanged -= OnCarouselIndexChanged;
            challengeButton.onClick.RemoveAllListeners();
            if (leaderBoardButton != null)
                leaderBoardButton.onClick.RemoveAllListeners();
        }
    }
}
