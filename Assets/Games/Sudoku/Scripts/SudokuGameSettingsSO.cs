using Gamio.Core;
using UnityEngine;

namespace Gamio.Games.Sudoku
{
    [CreateAssetMenu(menuName = "Gamio/Sudoku/Game Settings", fileName = "SudokuGameSettings")]
    public class SudokuGameSettingsSO : ScriptableObject
    {
        [Header("Grid")]
        [SerializeField] private int gridSize = 9;
        [SerializeField] private int boxSize = 3;

        [Header("Difficulty")]
        [SerializeField] private int cellsToRemove = 45;
        [SerializeField] private int cellsToRemoveEasy = 36;
        [SerializeField] private int cellsToRemoveHard = 54;

        public int GridSize => gridSize;
        public int BoxSize => boxSize;
        public int CellsToRemove => cellsToRemove;
        public int CellsToRemoveEasy => cellsToRemoveEasy;
        public int CellsToRemoveHard => cellsToRemoveHard;

        public int GetCellsToRemove(Difficulty d) => d switch { Difficulty.Easy => cellsToRemoveEasy, Difficulty.Hard => cellsToRemoveHard, _ => cellsToRemove };
    }
}
