using System;
using UnityEngine;

namespace Gamio.Core
{
    public class GamesLibrary : MonoBehaviour
    {
        [SerializeField] GameScenes[] gameScenes;
        [SerializeField] public GameIcons[] gameIcons;

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

        public Games GetGame(string gameType)
        {
            return EnumUtility.TryParse(gameType, true, out Games game) ? game : default;
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

        public GameObject GetGameIcon(Games game)
        {
            GameObject gameIcon = null;
            foreach (var item in gameIcons)
            {
                if (item.gameType.Equals(game))
                {
                    gameIcon = item.gameIcon;
                }
            }

            return gameIcon;
        }

        public GameObject GetGameIcon(string gameType)
        {
            GameObject gameIcon = null;
            foreach (var item in gameIcons)
            {
                if (item.gameType.ToString().Equals(gameType, StringComparison.OrdinalIgnoreCase))
                {
                    gameIcon = item.gameIcon;
                }
            }

            return gameIcon;
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

    [Serializable]
    public struct GameIcons
    {
        public Games gameType;
        public GameObject gameIcon;
    }

}
