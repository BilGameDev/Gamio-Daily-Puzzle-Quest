using Gamio.Core;
using UnityEngine;

namespace Gamio.Root
{
    public class GameLauncher : MonoBehaviour
    {
        enum CurrentGames
        {
            Kings
        }
        [Header("Game")]
        [SerializeField] CurrentGames game;

        [Header("Test")]
        [SerializeField] bool launchOnStart;
        [SerializeField] Difficulty difficulty;
        [SerializeField] int seed;

        IGame currentGame;
    }
}
