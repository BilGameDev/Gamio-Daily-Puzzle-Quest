using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Arrows
{
    [System.Serializable]
    public struct ArrowsDifficultyConfig
    {
        public int rows;
        public int cols;
        [Range(0.1f, 1.0f)] public float density;
        public Vector2 cellSize;

        public ArrowsDifficultyConfig(int rows, int cols, float density, float cellSize)
        {
            this.rows = rows;
            this.cols = cols;
            this.density = density;
            this.cellSize = new Vector2(cellSize, cellSize);
        }
    }

    [CreateAssetMenu(menuName = "Gamio/Arrows/Game Settings", fileName = "ArrowsGameSettings")]
    public class ArrowsGameSettingsSO : ScriptableObject
    {
        [Header("Difficulty Configs")]
        public ArrowsDifficultyConfig easy = new ArrowsDifficultyConfig(4, 4, 0.5f, 130f);
        public ArrowsDifficultyConfig medium = new ArrowsDifficultyConfig(5, 5, 0.55f, 105f);
        public ArrowsDifficultyConfig hard = new ArrowsDifficultyConfig(6, 6, 0.6f, 85f);

        [Header("Tile Colors")]
        public Color tileColor = new Color(0.25f, 0.30f, 0.45f);
        public Color tileArrowColor = Color.white;
        public Color obstacleColor = new Color(0.12f, 0.12f, 0.14f);
        public Color flashColor = Color.red;

        [Header("Animation")]
        public float slideDuration = 0.35f;
        public DG.Tweening.Ease slideEase = DG.Tweening.Ease.InBack;
        public float shakeDuration = 0.25f;
        public float shakeStrength = 8f;
        public int shakeVibrato = 10;
        public float flashDuration = 0.1f;
        public int flashLoopCount = 2;

        public ArrowsDifficultyConfig GetConfig(Difficulty d) => d switch
        {
            Difficulty.Easy => easy,
            Difficulty.Hard => hard,
            _ => medium
        };
    }
}