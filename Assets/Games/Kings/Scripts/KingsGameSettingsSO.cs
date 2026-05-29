using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Kings
{
    [System.Serializable]
    public struct KingsDifficultyConfig
    {
        public int gridSize;
        public Vector2 cellSize;

        public KingsDifficultyConfig(int gridSize, float cellSize)
        {
            this.gridSize = gridSize;
            this.cellSize = new Vector2(cellSize, cellSize);
        }
    }

    [CreateAssetMenu(menuName = "Gamio/Kings/Game Settings", fileName = "KingsGameSettings")]
    public class KingsGameSettingsSO : ScriptableObject
    {
        public KingsDifficultyConfig easy = new KingsDifficultyConfig(5, 140);
        public KingsDifficultyConfig medium = new KingsDifficultyConfig(7, 100);
        public KingsDifficultyConfig hard = new KingsDifficultyConfig(9, 80);

        public KingsDifficultyConfig GetConfig(Difficulty d) => d switch
        {
            Difficulty.Easy => easy,
            Difficulty.Hard => hard,
            _ => medium
        };
    }
}
