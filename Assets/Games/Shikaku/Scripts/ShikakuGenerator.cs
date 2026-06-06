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

        public ShikakuPuzzle Generate(int gridSize, int minArea = 1, int maxArea = 5)
        {
            rng = new Random(seed.GetHashCode());
            var rects = new List<ShikakuRect>();
            PartitionGrid(0, 0, gridSize, gridSize, rects, minArea, maxArea);

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

        private void PartitionGrid(int row, int col, int height, int width, List<ShikakuRect> rects, int minArea, int maxArea)
        {
            var area = height * width;

            if (area <= maxArea)
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
            if (height <= 1)
                splitHorizontal = false;
            else if (width <= 1)
                splitHorizontal = true;
            else
                splitHorizontal = rng.Next(2) == 0;

            int splitPos;
            if (splitHorizontal)
            {
                var minSplit = Math.Max(1, (minArea + width - 1) / width);
                var maxSplit = height - minSplit;
                if (minSplit > maxSplit) { minSplit = height / 2; maxSplit = height / 2; }
                splitPos = rng.Next(minSplit, maxSplit + 1);
                PartitionGrid(row, col, splitPos, width, rects, minArea, maxArea);
                PartitionGrid(row + splitPos, col, height - splitPos, width, rects, minArea, maxArea);
            }
            else
            {
                var minSplit = Math.Max(1, (minArea + height - 1) / height);
                var maxSplit = width - minSplit;
                if (minSplit > maxSplit) { minSplit = width / 2; maxSplit = width / 2; }
                splitPos = rng.Next(minSplit, maxSplit + 1);
                PartitionGrid(row, col, height, splitPos, rects, minArea, maxArea);
                PartitionGrid(row, col + splitPos, height, width - splitPos, rects, minArea, maxArea);
            }
        }
    }
}
