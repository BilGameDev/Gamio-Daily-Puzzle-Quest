namespace Gamio.Games.Pipes
{
    public enum PipeType { Empty, Straight, Bend, Cross, TJunction }

    public struct PipesCell
    {
        public int Row;
        public int Col;
        public PipeType Type;
        public bool IsFixed;
        public bool IsPort;
        public int PortDirection;
    }
}
