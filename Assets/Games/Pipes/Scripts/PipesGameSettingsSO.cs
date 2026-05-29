using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Pipes
{
    [System.Serializable]
    public struct PipesDifficultyConfig
    {
        public int gridSize;
        public Vector2 cellSize;

        public PipesDifficultyConfig(int gridSize, float cellSize)
        {
            this.gridSize = gridSize;
            this.cellSize = new Vector2(cellSize, cellSize);
        }
    }

    [CreateAssetMenu(menuName = "Gamio/Pipes/Game Settings", fileName = "PipesGameSettings")]
    public class PipesGameSettingsSO : ScriptableObject
    {
        public PipesDifficultyConfig easy = new PipesDifficultyConfig(4, 120f);
        public PipesDifficultyConfig medium = new PipesDifficultyConfig(6, 85f);
        public PipesDifficultyConfig hard = new PipesDifficultyConfig(8, 65f);

        public PipesDifficultyConfig GetConfig(Difficulty d) => d switch
        {
            Difficulty.Easy => easy,
            Difficulty.Hard => hard,
            _ => medium
        };
    }
}
