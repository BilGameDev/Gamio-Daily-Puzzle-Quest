using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Hitori
{
    [CreateAssetMenu(menuName = "Gamio/Hitori/Game Settings", fileName = "HitoriGameSettings")]
    public class HitoriGameSettingsSO : ScriptableObject
    {
        public int gridSize = 7;
        public int gridSizeEasy = 5;
        public int gridSizeHard = 9;

        public int GetGridSize(Difficulty d) => d switch { Difficulty.Easy => gridSizeEasy, Difficulty.Hard => gridSizeHard, _ => gridSize };
    }
}
