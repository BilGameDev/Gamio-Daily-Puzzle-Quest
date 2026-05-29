namespace Gamio.Games.Sudoku
{
    public struct SudokuCell
    {
        public int Row;
        public int Col;
        public int Value;
        public bool IsGiven;

        public SudokuCell(int row, int col)
        {
            Row = row;
            Col = col;
            Value = 0;
            IsGiven = false;
        }
    }
}
