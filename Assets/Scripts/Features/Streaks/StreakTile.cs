using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.Streaks
{
    public class StreakTile : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dayLabel;
        [SerializeField] private TextMeshProUGUI dateLabel;
        [SerializeField] private RectTransform fireIndicator;
        [SerializeField] private Image background;
        private DateTime date;

        public DateTime Date => date;

        private void Awake()
        {
            if (dayLabel == null)
                dayLabel = transform.Find("DayLabel")?.GetComponent<TextMeshProUGUI>();
            if (dateLabel == null)
                dateLabel = transform.Find("DateLabel")?.GetComponent<TextMeshProUGUI>();
            if (fireIndicator == null)
                fireIndicator = transform.Find("FireIndicator")?.GetComponent<RectTransform>();
            if (background == null)
                background = GetComponent<Image>();
        }

        public void Setup(DateTime newDate, bool isCompleted, bool isToday)
        {
            date = newDate;
            if (dayLabel != null) dayLabel.text = newDate.ToString("ddd");
            if (dateLabel != null) dateLabel.text = newDate.Day.ToString();
            if (fireIndicator != null)
            {
                fireIndicator.gameObject.SetActive(isCompleted);
                fireIndicator.localScale = isToday ? Vector3.zero : Vector3.one;
            }
            if (background != null)
            {
                var col = Color.orange;

                if (isCompleted)
                    ColorUtility.TryParseHtmlString(isToday ? "#FF8C00" : "#CC6600", out col);
                else
                    col = new Color(0.18f, 0.18f, 0.22f, 1f);
                background.color = col;
            }
        }

        public void AnimateFire()
        {
            if (fireIndicator == null) return;
            fireIndicator.gameObject.SetActive(true);
            fireIndicator.DOKill(true);
            fireIndicator.localScale = Vector3.zero;
            fireIndicator.DOScale(1f, 0.6f).SetEase(Ease.OutBack, 1.5f);
        }
    }
}
