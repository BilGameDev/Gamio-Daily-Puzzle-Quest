using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Shikaku
{
    [System.Serializable]
    public struct ShikakuDifficultyConfig
    {
        public int gridSize;
        public Vector2 cellSize;
        public int minRectSize;
        public int maxRectSize;

        public ShikakuDifficultyConfig(int gridSize, float cellSize, int minRectSize = 1, int maxRectSize = 5)
        {
            this.gridSize = gridSize;
            this.cellSize = new Vector2(cellSize, cellSize);
            this.minRectSize = minRectSize;
            this.maxRectSize = maxRectSize;
        }
    }

    [CreateAssetMenu(menuName = "Gamio/Shikaku/Game Settings", fileName = "ShikakuGameSettings")]
    public class ShikakuGameSettingsSO : ScriptableObject
    {
        public ShikakuDifficultyConfig easy = new ShikakuDifficultyConfig(5, 120f, 1, 4);
        public ShikakuDifficultyConfig medium = new ShikakuDifficultyConfig(7, 85f, 1, 5);
        public ShikakuDifficultyConfig hard = new ShikakuDifficultyConfig(9, 65f, 2, 6);

        public ShikakuDifficultyConfig GetConfig(Difficulty d) => d switch
        {
            Difficulty.Easy => easy,
            Difficulty.Hard => hard,
            _ => medium
        };
    }
}
