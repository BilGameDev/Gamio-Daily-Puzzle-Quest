using Gamio.Core;
using Gamio.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Gamio.Features.Popup
{
    public class SolvedPuzzlePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI puzzleNameText;
        [SerializeField] private TextMeshProUGUI displayNameText;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button backButton;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private CanvasGroup overlayGroup;

        private string replaySceneName;
        private bool requiresAd;

        private const string ReplayCountKey = "GamioReplayCount";

        public static SolvedPuzzlePopup Show(string puzzleName, string replayScene)
        {
            var prefab = Resources.Load<SolvedPuzzlePopup>("Popups/SolvedPuzzlePopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("SolvedPuzzlePopup prefab not found at Resources/Popups/SolvedPuzzlePopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab);
            popup.Setup(puzzleName, replayScene);
            return popup;
        }

        private void Setup(string puzzleName, string sceneName)
        {
            replaySceneName = sceneName;
            var adService = GamioAppContext.Get<IRewardedAdService>();
            requiresAd = CheckRequiresAd() && adService != null && adService.IsAdReady;

            if (puzzleNameText != null)
                puzzleNameText.text = puzzleName;
            if (displayNameText != null)
                displayNameText.text = "Puzzle Solved!";

            if (replayButton != null)
            {
                var buttonText = replayButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = requiresAd ? "Watch Ad" : "Replay";
                replayButton.onClick.AddListener(OnReplayClicked);
            }
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.DOFade(0.5f, 0.2f);
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.transform.localScale = Vector3.one * 0.8f;
                panelGroup.DOFade(1f, 0.2f);
                panelGroup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
        }

        private static bool CheckRequiresAd()
        {
            var count = PlayerPrefs.GetInt(ReplayCountKey, 0);
            return count % 2 == 1;
        }

        private static void IncrementReplayCount()
        {
            var count = PlayerPrefs.GetInt(ReplayCountKey, 0) + 1;
            PlayerPrefs.SetInt(ReplayCountKey, count);
            PlayerPrefs.Save();
        }

        private void OnReplayClicked()
        {
            if (requiresAd)
            {
                GamioAppContext.Get<IRewardedAdService>().ShowRewardedAd(() =>
                {
                    IncrementReplayCount();
                    Dismiss(() => SceneLoader.LoadScene(replaySceneName));
                });
            }
            else
            {
                IncrementReplayCount();
                Dismiss(() => SceneLoader.LoadScene(replaySceneName));
            }
        }

        private void OnBackClicked()
        {
            Dismiss(() =>
            {
                var uIEvents = GamioAppContext.Get<IUIEvents>();
                uIEvents?.RequestBack();
            });
        }

        private void Dismiss(System.Action onDone)
        {
            if (replayButton != null)
                replayButton.onClick.RemoveAllListeners();
            if (backButton != null)
                backButton.onClick.RemoveAllListeners();

            var seq = DOTween.Sequence();
            if (overlayGroup != null)
                seq.Join(overlayGroup.DOFade(0f, 0.15f));
            if (panelGroup != null)
                seq.Join(panelGroup.DOFade(0f, 0.15f));
            seq.OnComplete(() =>
            {
                onDone?.Invoke();
                if (this != null)
                    Destroy(gameObject);
            });
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
