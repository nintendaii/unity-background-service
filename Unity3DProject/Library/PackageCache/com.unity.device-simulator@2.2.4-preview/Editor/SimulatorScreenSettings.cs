using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    internal class SimulatorScreenSettingsUI
    {
        internal Foldout m_RootElement = null;

        // Controls for screen settings.
        private IntegerField m_ScreenWidthField = null;
        private IntegerField m_ScreenHeightField = null;
        private Button m_ScreenSetResolution = null;

        private Toggle m_FullScreenToggle = null;

        private Toggle m_AutoRotationToggle = null;
        private EnumField m_ScreenOrientationEnumField = null;
        private VisualElement m_RenderedOrientationContainer = null;
        private Label m_RendererOrientation = null;

        private VisualElement m_AllowedOrientationsSection = null;
        private Toggle m_AllowedPortrait = null;
        private Toggle m_AllowedPortraitUpsideDown = null;
        private Toggle m_AllowedLandscapeLeft = null;
        private Toggle m_AllowedLandscapeRight = null;

        public SimulatorScreenSettingsUI(Foldout rootElement, DeviceInfo deviceInfo, ScreenSimulation screenSimulation, SimulationPlayerSettings playerSettings)
        {
            m_RootElement = rootElement;
            Init(deviceInfo, screenSimulation, playerSettings);
        }

        private void Init(DeviceInfo deviceInfo, ScreenSimulation screenSimulation, SimulationPlayerSettings playerSettings)
        {
            m_ScreenWidthField = m_RootElement.Q<IntegerField>("screen-width");
            m_ScreenHeightField = m_RootElement.Q<IntegerField>("screen-height");

            m_ScreenSetResolution = m_RootElement.Q<Button>("screen-set-resolution-button");
            m_ScreenSetResolution.clickable = new Clickable(SetResolution);

            m_FullScreenToggle = m_RootElement.Q<Toggle>("full-screen");
            m_FullScreenToggle.RegisterValueChangedCallback(SetFullScreen);

            m_AutoRotationToggle = m_RootElement.Q<Toggle>("auto-rotation");
            m_AutoRotationToggle.RegisterValueChangedCallback(SetAutoRotation);

            m_ScreenOrientationEnumField = m_RootElement.Q<EnumField>("screen-orientations");
            m_ScreenOrientationEnumField.Init(RenderedScreenOrientation.Portrait);
            m_ScreenOrientationEnumField.RegisterValueChangedCallback(SetScreenOrientation);

            m_RenderedOrientationContainer = m_RootElement.Q<VisualElement>("rendered-orientation-container");
            m_RendererOrientation = m_RootElement.Q<Label>("rendered-orientation");

            m_AllowedOrientationsSection = m_RootElement.Q<VisualElement>("allowed-orientations");

            m_AllowedPortrait = m_RootElement.Q<Toggle>("orientation-allow-portrait");
            m_AllowedPortrait.RegisterValueChangedCallback((evt) => { Screen.autorotateToPortrait = evt.newValue; });

            m_AllowedPortraitUpsideDown = m_RootElement.Q<Toggle>("orientation-allow-portrait-upside-down");
            m_AllowedPortraitUpsideDown.RegisterValueChangedCallback((evt) => { Screen.autorotateToPortraitUpsideDown = evt.newValue; });

            m_AllowedLandscapeLeft = m_RootElement.Q<Toggle>("orientation-allow-landscape-left");
            m_AllowedLandscapeLeft.RegisterValueChangedCallback((evt) => { Screen.autorotateToLandscapeLeft = evt.newValue; });

            m_AllowedLandscapeRight = m_RootElement.Q<Toggle>("orientation-allow-landscape-right");
            m_AllowedLandscapeRight.RegisterValueChangedCallback((evt) => { Screen.autorotateToLandscapeRight = evt.newValue; });

            // Initialized the control states.
            UpdateOrientationVisualElements(screenSimulation.AutoRotation);
            UpdateAllowedOrientationVisualElements();

            Update(deviceInfo, screenSimulation, playerSettings);
        }

        private void SetResolution()
        {
            Screen.SetResolution(m_ScreenWidthField.value, m_ScreenHeightField.value, Screen.fullScreen);
        }

        private void SetFullScreen(ChangeEvent<bool> evt)
        {
            Screen.fullScreen = evt.newValue;
        }

        private void SetAutoRotation(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                Screen.orientation = ScreenOrientation.AutoRotation;
            else
                Screen.orientation = (ScreenOrientation)m_ScreenOrientationEnumField.value;
        }

        private void SetScreenOrientation(ChangeEvent<Enum> evt)
        {
            Screen.orientation = (ScreenOrientation)evt.newValue;
        }

        private void UpdateFullScreenToggle(DeviceInfo deviceInfo, SimulationPlayerSettings playerSettings)
        {
            bool isAndroid = deviceInfo.SystemInfo.operatingSystem.ToLower().Contains("android");
            if (isAndroid)
            {
                m_FullScreenToggle.SetValueWithoutNotify(playerSettings.androidStartInFullscreen);
                m_FullScreenToggle.SetEnabled(true);
            }
            else
            {
                m_FullScreenToggle.SetValueWithoutNotify(true);
                m_FullScreenToggle.SetEnabled(false);
            }
        }

        private void UpdateFullScreenToggle(bool fullScreen)
        {
            m_FullScreenToggle.SetValueWithoutNotify(fullScreen);
        }

        private void UpdateOrientationVisualElements(bool autoRotation)
        {
            m_AutoRotationToggle.SetValueWithoutNotify(autoRotation);
            UpdateOrientationDisplay();

            // Decide if we need to show/hide the visual elements by AutoRotation.
            m_ScreenOrientationEnumField.style.visibility = autoRotation ? Visibility.Hidden : Visibility.Visible;
            m_ScreenOrientationEnumField.style.position = autoRotation ? Position.Absolute : Position.Relative;

            m_RenderedOrientationContainer.style.visibility = autoRotation ? Visibility.Visible : Visibility.Hidden;
            m_RenderedOrientationContainer.style.position = autoRotation ? Position.Relative : Position.Absolute;

            m_AllowedOrientationsSection.style.visibility = autoRotation ? Visibility.Visible : Visibility.Hidden;
            m_AllowedOrientationsSection.style.position = autoRotation ? Position.Relative : Position.Absolute;
        }

        private void UpdateOrientationDisplay()
        {
            m_ScreenOrientationEnumField.SetValueWithoutNotify((RenderedScreenOrientation)Screen.orientation);
            m_RendererOrientation.text = ObjectNames.NicifyVariableName(Screen.orientation.ToString());
        }

        private void UpdateAllowedOrientationVisualElements()
        {
            m_AllowedPortrait.SetValueWithoutNotify(Screen.autorotateToPortrait);
            m_AllowedPortraitUpsideDown.SetValueWithoutNotify(Screen.autorotateToPortraitUpsideDown);
            m_AllowedLandscapeLeft.SetValueWithoutNotify(Screen.autorotateToLandscapeLeft);
            m_AllowedLandscapeRight.SetValueWithoutNotify(Screen.autorotateToLandscapeRight);

            UpdateOrientationDisplay();
        }

        // Only gets called during initialization and switching device.
        public void Update(DeviceInfo deviceInfo, ScreenSimulation screenSimulation, SimulationPlayerSettings playerSettings)
        {
            UpdateResolution(screenSimulation.Width, screenSimulation.Height);
            UpdateFullScreenToggle(deviceInfo, playerSettings);
            UpdateOrientationVisualElements(screenSimulation.AutoRotation);
            UpdateAllowedOrientationVisualElements();

            // Register callbacks.
            screenSimulation.OnOrientationChanged += UpdateOrientationVisualElements;
            screenSimulation.OnAllowedOrientationChanged += UpdateAllowedOrientationVisualElements;
            screenSimulation.OnResolutionChanged += UpdateResolution;
            screenSimulation.OnFullScreenChanged += UpdateFullScreenToggle;
        }

        private void UpdateResolution(int width, int height)
        {
            m_ScreenWidthField.SetValueWithoutNotify(width);
            m_ScreenHeightField.SetValueWithoutNotify(height);
        }
    }
}
