using UnityEngine;

namespace Gamio.Games.Shikaku
{
    public struct ShikakuRect
    {
        public int Id;
        public int Row;
        public int Col;
        public int Height;
        public int Width;
        public int Number;
        public Color Color;

        public readonly int Area => Height * Width;
        public readonly int Bottom => Row + Height - 1;
        public readonly int Right => Col + Width - 1;

        public bool ContainsCell(int r, int c)
        {
            return r >= Row && r <= Bottom && c >= Col && c <= Right;
        }

        public bool Overlaps(ShikakuRect other)
        {
            return Row <= other.Bottom && Bottom >= other.Row &&
                   Col <= other.Right && Right >= other.Col;
        }
    }
}
