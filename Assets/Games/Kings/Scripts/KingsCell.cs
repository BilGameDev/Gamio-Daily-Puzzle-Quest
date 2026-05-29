namespace Gamio.Games.Kings
{
    public enum KingsCellState { Empty, Null, King }

    public struct KingsCell
    {
        public int Row;
        public int Col;
        public int SectionIndex;
    }
}