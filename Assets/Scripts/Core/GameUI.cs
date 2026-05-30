using UnityEngine;

namespace Gamio.Core
{
    public class GameUI : MonoBehaviour
    {
        [Header("Test")]
        [SerializeField] protected bool launchOnStart;
        [SerializeField] protected Difficulty difficulty;
        [SerializeField] protected int seed;

        IUIEvents uIEvents;

        protected void TestGame(IGame game)
        {
            game.Initialize();
            StartCoroutine(game.Run(seed, difficulty));

            SolvedHandler solvedHandler = new GameObject("SolvedHandler").AddComponent<SolvedHandler>();
            solvedHandler.Setup(game);
        }

        private void OnEnable()
        {
            uIEvents = GamioAppContext.Get<IUIEvents>();

            if (uIEvents != null)
            {
                uIEvents.OnResetRequested += ResetPuzzle;
                uIEvents.OnHintRequested += OnHint;
            }
        }

        private void OnDisable()
        {
            if (uIEvents != null)
            {
                uIEvents.OnResetRequested -= ResetPuzzle;
                uIEvents.OnHintRequested -= OnHint;
            }
        }

        protected virtual void ResetPuzzle() { }
        protected virtual void OnHint() { }
    }
}
