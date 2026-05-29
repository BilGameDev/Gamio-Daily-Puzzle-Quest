using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gamio.Games.WordSearch
{
    public class WordSearchGenerator
    {
        private readonly int seed;
        private System.Random rng;

        private static readonly (int dr, int dc)[] Directions = new[]
        {
            (-1, -1), (-1, 0), (-1, 1),
            (0, -1),           (0, 1),
            (1, -1),  (1, 0),  (1, 1)
        };

        public WordSearchGenerator(int seedValue)
        {
            seed = seedValue;
        }

        public WordSearchPuzzle Generate(int gridSize, int wordCount, List<string> allWords)
        {
            rng = new System.Random(seed);

            var candidates = allWords
                .Where(w => w.Length >= 3 && w.Length <= gridSize)
                .Select(w => w.ToLowerInvariant())
                .Distinct()
                .OrderBy(x => rng.Next())
                .ToList();

            int targetCount = Mathf.Clamp(wordCount, 1, Mathf.Min(candidates.Count, gridSize * gridSize / 3));
            var placements = new List<WordPlacement>();
            var grid = new char[gridSize, gridSize];
            var wordIndex = new int[gridSize, gridSize];
            for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
            {
                grid[r, c] = ' ';
                wordIndex[r, c] = -1;
            }

            foreach (var word in candidates)
            {
                if (placements.Count >= targetCount) break;

                bool placed = TryPlaceWord(word, grid, gridSize, out var wp);
                if (placed)
                {
                    for (int i = 0; i < word.Length; i++)
                    {
                        int r = wp.StartRow + wp.DirRow * i;
                        int c = wp.StartCol + wp.DirCol * i;
                        grid[r, c] = word[i];
                        wordIndex[r, c] = placements.Count;
                    }
                    placements.Add(wp);
                }
            }

            for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
            {
                if (grid[r, c] == ' ')
                    grid[r, c] = (char)('a' + rng.Next(26));
            }

            var cells = new WordSearchCell[gridSize, gridSize];
            for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
            {
                cells[r, c] = new WordSearchCell
                {
                    Row = r, Col = c,
                    Letter = grid[r, c],
                    WordIndex = wordIndex[r, c]
                };
            }

            var wordList = placements.Select(p => p.Word).ToList();
            return new WordSearchPuzzle(gridSize, cells, placements, wordList);
        }

        private bool TryPlaceWord(string word, char[,] grid, int gridSize, out WordPlacement result)
        {
            int len = word.Length;
            int attempts = 100;

            for (int a = 0; a < attempts; a++)
            {
                int dirIdx = rng.Next(Directions.Length);
                var (dr, dc) = Directions[dirIdx];
                int startR = rng.Next(gridSize);
                int startC = rng.Next(gridSize);

                int endR = startR + dr * (len - 1);
                int endC = startC + dc * (len - 1);
                if (endR < 0 || endR >= gridSize || endC < 0 || endC >= gridSize)
                    continue;

                bool canPlace = true;
                for (int i = 0; i < len; i++)
                {
                    int r = startR + dr * i;
                    int c = startC + dc * i;
                    char existing = grid[r, c];
                    if (existing != ' ' && existing != word[i])
                    {
                        canPlace = false;
                        break;
                    }
                }

                if (canPlace)
                {
                    result = new WordPlacement
                    {
                        Word = word,
                        StartRow = startR,
                        StartCol = startC,
                        DirRow = dr,
                        DirCol = dc
                    };
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}
