using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    internal class SimulatorControlPanel
    {
        private VisualElement m_RootElement = null;

        private SimulatorDeviceSpecificationsUI m_DeviceSpecifications;
        private SimulatorScreenSettingsUI m_SimulatorScreenSettings;
        private SimulatorApplicationSettingsUI m_SimulatorApplicationSettings;
        private SimulatorExtensions m_SimulatorExtensions;
        private readonly Dictionary<string, Foldout> m_ExtensionFoldouts = new Dictionary<string, Foldout>();

        public SimulatorControlPanel(VisualElement rootElement, DeviceInfo deviceInfo, SystemInfoSimulation systemInfoSimulation, ScreenSimulation screenSimulation,
                                     ApplicationSimulation applicationSimulation,
                                     SimulationPlayerSettings playerSettings, SimulatorSerializationStates states)
        {
            m_RootElement = rootElement;

            m_DeviceSpecifications = new SimulatorDeviceSpecificationsUI(m_RootElement.Q<Foldout>("device-specifications"), deviceInfo, systemInfoSimulation);
            m_SimulatorScreenSettings = new SimulatorScreenSettingsUI(m_RootElement.Q<Foldout>("screen-settings"), deviceInfo, screenSimulation, playerSettings);
            m_SimulatorApplicationSettings = new SimulatorApplicationSettingsUI(m_RootElement.Q<Foldout>("application-settings"), applicationSimulation, states);
            m_SimulatorExtensions = new SimulatorExtensions();

            foreach (var extension in m_SimulatorExtensions.Extensions)
            {
                var foldout = new Foldout()
                {
                    text = extension.extensionTitle,
                    value = false
                };
                foldout.AddToClassList("unity-device-simulator__control-panel_foldout");

                m_RootElement.Add(foldout);
                m_ExtensionFoldouts.Add(extension.GetType().ToString(), foldout);

                if (states != null && states.extensions.TryGetValue(extension.GetType().ToString(), out var serializedExtension))
                    JsonUtility.FromJsonOverwrite(serializedExtension, extension);

                extension.OnExtendDeviceSimulator(foldout);
            }
        }

        public void StoreSerializationStates(ref SimulatorSerializationStates states)
        {
            states.controlPanelFoldouts.Add(m_DeviceSpecifications.GetType().ToString(), m_DeviceSpecifications.m_RootElement.value);
            states.controlPanelFoldouts.Add(m_SimulatorScreenSettings.GetType().ToString(), m_SimulatorScreenSettings.m_RootElement.value);
            states.controlPanelFoldouts.Add(m_SimulatorApplicationSettings.GetType().ToString(), m_SimulatorApplicationSettings.m_RootElement.value);

            foreach (var foldout in m_ExtensionFoldouts)
            {
                states.controlPanelFoldouts.Add(foldout.Key, foldout.Value.value);
            }

            foreach (var extension in m_SimulatorExtensions.Extensions)
            {
                var serializedExtension = JsonUtility.ToJson(extension);
                states.extensions.Add(extension.GetType().ToString(), serializedExtension);
            }

            m_SimulatorApplicationSettings.StoreSerializationStates(ref states);
        }

        // TODO get rid of this method when we deprecate DeserializeView, currently exists as a hack
        public void ApplySerializationStates(SimulatorSerializationStates states)
        {
            if (states == null)
                return;

            bool state;
            if (states.controlPanelFoldouts.TryGetValue(m_DeviceSpecifications.GetType().ToString(), out state))
            {
                m_DeviceSpecifications.m_RootElement.value = state;
            }
            if (states.controlPanelFoldouts.TryGetValue(m_SimulatorScreenSettings.GetType().ToString(), out state))
            {
                m_SimulatorScreenSettings.m_RootElement.value = state;
            }
            if (states.controlPanelFoldouts.TryGetValue(m_SimulatorApplicationSettings.GetType().ToString(), out state))
            {
                m_SimulatorApplicationSettings.m_RootElement.value = state;
            }

            foreach (var foldout in m_ExtensionFoldouts)
            {
                if (states.controlPanelFoldouts.TryGetValue(foldout.Key, out state))
                {
                    foldout.Value.value = state;
                }
            }
        }

        // Only gets called during initialization and switching device.
        public void Update(DeviceInfo deviceInfo, SystemInfoSimulation systemInfoSimulation, ScreenSimulation screenSimulation, SimulationPlayerSettings playerSettings)
        {
            if (deviceInfo == null)
                return;

            m_DeviceSpecifications.Update(deviceInfo, systemInfoSimulation);
            m_SimulatorScreenSettings.Update(deviceInfo, screenSimulation, playerSettings);
        }
    }
}
