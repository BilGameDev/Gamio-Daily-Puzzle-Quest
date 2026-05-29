using UnityEngine;

namespace Gamio.Core
{
    public class GameUI : MonoBehaviour
    {
        [Header("Test")]
        [SerializeField] protected bool launchOnStart;
        [SerializeField] protected Difficulty difficulty;
        [SerializeField] protected int seed;

        protected void TestGame(IGame game)
        {
            game.Initialize();
            StartCoroutine(game.Run(seed, difficulty));

            SolvedHandler solvedHandler = new GameObject("SolvedHandler").AddComponent<SolvedHandler>();
            solvedHandler.Setup(game);
        }
    }
}
