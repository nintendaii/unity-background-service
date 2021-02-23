using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    internal class SimulatorDeviceSpecificationsUI
    {
        internal Foldout m_RootElement = null;

        // Controls for device specifications.
        private Label m_OS = null;
        private Label m_CPU = null;
        private Label m_GPU = null;
        private Label m_Resolution = null;

        public SimulatorDeviceSpecificationsUI(Foldout rootElement, DeviceInfo deviceInfo, SystemInfoSimulation systemInfoSimulation)
        {
            m_RootElement = rootElement;

            m_OS = m_RootElement.Q<Label>("device_os");
            m_CPU = m_RootElement.Q<Label>("device_cpu");
            m_GPU = m_RootElement.Q<Label>("device_gpu");
            m_Resolution = m_RootElement.Q<Label>("device_resolution");

            Update(deviceInfo, systemInfoSimulation);
        }

        // Only gets called during initialization and switching device.
        public void Update(DeviceInfo deviceInfo, SystemInfoSimulation systemInfoSimulation)
        {
            m_OS.text = "OS: " + (string.IsNullOrEmpty(deviceInfo.SystemInfo.operatingSystem) ? "N/A" : deviceInfo.SystemInfo.operatingSystem);
            m_CPU.text = "CPU: " + (string.IsNullOrEmpty(deviceInfo.SystemInfo.processorType) ? "N/A" : deviceInfo.SystemInfo.processorType);
            m_GPU.text = "GPU: " + (systemInfoSimulation.GraphicsDependentData == null ? "N/A" : systemInfoSimulation.GraphicsDependentData.graphicsDeviceName);
            m_Resolution.text = $"Resolution: {deviceInfo.Screens[0].width} x {deviceInfo.Screens[0].height}";
        }
    }
}
