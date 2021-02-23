using System;
using UnityEngine;

namespace Unity.DeviceSimulator
{
    internal class DeviceSimulatorUserSettings : ScriptableObject
    {
        [SerializeField]
        public string DeviceDirectory = String.Empty;
        [SerializeField]
        public Color SafeAreaHighlightColor = Color.green;
        [SerializeField]
        public int SafeAreaHighlightLineWidth = 2;
        [SerializeField]
        public int MaximumVisibleDeviceCount = 20;
    }
}
