using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobileHapticsProFreeEdition
{
    public enum HapticModes
    {
        Select,
        Confirm,
        Alert,
        Failure,

        VerySoftTap,
        SoftTap,
        MediumTap,
        HardTap,
        HalfSecondWave,
        OneSecondWave,
        TwoSecondsWave
    }

    public static class TapticWave
    {
        // Toggle for enabling/disabling the Haptic feedback
        public static bool TapticOn = true;

        public static void TriggerHaptic(HapticModes mode)
        {
            if (!TapticOn || Application.isEditor)
                return;

#if UNITY_ANDROID
            TriggerAndroidHaptic(mode);
#endif
        }

        private static void TriggerAndroidHaptic(HapticModes mode)
        {
            AndroidTapticWave.Haptic(mode);
        }
    }
}