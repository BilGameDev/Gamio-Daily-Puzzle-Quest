using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Shikaku
{
    [System.Serializable]
    public struct ShikakuDifficultyConfig
    {
        public int gridSize;
        public Vector2 cellSize;

        public ShikakuDifficultyConfig(int gridSize, float cellSize)
        {
            this.gridSize = gridSize;
            this.cellSize = new Vector2(cellSize, cellSize);
        }
    }

    [CreateAssetMenu(menuName = "Gamio/Shikaku/Game Settings", fileName = "ShikakuGameSettings")]
    public class ShikakuGameSettingsSO : ScriptableObject
    {
        public ShikakuDifficultyConfig easy = new ShikakuDifficultyConfig(5, 120f);
        public ShikakuDifficultyConfig medium = new ShikakuDifficultyConfig(7, 85f);
        public ShikakuDifficultyConfig hard = new ShikakuDifficultyConfig(9, 65f);

        public ShikakuDifficultyConfig GetConfig(Difficulty d) => d switch
        {
            Difficulty.Easy => easy,
            Difficulty.Hard => hard,
            _ => medium
        };
    }
}
