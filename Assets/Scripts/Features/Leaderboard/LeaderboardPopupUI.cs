using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Gamio.Core;
using Gamio.Core.Services;
using Gamio.Features.Streaks;
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

    [Serializable]
    struct TabButton
    {
        public Button button;
        public TextMeshProUGUI label;
        public Image background;
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
        [SerializeField] private TabButton[] tabButtons;

        [SerializeField] private Color tabSelectedColor = new Color32(255, 128, 0, 255);
        [SerializeField] private Color tabDeselectedColor = new Color32(40, 40, 40, 200);
        [SerializeField] private Color tabLabelSelectedColor = Color.white;
        [SerializeField] private Color tabLabelDeselectedColor = Color.gray;

        private LeaderboardManager manager;
        private SlotLeaderboard[] allLeaderboards;
        private LeaderboardEntry[] entries;
        private string myUserId;
        private int currentSlotIndex;
        private int myRank;
        private int myTotalParticipants;
        private LeaderboardMode mode;
        private bool hasRank;
        private Sequence animSeq;
        private VerticalLayoutGroup layoutGroup;
        private GamioManager gamioManager;
        private CloudAPIService cloudAPI;
        private bool _buttonsShown;

        public static async Task<LeaderboardPopupUI> Show(LeaderboardManager manager, LeaderboardMode mode, int? initialSeedId = null)
        {
            var prefab = Resources.Load<LeaderboardPopupUI>("Popups/LeaderboardPopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("LeaderboardPopupUI prefab not found at Resources/Popups/LeaderboardPopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab);
            await popup.Initialize(manager, mode, initialSeedId);
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

            closeButton?.onClick.AddListener(CloseLeaderboard);
            topButton?.onClick.AddListener(GoToTop);
            myRankButton?.onClick.AddListener(GoToMyRank);

            gamioManager = GamioAppContext.Get<GamioManager>();
            cloudAPI = GamioAppContext.Get<CloudAPIService>();
        }

        private async Task Initialize(LeaderboardManager managerRef, LeaderboardMode newMode, int? initialSeedId)
        {
            manager = managerRef;
            mode = newMode;
            myUserId = managerRef.MyUserId;
            hasRank = false;

            if (titleText != null)
                titleText.text = newMode == LeaderboardMode.Result ? "Challenge Complete!" : "Leaderboard";

            managerRef.OnLeaderboardUpdated += OnMyRankUpdated;
            managerRef.OnError += OnError;

            ShowLoading(true);

            var response = await cloudAPI.GetTodayLeaderboards();
            await managerRef.FetchMyRank();

            allLeaderboards = response?.leaderboards ?? new SlotLeaderboard[0];
            hasRank = manager.MyRanks?.rankings != null && manager.MyRanks.rankings.Length > 0;

            if (allLeaderboards.Length == 0)
            {
                ShowLoading(false);
                StartCoroutine(ShowCloseButton());
                return;
            }

            currentSlotIndex = 0;
            if (initialSeedId.HasValue)
            {
                for (int i = 0; i < allLeaderboards.Length; i++)
                {
                    if (allLeaderboards[i].seedId == initialSeedId.Value)
                    {
                        currentSlotIndex = i;
                        break;
                    }
                }
            }

            CreateTabs();

            var introSeq = DOTween.Sequence().SetId(this);
            if (mode == LeaderboardMode.Result && splashGroup != null)
            {
                splashGroup.alpha = 0f;
                splashGroup.transform.localScale = Vector3.one * 0.7f;
                introSeq.Append(splashGroup.DOFade(1f, 0.3f));
                introSeq.Join(splashGroup.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
                introSeq.AppendInterval(3f);
                introSeq.Append(splashGroup.DOFade(0f, 0.25f));
                introSeq.Join(splashGroup.transform.DOScale(0.8f, 0.25f));
                HapticsHelper.PlayPreset(HapticPatterns.PresetType.Success);
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.transform.localScale = Vector3.one * 0.92f;
                if (mode == LeaderboardMode.Result)
                    introSeq.Append(panelGroup.DOFade(1f, 0.25f).SetDelay(1f));
                else
                    introSeq.Append(panelGroup.DOFade(1f, 0.2f));
                introSeq.Join(panelGroup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            }
            else
            {
                HapticsHelper.PlayPreset(HapticPatterns.PresetType.LightImpact);
            }

            introSeq.OnComplete(() =>
            {
                PopulateSlot();
                ShowButtons();
                if (mode == LeaderboardMode.Result)
                {
                    HapticsHelper.PlayEmphasis(0.7f, 0.5f);
                    if (myRank > 0 && entries != null && entries.Length > 0)
                        DOVirtual.DelayedCall(1.5f, () =>
                        {
                            HapticsHelper.PlaySoftImpact();
                            SmoothScrollTo(myRank - 1, true);
                        }).SetId(this);
                }
            });
        }

        private void CreateTabs()
        {
            for (int i = 0; i < tabButtons.Length && i < allLeaderboards.Length; i++)
            {
                var idx = i;

                if (tabButtons[i].label != null)
                    tabButtons[i].label.text = allLeaderboards[i].gameType;

                if (tabButtons[i].button != null)
                {
                    tabButtons[i].button.onClick.RemoveAllListeners();
                    tabButtons[i].button.onClick.AddListener(() => SelectSlot(idx));
                }
            }

            for (int i = allLeaderboards.Length; i < tabButtons.Length; i++)
            {
                if (tabButtons[i].button != null)
                    tabButtons[i].button.gameObject.SetActive(false);
            }

            UpdateTabHighlight();
        }

        private void UpdateTabHighlight()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                bool selected = i == currentSlotIndex;

                if (tabButtons[i].label != null)
                    tabButtons[i].label.color = selected ? tabLabelSelectedColor : tabLabelDeselectedColor;

                if (tabButtons[i].background != null)
                    tabButtons[i].background.color = selected ? tabSelectedColor : tabDeselectedColor;
            }
        }

        private void SelectSlot(int index)
        {
            DOTween.Kill(this);
            currentSlotIndex = index;
            PopulateSlot();
            UpdateTabHighlight();
        }

        private void PopulateSlot()
        {
            var slot = allLeaderboards[currentSlotIndex];
            entries = slot.entries;

            myRank = -1;
            myTotalParticipants = slot.totalParticipants;
            if (hasRank && manager.MyRanks?.rankings != null)
            {
                var ranking = manager.MyRanks.rankings.FirstOrDefault(r => r.seedId == slot.seedId);
                if (ranking != null)
                {
                    myRank = ranking.rank;
                    myTotalParticipants = ranking.totalParticipants;
                }
            }

            ShowLoading(false);
            BuildList();
        }

        private void OnMyRankUpdated()
        {
            hasRank = manager.MyRanks?.rankings != null && manager.MyRanks.rankings.Length > 0;
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
        }

        private void ShowButtons()
        {
            if (_buttonsShown) return;
            _buttonsShown = true;

            if (topButton != null)
            {
                topButton.gameObject.SetActive(true);
                topButton.transform.localScale = Vector3.zero;
                topButton.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.transform.localScale = Vector3.zero;
                closeButton.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetDelay(0.1f);
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
        }

        private void GoToTop()
        {
            SmoothScrollTo(0, false);
        }

        private void GoToMyRank()
        {
            if (myRank > 0)
            {
                HapticsHelper.PlaySoftImpact();
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
                closeButton.gameObject.SetActive(true);
        }

        private void CloseLeaderboard()
        {
            DOTween.Kill(this);
            if (closeButton != null) closeButton.interactable = false;

            var seq = DOTween.Sequence();
            seq.SetId("Close_" + GetInstanceID());
            if (panelGroup != null)
                seq.Join(panelGroup.DOFade(0f, 0.2f));
            if (splashGroup != null)
                seq.Join(splashGroup.DOFade(0f, 0.15f));
            seq.OnComplete(() =>
            {
                if (gamioManager.StreakPending)
                {
                    StreakOverlay.Show(gamioManager.StreakInfo.current, GamioAppContext.Get<IUIEvents>().RequestBack);
                    gamioManager.SetStreakPending(false);
                }
                Destroy(gameObject);
            });
        }

        private void OnDestroy()
        {
            animSeq?.Kill();
            if (manager != null)
            {
                manager.OnLeaderboardUpdated -= OnMyRankUpdated;
                manager.OnError -= OnError;
            }

            if (closeButton != null) closeButton?.onClick.RemoveAllListeners();
            if (topButton != null) topButton?.onClick.RemoveAllListeners();
            if (myRankButton != null) myRankButton?.onClick.RemoveAllListeners();
            foreach (var tb in tabButtons)
            {
                if (tb.button != null) tb.button.onClick.RemoveAllListeners();
            }

            DOTween.Kill(this);
            DOTween.Kill("Close_" + GetInstanceID());
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
