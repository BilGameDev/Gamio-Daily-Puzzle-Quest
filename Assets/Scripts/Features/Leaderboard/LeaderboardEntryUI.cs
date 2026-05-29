using Gamio.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.Leaderboard
{
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private Image avatarImage;

        public void SetData(LeaderboardEntry entry)
        {
            if (rankText != null)
                rankText.text = $"#{entry.rank}";

            if (nameText != null)
                nameText.text = entry.displayName;

            if (timeText != null)
                timeText.text = LeaderboardPopupUI.FormatTime(entry.timeSeconds);

            if (streakText != null)
                streakText.text = $"{entry.streakCount} day streak";

            if (avatarImage != null && !string.IsNullOrEmpty(entry.avatarUrl))
                StartCoroutine(LoadAvatar(entry.avatarUrl));
        }

        private System.Collections.IEnumerator LoadAvatar(string url)
        {
            using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
                avatarImage.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            }
        }
    }
}
