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

        public static void PlaySoftImpact()
        {
            PlayPreset(HapticPatterns.PresetType.SoftImpact);
        }

        public static void PlayEmphasis(float amplitude = 0.4f, float frequency = 0.5f)
        {
            if (Enabled)
                HapticPatterns.PlayEmphasis(amplitude, frequency);
        }

        public static void PlayConstant(float amplitude, float frequency, float duration)
        {
            if (Enabled)
                HapticPatterns.PlayConstant(amplitude, frequency, duration);
        }

        public static void UpdateContinuous(float amplitude, float frequencyShift)
        {
            if (Enabled && HapticController.IsPlaying())
            {
                HapticController.clipLevel = Mathf.Clamp01(amplitude);
                HapticController.clipFrequencyShift = Mathf.Clamp(frequencyShift, -1f, 1f);
            }
        }

        public static void StopContinuous()
        {
            if (HapticController.IsPlaying())
                HapticController.Stop();
        }
    }
}
