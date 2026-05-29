using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.WordGrid
{
    [CreateAssetMenu(menuName = "Gamio/WordGrid/Game Settings", fileName = "WordGridGameSettings")]
    public class WordGridGameSettingsSO : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] private int wordLength = 5;
        [SerializeField] private TextAsset wordList4;
        [SerializeField] private TextAsset wordList6;
        [Header("Colors")]
        [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color wrongPositionColor = new Color(0.8f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.3f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color cellDefaultColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color cellFilledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color tileDefaultColor = new Color(0.22f, 0.22f, 0.22f, 1f);

        public int WordLength => wordLength;
        public TextAsset WordList4 => wordList4;
        public TextAsset WordList6 => wordList6;
        public Color CorrectColor => correctColor;
        public Color WrongPositionColor => wrongPositionColor;
        public Color WrongColor => wrongColor;
        public Color CellDefaultColor => cellDefaultColor;
        public Color CellFilledColor => cellFilledColor;
        public Color TileDefaultColor => tileDefaultColor;
    }
}
