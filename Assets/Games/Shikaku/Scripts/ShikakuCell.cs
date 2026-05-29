namespace Gamio.Games.Shikaku
{
    public struct ShikakuCell
    {
        public int Row;
        public int Col;
        public int? Number;
        public int AssignedRectId;
        public bool IsPlayable;

        public ShikakuCell(int row, int col)
        {
            Row = row;
            Col = col;
            Number = null;
            AssignedRectId = -1;
            IsPlayable = true;
        }
    }
}
