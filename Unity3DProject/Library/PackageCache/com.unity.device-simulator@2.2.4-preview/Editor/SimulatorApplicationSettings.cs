using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    internal class SimulatorApplicationSettingsUI
    {
        internal Foldout m_RootElement = null;

        private ApplicationSimulation m_ApplicationSimulation = null;

        private EnumField m_SystemLanguageEnumField;
        private EnumField m_InternetReachabilityEnumField;

        public SimulatorApplicationSettingsUI(Foldout rootElement, ApplicationSimulation applicationSimulation, SimulatorSerializationStates states)
        {
            m_RootElement = rootElement;
            m_ApplicationSimulation = applicationSimulation;

            m_SystemLanguageEnumField = m_RootElement.Q<EnumField>("application-system-language");
            m_SystemLanguageEnumField.Init(states?.systemLanguage ?? SystemLanguage.English);
            m_ApplicationSimulation.ShimmedSystemLanguage = (SystemLanguage)m_SystemLanguageEnumField.value;
            m_SystemLanguageEnumField.RegisterValueChangedCallback((evt) => { m_ApplicationSimulation.ShimmedSystemLanguage = (SystemLanguage)evt.newValue; });

            m_InternetReachabilityEnumField = m_RootElement.Q<EnumField>("application-internet-reachability");
            m_InternetReachabilityEnumField.Init(states?.networkReachability ?? NetworkReachability.NotReachable);
            m_ApplicationSimulation.ShimmedInternetReachability = (NetworkReachability)m_InternetReachabilityEnumField.value;
            m_InternetReachabilityEnumField.RegisterValueChangedCallback((evt) => { m_ApplicationSimulation.ShimmedInternetReachability = (NetworkReachability)evt.newValue; });

            var onLowMemoryButton = m_RootElement.Q<Button>("application-low-memory");
            onLowMemoryButton.clickable = new Clickable(() => m_ApplicationSimulation.OnLowMemory());
        }

        public void StoreSerializationStates(ref SimulatorSerializationStates states)
        {
            states.systemLanguage = (SystemLanguage)m_SystemLanguageEnumField.value;
            states.networkReachability = (NetworkReachability)m_InternetReachabilityEnumField.value;
        }
    }
}
