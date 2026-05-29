using Gamio.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnlimitedScrollUI;

namespace Gamio.Features.Leaderboard
{
    public class LeaderboardCell : MonoBehaviour, ICell
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private TextMeshProUGUI streakIcon;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color myRankColor = Color.white;
        [SerializeField] private Color normalColor = new Color(0.12f, 0.12f, 0.12f, 0.8f);
        [SerializeField] private Color dismissColor;

        private LeaderboardEntry data;
        private bool isMe;

        public void SetData(LeaderboardEntry entry, bool isMe)
        {
            data = entry;
            this.isMe = isMe;

            if (rankText != null)
            {
                rankText.text = $"#{entry.rank}";
                rankText.fontSize = entry.rank <= 3 ? 36 : 28;
            }

            if (nameText != null)
                nameText.text = entry.displayName;

            if (timeText != null)
                timeText.text = LeaderboardPopupUI.FormatTime(entry.timeSeconds);

            if (streakText != null)
                streakText.text = $"{entry.streakCount}";

            if (backgroundImage != null)
            {
                backgroundImage.color = isMe ? myRankColor : normalColor;

                switch (entry.rank)
                {
                    case 1:
                    backgroundImage.color = Color.gold;
                    break;

                    case 2:
                    backgroundImage.color = Color.silver;
                    break;

                    case 3:
                    backgroundImage.color = Color.brown;
                    break;
                }
            }

            if (entry.streakCount == 0)
            {
                streakText.color = dismissColor;
                streakIcon.color = dismissColor;
            }
        }

        public void OnBecomeVisible(ScrollerPanelSide side) { }
        public void OnBecomeInvisible(ScrollerPanelSide side) { }
    }
}
