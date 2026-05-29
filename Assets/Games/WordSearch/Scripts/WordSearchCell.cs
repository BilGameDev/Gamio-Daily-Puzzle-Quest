namespace Gamio.Games.WordSearch
{
    public struct WordPlacement
    {
        public string Word;
        public int StartRow;
        public int StartCol;
        public int DirRow;
        public int DirCol;
    }

    public struct WordSearchCell
    {
        public int Row;
        public int Col;
        public char Letter;
        public int WordIndex;
    }
}
