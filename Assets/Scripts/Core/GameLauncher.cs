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
            var fader = CanvasFader.instance;
            if (fader != null)
                fader.PlayOut(() => LoadGameScene());
            else
                LoadGameScene();
        }
        
        void LoadGameScene()
        {
            mainCamera.DOColor(thisGameButton.targetGraphic.color, 1f).OnComplete(() =>
            {
                uIEvents?.RequestGameScene(gamesLibrary.GetGameScene(game));
            });
        }
    }
}
