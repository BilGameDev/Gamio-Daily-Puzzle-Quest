using Gamio.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.HomeHub
{
    public class HomeSceneManager : MonoBehaviour
    {
        [Header("Streak")]
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private GameObject hasStreakGraphic;
        [SerializeField] private GameObject noStreakGraphic;

        [Header("Challenge")]
        [SerializeField] private Button challengeButton;
        [SerializeField] private TextMeshProUGUI challengeButtonLabel;
        [SerializeField] private Color challengeAvailableColor = new Color32(255, 128, 0, 255);
        [SerializeField] private Color challengeCompletedColor = new Color32(80, 80, 80, 255);

        GamioManager gamioManager;

        private void Start()
        {
            gamioManager = GamioAppContext.Get<GamioManager>();
            challengeButton.onClick.AddListener(ShowChallengePopup);
            RefreshUI();
        }

        void ShowChallengePopup()
        {
            GamioAppContext.Get<IUIEvents>().RequestChallenge();
        }

        private void RefreshUI()
        {
            var streak = gamioManager.StreakInfo.current;

            if (streakText != null)
            {
                ColorUtility.TryParseHtmlString(streak > 0 ? "#FF8C00" : "#202020", out var color);
                streakText.color = color;
                streakText.text = streak > 0 ? $"{streak} days" : "No Streak";
            }

            if (hasStreakGraphic != null)
                hasStreakGraphic.SetActive(streak > 0);

            if (noStreakGraphic != null)
                noStreakGraphic.SetActive(streak == 0);

            if (challengeButton != null && challengeButtonLabel != null)
            {
                bool completed = gamioManager.DailyCompleted;
                challengeButton.interactable = !completed;
                challengeButtonLabel.text = completed ? "New challenge tomorrow" : "Begin Challenge";
                var colors = challengeButton.colors;
                colors.normalColor = completed ? challengeCompletedColor : challengeAvailableColor;
                challengeButton.colors = colors;
            }
        }

        void OnDestroy()
        {
            challengeButton.onClick.RemoveAllListeners();
        }
    }
}
