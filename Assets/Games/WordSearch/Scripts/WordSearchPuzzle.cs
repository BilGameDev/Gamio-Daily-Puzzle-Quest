using System.Collections.Generic;
using System.Linq;

namespace Gamio.Games.WordSearch
{
    public class WordSearchPuzzle
    {
        public int GridSize { get; }
        public WordSearchCell[,] Cells { get; }
        public IReadOnlyList<WordPlacement> Placements { get; }
        public IReadOnlyList<string> WordList { get; }

        private readonly HashSet<string> foundWords;

        public WordSearchPuzzle(int gridSize, WordSearchCell[,] cells, IReadOnlyList<WordPlacement> placements, IReadOnlyList<string> wordList)
        {
            GridSize = gridSize;
            Cells = cells;
            Placements = placements;
            WordList = wordList;
            foundWords = new HashSet<string>();
        }

        public bool IsWordFound(string word) => foundWords.Contains(word);

        public bool TryFindWord(int startR, int startC, int endR, int endC, out string foundWord)
        {
            foundWord = null;

            int dr = endR - startR;
            int dc = endC - startC;
            int len = System.Math.Max(System.Math.Abs(dr), System.Math.Abs(dc)) + 1;

            if (len < 3) return false;

            int ndr = dr == 0 ? 0 : dr / System.Math.Abs(dr);
            int ndc = dc == 0 ? 0 : dc / System.Math.Abs(dc);

            if (dr != 0 && dc != 0 && System.Math.Abs(dr) != System.Math.Abs(dc))
                return false;

            char[] chars = new char[len];
            for (int i = 0; i < len; i++)
            {
                int r = startR + ndr * i;
                int c = startC + ndc * i;
                if (r < 0 || r >= GridSize || c < 0 || c >= GridSize)
                    return false;
                chars[i] = Cells[r, c].Letter;
            }

            string word = new string(chars);

            if (foundWords.Contains(word))
                return false;

            if (!WordList.Contains(word))
            {
                char[] rev = chars;
                System.Array.Reverse(rev);
                word = new string(rev);
                if (foundWords.Contains(word) || !WordList.Contains(word))
                    return false;
            }

            foundWords.Add(word);
            foundWord = word;
            return true;
        }

        public bool CheckAllFound()
        {
            return WordList.All(w => foundWords.Contains(w));
        }

        public bool IsCellFound(int row, int col)
        {
            int idx = Cells[row, col].WordIndex;
            if (idx < 0) return false;
            return foundWords.Contains(Placements[idx].Word);
        }
    }
}
