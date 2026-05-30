using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gamio.Games.WordGrid
{
    public class WordGridGenerator
    {
        private readonly string seed;
        private System.Random rng;

        public WordGridGenerator(string seed)
        {
            this.seed = seed;
        }

        public WordGridPuzzle Generate(int wordLength, List<string> allWords)
        {
            rng = new System.Random(seed.GetHashCode());

            var candidates = allWords
                .Select(w => w.ToUpperInvariant().Trim())
                .Where(w => w.Length == wordLength)
                .Distinct()
                .OrderBy(x => rng.Next())
                .ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException($"No words of length {wordLength} found.");

            string targetWord = candidates[0];
            return new WordGridPuzzle(targetWord);
        }
    }
}
