using System;
using System.Collections;

namespace Gamio.Core
{
    public interface IGame
    {
        string GameId { get; }
        string DisplayName { get; }
        public event Action OnSolved;
        void Initialize();
        IEnumerator Run(string seed, Difficulty difficulty);
        void Cleanup();
    }
}
