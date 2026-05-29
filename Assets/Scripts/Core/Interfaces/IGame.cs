using System;
using System.Collections;

namespace Gamio.Core
{
    public interface IGame
    {
        string GameId { get; }
        string DisplayName { get; }
        event Action OnSolved;
        void Initialize();
        IEnumerator Run(int seed, Difficulty difficulty);
        void Cleanup();
    }
}
