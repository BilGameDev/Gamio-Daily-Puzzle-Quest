using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using Gamio.Core.Services;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnlimitedScrollUI;

namespace Gamio.Features.Leaderboard
{
    public enum LeaderboardMode
    {
        Preview,
        Result
    }

    public class LeaderboardPopupUI : MonoBehaviour
    {
        [SerializeField] private VerticalUnlimitedScroller scroller;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CanvasGroup splashGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button topButton;
        [SerializeField] private Button myRankButton;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject emptyStateText;

        private LeaderboardManager manager;
        private LeaderboardEntry[] entries;
        private string myUserId;
        private int myRank;
        private LeaderboardMode mode;
        private bool hasRank;
        private bool hasEntries;
        private Sequence animSeq;
        private VerticalLayoutGroup layoutGroup;

        public static async Task<LeaderboardPopupUI> Show(LeaderboardManager manager, int seedId, LeaderboardMode mode)
        {
            var prefab = Resources.Load<LeaderboardPopupUI>("Popups/LeaderboardPopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("LeaderboardPopupUI prefab not found at Resources/Popups/LeaderboardPopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab);
            await popup.Initialize(manager, seedId, mode);
            return popup;
        }

        private void Awake()
        {
            if (panelGroup != null) panelGroup.alpha = 0f;
            if (splashGroup != null) splashGroup.alpha = 0f;
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (emptyStateText != null) emptyStateText.SetActive(false);
            if (topButton != null) topButton.gameObject.SetActive(false);
            if (myRankButton != null) myRankButton.gameObject.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);

            closeButton?.onClick.AddListener(() => Destroy(gameObject));
            topButton?.onClick.AddListener(GoToTop);
            myRankButton?.onClick.AddListener(GoToMyRank);
        }

        private async Task Initialize(LeaderboardManager managerRef, int seedId, LeaderboardMode newMode)
        {
            manager = managerRef;
            mode = newMode;
            myUserId = managerRef.MyUserId;
            hasRank = false;
            hasEntries = false;

            if (titleText != null)
                titleText.text = newMode == LeaderboardMode.Result ? "Challenge Complete!" : "Leaderboard";

            managerRef.OnLeaderboardUpdated += OnDataUpdated;
            managerRef.OnError += OnError;

            if (managerRef.MyRanks?.rankings != null && managerRef.CurrentEntries?.Length > 0)
            {
                OnDataUpdated();
                return;
            }

            ShowLoading(true);
            await managerRef.FetchMyRank();
            await managerRef.FetchLeaderboard(seedId);
        }

        private void OnDataUpdated()
        {
            if (!hasRank && manager.MyRanks?.rankings != null)
            {
                hasRank = true;
                myRank = manager.MyRanks.rankings.Length > 0 ? manager.MyRanks.rankings[0].rank : -1;
            }

            if (!hasEntries && manager.CurrentEntries != null)
            {
                hasEntries = true;
                entries = manager.CurrentEntries;
            }

            if (hasRank && hasEntries)
            {
                ShowLoading(false);
                BuildList();
            }
        }

        private void OnError(string error)
        {
            ShowLoading(false);
            Debug.LogError($"[Leaderboard] Error: {error}");
        }

        private void BuildList()
        {
            bool empty = entries == null || entries.Length == 0;
            if (emptyStateText != null) emptyStateText.SetActive(empty);
            if (scroller != null) scroller.gameObject.SetActive(!empty);

            if (empty)
            {
                if (panelGroup != null)
                {
                    panelGroup.alpha = 0f;
                    panelGroup.transform.localScale = Vector3.one * 0.92f;
                    panelGroup.DOFade(1f, 0.2f);
                    panelGroup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                }
                HapticsHelper.PlayPreset(HapticPatterns.PresetType.LightImpact);
                StartCoroutine(ShowCloseButton());
                return;
            }

            scroller.Generate(cellPrefab, entries.Length, (index, cell) =>
            {
                var mono = cell as MonoBehaviour;
                if (mono != null)
                {
                    var lbCell = mono.GetComponent<LeaderboardCell>();
                    if (lbCell != null)
                        lbCell.SetData(entries[index], entries[index].userId == myUserId);
                }
            });

            if (mode == LeaderboardMode.Result)
                StartCoroutine(ResultAnimation());
            else
                StartCoroutine(PreviewAnimation());
        }

        private IEnumerator PreviewAnimation()
        {
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.transform.localScale = Vector3.one * 0.92f;
                panelGroup.DOFade(1f, 0.2f);
                panelGroup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.LightImpact);

            yield return new WaitForSeconds(0.3f);
            ShowButtons();
        }

        private IEnumerator ResultAnimation()
        {
            if (splashGroup != null)
            {
                splashGroup.alpha = 0f;
                splashGroup.transform.localScale = Vector3.one * 0.7f;
                splashGroup.DOFade(1f, 0.3f);
                splashGroup.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
            }
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);

            yield return new WaitForSeconds(3f);

            if (splashGroup != null)
            {
                splashGroup.DOFade(0f, 0.25f);
                splashGroup.transform.DOScale(0.8f, 0.25f);
            }

            if (panelGroup != null)
            {
                panelGroup.transform.localScale = Vector3.one * 0.85f;
                panelGroup.alpha = 0f;
                panelGroup.DOFade(1f, 0.25f).SetDelay(1f);
                panelGroup.transform.DOScale(1f, 0.35f).SetDelay(1f).SetEase(Ease.OutBack);
            }
            HapticsHelper.PlayPreset(HapticPatterns.PresetType.MediumImpact);

            ShowButtons();

            yield return new WaitForSeconds(1.5f);

            if (myRank > 0 && entries != null && entries.Length > 0)
            {
                HapticsHelper.PlayPreset(HapticPatterns.PresetType.Selection);
                SmoothScrollTo(myRank - 1, true);
            }
        }

        private void ShowButtons()
        {
            if (topButton != null)
            {
                topButton.gameObject.SetActive(true);
                topButton.transform.localScale = Vector3.zero;
                topButton.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
            if (myRankButton != null)
            {
                bool hasRankBool = myRank > 0;
                myRankButton.gameObject.SetActive(hasRankBool);
                if (hasRankBool)
                {
                    myRankButton.transform.localScale = Vector3.zero;
                    myRankButton.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetDelay(0.05f);
                }
            }
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.transform.localScale = Vector3.zero;
                closeButton.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetDelay(0.1f);
            }
        }

        private void GoToTop()
        {
            SmoothScrollTo(0, false);
        }

        private void GoToMyRank()
        {
            if (myRank > 0)
            {
                HapticsHelper.PlayPreset(HapticPatterns.PresetType.Selection);
                SmoothScrollTo(myRank - 1, true);
            }
        }

        private void SmoothScrollTo(int index, bool center)
        {
            float targetPos = CalcScrollPosition(index, center);
            DOTween.Kill("Scroll_" + GetInstanceID());
            DOTween.To(() => scroller.scrollRect.verticalNormalizedPosition,
                    x => scroller.scrollRect.verticalNormalizedPosition = x,
                    targetPos, 0.6f)
                .SetEase(Ease.OutCubic)
                .SetId("Scroll_" + GetInstanceID());
        }

        private float CalcScrollPosition(int index, bool center)
        {
            if (scroller.RowCount <= 1) return 0f;
            var cellRect = cellPrefab.GetComponent<RectTransform>().rect;
            if (layoutGroup == null)
                layoutGroup = scroller.scrollRect.content.GetComponent<VerticalLayoutGroup>();
            var spacing = layoutGroup.spacing;
            var viewH = scroller.ViewportHeight;
            float totalH = cellRect.height * entries.Length + spacing * (entries.Length - 1);
            float scrollable = Mathf.Max(totalH - viewH, 0f);
            if (scrollable <= 0f) return 0f;

            float itemPos = index * (cellRect.height + spacing);
            if (center)
                itemPos += cellRect.height / 2f - viewH / 2f;
            float norm = Mathf.Clamp01(itemPos / scrollable);
            return 1f - norm;
        }

        private void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(show);
        }

        private IEnumerator ShowCloseButton()
        {
            yield return new WaitForSeconds(0.3f);
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.transform.localScale = Vector3.zero;
                closeButton.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
        }

        private void OnDestroy()
        {
            animSeq?.Kill();
            if (manager != null)
            {
                manager.OnLeaderboardUpdated -= OnDataUpdated;
                manager.OnError -= OnError;
            }
            DOTween.Kill(this);
            StopAllCoroutines();
        }

        public static string FormatTime(float seconds)
        {
            var ts = System.TimeSpan.FromSeconds(seconds);
            if (ts.Hours > 0)
                return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.Minutes > 0)
                return $"{ts.Minutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }
    }
}
