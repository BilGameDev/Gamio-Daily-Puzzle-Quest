using System;
using UnityEngine;

namespace Gamio.Core
{
    public class GamesLibrary : MonoBehaviour
    {
        [SerializeField] GameScenes[] gameScenes;

        void Awake()
        {
            GamioAppContext.Register(this);
        }

        [Serializable]
        struct GameScenes
        {
            public Games game;
            [Scene] public string gameScene;
        }

        public string GetGameScene(Games game)
        {
            string gameScene = string.Empty;
            foreach (var item in gameScenes)
            {
                if (item.game.Equals(game))
                {
                    gameScene = item.gameScene;
                }
            }

            return gameScene;
        }

        public string GetGameScene(string scene)
        {
            string gameScene = string.Empty;
            foreach (var item in gameScenes)
            {
                if (item.game.ToString().Equals(scene, StringComparison.OrdinalIgnoreCase))
                {
                    gameScene = item.gameScene;
                }
            }

            return gameScene;
        }
    }

    public enum Games
    {
        Shikaku,
        Hitori,
        WordGrid,
        WordSearch,
        Sudoku,
        Pipes,
        LineConnect,
        Arrows,
        Kings
    }

}
