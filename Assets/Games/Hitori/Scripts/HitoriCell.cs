namespace Gamio.Games.Hitori
{
    public enum HitoriCellState { None, Black, White }

    public struct HitoriCell
    {
        public int Row;
        public int Col;
        public int Number;
        public bool IsBlackInSolution;
    }
}
