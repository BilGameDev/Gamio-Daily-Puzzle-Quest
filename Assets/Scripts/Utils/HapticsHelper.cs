using Lofelt.NiceVibrations;
using UnityEngine;

namespace Gamio.Features
{
    public static class HapticsHelper
    {
        private const string HapticsPrefKey = "Gamio_HapticsEnabled";

        public static bool Enabled => PlayerPrefs.GetInt(HapticsPrefKey, 1) == 1;

        public static void PlayPreset(HapticPatterns.PresetType preset)
        {
            if (Enabled)
                HapticPatterns.PlayPreset(preset);
        }
    }
}
