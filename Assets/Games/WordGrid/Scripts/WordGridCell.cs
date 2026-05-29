namespace Gamio.Games.WordGrid
{
    public enum TileState
    {
        Empty,
        Filled,
        Correct,
        WrongPosition,
        Wrong
    }

    public class WordGridCell
    {
        public int Index;
        public char Letter;
        public char? PlacedLetter;
        public TileState State;

        public WordGridCell(int index, char letter)
        {
            Index = index;
            Letter = letter;
            PlacedLetter = null;
            State = TileState.Empty;
        }
    }
}
