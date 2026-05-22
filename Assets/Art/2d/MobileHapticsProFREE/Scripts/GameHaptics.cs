using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MobileHapticsProFreeEdition
{
    public class GameHaptics : MonoBehaviour
    {
        public static GameHaptics Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LightHaptic()
        {
            TapticWave.TriggerHaptic(HapticModes.Select);
        }

        public void MediumHaptic()
        {
            TapticWave.TriggerHaptic(HapticModes.Confirm);
        }

        public void HighHaptic()
        {
            TapticWave.TriggerHaptic(HapticModes.Alert);
        }

        public void FailureHaptic()
        {
            TapticWave.TriggerHaptic(HapticModes.Failure);
        }

    }
}
