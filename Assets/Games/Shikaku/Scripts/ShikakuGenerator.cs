using System;
using System.Collections.Generic;

namespace Gamio.Games.Shikaku
{
    public class ShikakuGenerator
    {
        private readonly string seed;
        private Random rng;

        public ShikakuGenerator(string s)
        {
            seed = s;
        }

        public ShikakuPuzzle Generate(int gridSize)
        {
            rng = new Random(seed.GetHashCode());
            var rects = new List<ShikakuRect>();
            PartitionGrid(0, 0, gridSize, gridSize, rects);

            var cells = new ShikakuCell[gridSize, gridSize];
            for (var r = 0; r < gridSize; r++)
            for (var c = 0; c < gridSize; c++)
                cells[r, c] = new ShikakuCell(r, c);

            for (var i = 0; i < rects.Count; i++)
            {
                var rect = rects[i];
                var rectWithId = rect;
                rectWithId.Id = i;
                rects[i] = rectWithId;

                var centerR = rect.Row + rect.Height / 2;
                var centerC = rect.Col + rect.Width / 2;
                cells[centerR, centerC] = new ShikakuCell(centerR, centerC)
                {
                    Number = rect.Area,
                    AssignedRectId = i,
                    IsPlayable = true
                };
            }

            return new ShikakuPuzzle(cells, rects.AsReadOnly());
        }

        private void PartitionGrid(int row, int col, int height, int width, List<ShikakuRect> rects)
        {
            var area = height * width;
            var minSize = 1;
            var maxSize = 5;

            if (area <= maxSize || (height <= maxSize && width <= maxSize))
            {
                rects.Add(new ShikakuRect
                {
                    Id = rects.Count,
                    Row = row,
                    Col = col,
                    Height = height,
                    Width = width,
                    Number = area
                });
                return;
            }

            bool splitHorizontal;
            int splitPos;

            if (height <= minSize * 2)
            {
                splitHorizontal = false;
            }
            else if (width <= minSize * 2)
            {
                splitHorizontal = true;
            }
            else
            {
                splitHorizontal = rng.Next(2) == 0;
            }

            if (splitHorizontal)
            {
                var minSplit = Math.Max(minSize, height / 4);
                var maxSplit = Math.Min(height - minSize, height * 3 / 4);
                splitPos = rng.Next(minSplit, maxSplit + 1);
                if (splitPos <= 0 || splitPos >= height) splitPos = height / 2;

                PartitionGrid(row, col, splitPos, width, rects);
                PartitionGrid(row + splitPos, col, height - splitPos, width, rects);
            }
            else
            {
                var minSplit = Math.Max(minSize, width / 4);
                var maxSplit = Math.Min(width - minSize, width * 3 / 4);
                splitPos = rng.Next(minSplit, maxSplit + 1);
                if (splitPos <= 0 || splitPos >= width) splitPos = width / 2;

                PartitionGrid(row, col, height, splitPos, rects);
                PartitionGrid(row, col + splitPos, height, width - splitPos, rects);
            }
        }
    }
}
