using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.WordSearch
{
    [System.Serializable]
    public struct WordSearchDifficultyConfig
    {
        public int gridSize;
        public int wordCount;
        public Vector2 cellSize;

        public WordSearchDifficultyConfig(int gridSize, int wordCount, float cellSize)
        {
            this.gridSize = gridSize;
            this.wordCount = wordCount;
            this.cellSize = new Vector2(cellSize, cellSize);
        }
    }

    [CreateAssetMenu(menuName = "Gamio/WordSearch/Game Settings", fileName = "WordSearchGameSettings")]
    public class WordSearchGameSettingsSO : ScriptableObject
    {
        [Header("Difficulty Configs")]
        public WordSearchDifficultyConfig easy = new WordSearchDifficultyConfig(8, 4, 100f);
        public WordSearchDifficultyConfig medium = new WordSearchDifficultyConfig(12, 6, 75f);
        public WordSearchDifficultyConfig hard = new WordSearchDifficultyConfig(15, 8, 60f);
        [Header("Word List")]
        [SerializeField] private TextAsset wordList;

        public TextAsset WordList => wordList;

        public WordSearchDifficultyConfig GetConfig(Difficulty d) => d switch
        {
            Difficulty.Easy => easy,
            Difficulty.Hard => hard,
            _ => medium
        };
    }
}
