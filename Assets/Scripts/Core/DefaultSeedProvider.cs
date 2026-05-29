using System;

namespace Gamio.Core
{
    public class DefaultSeedProvider : IGameSeedProvider
    {
        public string CloudSeed { get; set; }
        public int? RandomSeedOverride { get; set; }

        public int GetSeed(string gameId, int year, int month, int day)
        {
            if (!string.IsNullOrEmpty(CloudSeed))
                return StableHash(CloudSeed, gameId, year, month, day);
            if (RandomSeedOverride.HasValue)
                return StableHash(RandomSeedOverride.Value, gameId, year, month, day);
            return StableHash(gameId, year, month, day);
        }

        public static int GetSeedFromCloudSeed(string cloudSeed, string gameId)
        {
            var now = DateTime.Today;
            return StableHash(cloudSeed, gameId, now.Year, now.Month, now.Day);
        }

        private static int StableHash(params object[] inputs)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var input in inputs)
                {
                    string str = input?.ToString() ?? "";
                    foreach (char c in str)
                    {
                        hash ^= c;
                        hash *= 16777619;
                    }
                }
                return (int)hash;
            }
        }
    }
}
