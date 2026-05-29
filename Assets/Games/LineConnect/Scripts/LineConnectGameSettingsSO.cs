using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.LineConnect
{
    [System.Serializable]
    public struct DifficultyConfig
    {
        public int gridSize;
        public Vector2 cellSize;

        public DifficultyConfig(int gridSize, float cellSize)
        {
            this.gridSize = gridSize;
            this.cellSize = new Vector2(cellSize, cellSize);
        }
    }

    [CreateAssetMenu(menuName = "Gamio/LineConnect/Game Settings", fileName = "LineConnectGameSettings")]
    public class LineConnectGameSettingsSO : ScriptableObject
    {
        public DifficultyConfig easy = new DifficultyConfig(5, 120);
        public DifficultyConfig medium = new DifficultyConfig(7, 90);
        public DifficultyConfig hard = new DifficultyConfig(9, 70);

        public DifficultyConfig GetConfig(Difficulty d) => d switch
        {
            Difficulty.Easy => easy,
            Difficulty.Hard => hard,
            _ => medium
        };
    }
}