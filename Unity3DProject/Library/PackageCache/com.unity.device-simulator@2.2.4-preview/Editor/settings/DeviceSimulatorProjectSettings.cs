using UnityEngine;

namespace Unity.DeviceSimulator
{
    internal class DeviceSimulatorProjectSettings : ScriptableObject
    {
        [SerializeField] public bool SystemInfoDefaultAssembly;
        [SerializeField] public string[] SystemInfoAssemblies;

        public DeviceSimulatorProjectSettings()
        {
            SystemInfoDefaultAssembly = true;
            SystemInfoAssemblies = new string[0];
        }
    }
}
