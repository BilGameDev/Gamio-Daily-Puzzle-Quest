using Gamio.Core;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Gamio.Root
{
    public class GameLauncher : MonoBehaviour
    {
        [SerializeField] Games game;
        Button thisGameButton;

        IUIEvents uIEvents;
        GamesLibrary gamesLibrary;

        Camera mainCamera;

        const string ReplayCountKey = "GamioReplayCount";

        void Start()
        {
            mainCamera = Camera.main;
            uIEvents = GamioAppContext.Get<IUIEvents>();
            gamesLibrary = GamioAppContext.Get<GamesLibrary>();

            if (TryGetComponent(out Button gameButton))
            {
                thisGameButton = gameButton;
                thisGameButton.onClick.RemoveListener(LaunchGame);
                thisGameButton.onClick.AddListener(LaunchGame);
            }
        }

        void OnDestroy()
        {
            if (thisGameButton != null)
            {
                thisGameButton.onClick.RemoveAllListeners();
            }
        }

        public void LaunchGame()
        {
            var adService = GamioAppContext.Get<IRewardedAdService>();
            var count = PlayerPrefs.GetInt(ReplayCountKey, 0);
            if (count % 2 == 1 && adService != null && adService.IsAdReady)
            {
                uIEvents?.RequestAdGate(
                    onProceed: () =>
                    {
                        adService.ShowRewardedAd(() =>
                        {
                            IncrementCount();
                            ProceedToGame();
                        });
                    },
                    onCancel: null
                );
            }
            else
            {
                ProceedToGame();
            }
        }

        void ProceedToGame()
        {
            var fader = CanvasFader.instance;
            if (fader != null)
                fader.PlayOut(() => LoadGameScene());
            else
                LoadGameScene();
        }

        void LoadGameScene()
        {
            mainCamera.DOColor(thisGameButton.targetGraphic.color, .5f).OnComplete(() =>
            {
                uIEvents?.RequestGameScene(gamesLibrary.GetGameScene(game));
            });
        }

        static void IncrementCount()
        {
            var count = PlayerPrefs.GetInt(ReplayCountKey, 0) + 1;
            PlayerPrefs.SetInt(ReplayCountKey, count);
            PlayerPrefs.Save();
        }
    }
}
