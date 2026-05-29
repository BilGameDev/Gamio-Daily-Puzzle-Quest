using System;
using UnityEngine;

namespace Gamio.Core
{
    public static class AvatarService
    {
        private const string SeedKey = "Gamio_AvatarSeed";

        public static event Action OnAvatarSeedChanged;

        public static string GetSavedSeed()
        {
            var seed = PlayerPrefs.GetString(SeedKey, "");
            if (string.IsNullOrEmpty(seed))
            {
                seed = GenerateRandomSeed();
                SaveSeed(seed);
            }
            return seed;
        }

        public static void SaveSeed(string seed)
        {
            PlayerPrefs.SetString(SeedKey, seed);
            PlayerPrefs.Save();
            OnAvatarSeedChanged?.Invoke();
        }

        public static string GenerateRandomSeed()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public static string GetAvatarUrl(string seed, int size = 512)
        {
            return $"https://api.dicebear.com/9.x/bottts/png?seed={seed}&size={size}";
        }
    }
}
