using System.Collections;
using Gamio.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gamio.Features.Popup
{
    public class SolvedHandler : MonoBehaviour
    {
        IGame game;
        IUIEvents uiEvents;

        void OnEnable()
        {
            uiEvents = GamioAppContext.Get<IUIEvents>();
            if (uiEvents != null)
            {
                uiEvents.OnGameLaunched += Setup;
            }
        }

        void OnDisable()
        {
            if (uiEvents != null)
            {
                uiEvents.OnGameLaunched -= Setup;
            }
        }

        public void Setup(IGame currentGame)
        {
            game = currentGame;
            game.OnSolved += Solved;
        }

        void Solved()
        {
            if (GamioAppContext.Get<GamioManager>() is { ChallengeActive: true }) return;
            StartCoroutine(DelayedPopup());
        }

        IEnumerator DelayedPopup()
        {
            yield return new WaitForSeconds(1.5f);
            var sceneName = SceneManager.GetActiveScene().name;
            SolvedPuzzlePopup.Show(game.DisplayName, sceneName);
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.OnSolved -= Solved;
            }
        }
    }
}

