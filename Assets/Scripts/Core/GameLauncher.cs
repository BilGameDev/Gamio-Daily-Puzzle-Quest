using Gamio.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Root
{
    public class GameLauncher : MonoBehaviour
    {
        [SerializeField] Games game;
        Button thisGameButton;

        IUIEvents uIEvents;
        GamesLibrary gamesLibrary;

        void Start()
        {
            uIEvents = GamioAppContext.Get<IUIEvents>();
            gamesLibrary = GamioAppContext.Get<GamesLibrary>();

            if (TryGetComponent(out Button gameButton))
            {
                thisGameButton = gameButton;
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
            uIEvents?.RequestGameScene(gamesLibrary.GetGameScene(game));
        }
    }
}
