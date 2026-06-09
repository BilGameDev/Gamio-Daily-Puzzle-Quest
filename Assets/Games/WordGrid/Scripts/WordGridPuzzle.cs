using System.Collections.Generic;

namespace Gamio.Games.WordGrid
{
    public class WordGridPuzzle
    {
        public string TargetWord { get; }
        public int WordLength => TargetWord.Length;
        public WordGridCell[] Cells { get; }

        public WordGridPuzzle(string targetWord)
        {
            TargetWord = targetWord.ToUpperInvariant();
            Cells = new WordGridCell[WordLength];
            for (int i = 0; i < WordLength; i++)
            {
                Cells[i] = new WordGridCell(i, TargetWord[i]);
            }
        }

        public bool AllCorrect()
        {
            for (int i = 0; i < WordLength; i++)
                if (Cells[i].State != TileState.Correct)
                    return false;
            return true;
        }

        public string GetCurrentGuess()
        {
            char[] chars = new char[WordLength];
            for (int i = 0; i < WordLength; i++)
                chars[i] = Cells[i].PlacedLetter ?? ' ';
            return new string(chars).Trim();
        }

        public bool IsFullyFilled()
        {
            for (int i = 0; i < WordLength; i++)
                if (Cells[i].PlacedLetter == null)
                    return false;
            return true;
        }

        public void Reset(bool preserveCorrect = false)
        {
            for (int i = 0; i < WordLength; i++)
            {
                if (preserveCorrect && Cells[i].State == TileState.Correct) continue;
                Cells[i].PlacedLetter = null;
                Cells[i].State = TileState.Empty;
            }
        }
    }
}
