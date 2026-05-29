namespace Gamio.Games.Arrows
{
    public enum ArrowDirection { Up, Down, Left, Right, None }

    public struct ArrowsCell
    {
        public int Row;
        public int Col;
        public ArrowDirection Direction;
        public bool IsEmpty;
        public bool IsObstacle;
    }
}
