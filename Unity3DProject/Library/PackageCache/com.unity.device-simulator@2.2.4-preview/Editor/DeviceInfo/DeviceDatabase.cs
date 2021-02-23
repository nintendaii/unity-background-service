using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace Unity.DeviceSimulator
{
    internal class DeviceDatabase
    {
        public readonly List<DeviceInfo> m_Devices = new List<DeviceInfo>();

        public DeviceDatabase()
        {
            Refresh();
        }

        public void Refresh()
        {
            m_Devices.Clear();

            List<string> deviceDirectoryPaths = new List<string>
            {
                Path.GetFullPath(Path.Combine("Packages", "com.unity.device-simulator", ".DeviceDefinitions")),
                DeviceSimulatorUserSettingsProvider.LoadOrCreateSettings().DeviceDirectory
            };

            // Remove empty or duplicated directories.
            deviceDirectoryPaths.RemoveAll(string.IsNullOrEmpty);
            var filteredDirectoryPaths = deviceDirectoryPaths.Distinct(new DirectoryComparer()).ToArray();

            foreach (var deviceDirectoryPath in filteredDirectoryPaths)
            {
                if (!Directory.Exists(deviceDirectoryPath))
                    continue;

                var deviceDirectory = new DirectoryInfo(deviceDirectoryPath);
                var deviceDefinitions = deviceDirectory.GetFiles("*.device.json");

                foreach (var deviceDefinition in deviceDefinitions)
                {
                    string deviceFileText;
                    using (StreamReader sr = deviceDefinition.OpenText())
                    {
                        deviceFileText = sr.ReadToEnd();
                    }
                    if (!DeviceInfoParse(deviceFileText, out var parseErrors, out var deviceInfo))
                    {
                        Debug.LogWarningFormat("Device Simulator could not parse {0}. Errors found:\n{1}", deviceDefinition.FullName, parseErrors);
                    }
                    else
                    {
                        deviceInfo.Directory = deviceDirectory.FullName;
                        m_Devices.Add(deviceInfo);
                    }
                }
            }

            m_Devices.Sort((x, y) => string.CompareOrdinal(x.friendlyName, y.friendlyName));
        }

        public DeviceInfo GetDevice(int index)
        {
            var deviceInfo =  m_Devices[index];

            if (!deviceInfo.LoadOverlayImage() && deviceInfo.Screens[0].presentation.borderSize == Vector4.zero)
            {
                deviceInfo.Screens[0].presentation.borderSize = new Vector4(40, 60, 40, 60);
            }

            return deviceInfo;
        }

        private class DirectoryComparer : IEqualityComparer<string>
        {
            // There is no easy way to do the directory comparison.
            // Here we check if the platform is case-sensitive or not, then trim the FullName returned by DirectoryInfo.
            public bool Equals(string dir1, string dir2)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dir1 = dir1.ToLower();
                    dir2 = dir2.ToLower();
                }

                var directoryInfo1 = new DirectoryInfo(dir1);
                var directoryInfo2 = new DirectoryInfo(dir2);

                return string.CompareOrdinal(directoryInfo1.FullName.TrimEnd(Path.DirectorySeparatorChar), directoryInfo2.FullName.TrimEnd(Path.DirectorySeparatorChar)) == 0;
            }

            public int GetHashCode(string obj)
            {
                // Here we always return the same hash to make sure Equals() can always be called.
                return string.Empty.GetHashCode();
            }
        }

        public static bool DeviceInfoParse(string deviceJson, out string errors, out DeviceInfo deviceInfo)
        {
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(deviceJson), new XmlDictionaryReaderQuotas());
            var errorsBuilder = new StringBuilder();

            XElement root;
            try
            {
                root = XElement.Load(jsonReader);
            }
            catch (XmlException)
            {
                errorsBuilder.AppendLine("Not a valid JSON.");
                errors = errorsBuilder.ToString();
                deviceInfo = null;
                return false;
            }

            var versionElement = root.Element("version");
            if (versionElement == null)
                errorsBuilder.AppendLine("version field not found.");
            else if (versionElement.Value != "1")
                errorsBuilder.AppendLine("version field is set to an unknown version.");

            var friendlyNameElement = root.Element("friendlyName");
            if (friendlyNameElement == null)
                errorsBuilder.AppendLine("friendlyName field not found.");
            else if (string.IsNullOrEmpty(friendlyNameElement.Value))
                errorsBuilder.AppendLine("friendlyName field is empty.");

            var systemInfoElement = root.Element("SystemInfo");
            if (systemInfoElement == null)
                errorsBuilder.AppendLine("SystemInfo field not found. SystemInfo must have an operatingSystem field, which must be set to a string containing either <android> or <ios>.");
            else
            {
                var operatingSystemElement = systemInfoElement.Element("operatingSystem");
                if (operatingSystemElement == null)
                    errorsBuilder.AppendLine("SystemInfo -> operatingSystem field not found. OperatingSystem field must be set to a string containing either <android> or <ios>.");
                else if (!operatingSystemElement.Value.ToLower().Contains("android") && !operatingSystemElement.Value.ToLower().Contains("ios"))
                    errorsBuilder.AppendLine("SystemInfo -> operatingSystem field must containing either <android> or <ios>.");
            }

            var screensElement = root.Element("Screens");
            if (screensElement == null)
            {
                errorsBuilder.AppendLine("Screens field not found. Screens field must contain at least one screen.");
            }
            else
            {
                var screenElements = screensElement.Elements("item").ToArray();
                if (!screenElements.Any())
                {
                    errorsBuilder.AppendLine("Screens field must contain at least one screen.");
                }
                else
                {
                    for (var i = 0; i < screenElements.Length; i++)
                    {
                        if (screenElements[i].Element("width") == null)
                        {
                            errorsBuilder.AppendLine($"Screens[{i}] -> width field not found.");
                        }

                        if (screenElements[i].Element("height") == null)
                        {
                            errorsBuilder.AppendLine($"Screens[{i}] -> height field not found.");
                        }

                        if (screenElements[i].Element("dpi") == null)
                        {
                            errorsBuilder.AppendLine($"Screens[{i}] -> dpi field not found.");
                        }
                    }
                }
            }

            errors = errorsBuilder.ToString();
            if (errorsBuilder.Length != 0)
            {
                deviceInfo = null;
                return false;
            }

            deviceInfo = JsonUtility.FromJson<DeviceInfo>(deviceJson);
            deviceInfo.AddOptionalFields();
            return true;
        }
    }
}
