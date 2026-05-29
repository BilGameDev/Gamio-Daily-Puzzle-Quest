namespace Gamio.Core
{
    public interface IGameSeedProvider
    {
        int GetSeed(string gameId, int year, int month, int day);
    }
}
