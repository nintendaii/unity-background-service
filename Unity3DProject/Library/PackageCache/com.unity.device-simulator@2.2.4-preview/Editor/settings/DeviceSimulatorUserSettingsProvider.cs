using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    internal class DeviceSimulatorUserSettingsProvider : SettingsProvider
    {
        private TextField m_CustomizedDeviceDirectoryField = null;

        private static DeviceSimulatorUserSettings s_Settings;

        private const string k_UserSettingsPreferenceKey = "DeviceSimulatorUserSettings";

        private SerializedObject SerializedSettings => new SerializedObject(LoadOrCreateSettings());

        public DeviceSimulatorUserSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateDeviceSimulatorSettingsProvider()
        {
            var provider = new DeviceSimulatorUserSettingsProvider("Preferences/Device Simulator", SettingsScope.User);

            provider.activateHandler = (searchContext, rootElement) =>
            {
                provider.InitUI(rootElement);
            };

            return provider;
        }

        private void InitUI(VisualElement rootElement)
        {
            var settings = LoadOrCreateSettings();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("packages/com.unity.device-simulator/SimulatorResources/UXML/ui_user_settings.uxml");
            visualTree.CloneTree(rootElement);
            rootElement.Bind(new SerializedObject(settings));

            // Don't bind the device directory as we need to validate the directory before setting to DeviceSimulatorUserSettings.
            var textField = rootElement.Q<TextField>("customized-device-directory");
            textField.isDelayed = true;
            textField.SetValueWithoutNotify(settings.DeviceDirectory);
            textField.RegisterValueChangedCallback(SetCustomizedDeviceDirectory);
            m_CustomizedDeviceDirectoryField = textField;

            rootElement.Q<Button>("browse-customized-device-directory").clickable = new Clickable(BrowseCustomizedDeviceDirectory);
        }

        private void SetCustomizedDeviceDirectory(ChangeEvent<string> evt)
        {
            // We allow users to set the directory to empty.
            var directory = evt.newValue;
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Debug.LogWarning($"Input device directory '{directory}' is invalid.");
                return;
            }

            LoadOrCreateSettings().DeviceDirectory = directory;
        }

        private void BrowseCustomizedDeviceDirectory()
        {
            var settings = LoadOrCreateSettings();

            var directory = EditorUtility.OpenFolderPanel("Select directory", settings.DeviceDirectory, String.Empty);
            if (string.IsNullOrEmpty(directory))
                return;

            settings.DeviceDirectory = directory;
            m_CustomizedDeviceDirectoryField.SetValueWithoutNotify(directory);
        }

        public static DeviceSimulatorUserSettings LoadOrCreateSettings()
        {
            if (s_Settings != null)
                return s_Settings;

            DeviceSimulatorUserSettings settings = ScriptableObject.CreateInstance<DeviceSimulatorUserSettings>();
            try
            {
                var settingsString = EditorPrefs.GetString(k_UserSettingsPreferenceKey, "");
                if (!string.IsNullOrEmpty(settingsString))
                    JsonUtility.FromJsonOverwrite(settingsString, settings);
            }
            catch (Exception)
            {
            }

            s_Settings = settings;
            return settings;
        }

        private void SaveSettings()
        {
            if (s_Settings == null)
                return;

            // For now we only store a string to the EditorPrefs, please make sure we don't store too long string here.
            // Otherwise we have to save into a file and store the file path in EditorPrefs.
            var settingsString = JsonUtility.ToJson(s_Settings);
            if (!string.IsNullOrEmpty(settingsString))
                EditorPrefs.SetString(k_UserSettingsPreferenceKey, settingsString);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            SaveSettings();
        }
    }
}
