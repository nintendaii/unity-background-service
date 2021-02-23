using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.DeviceSimulator
{
    internal enum RenderedScreenOrientation
    {
        Portrait = 1,
        PortraitUpsideDown = 2,
        LandscapeLeft = 3,
        LandscapeRight = 4,
    }

    internal enum ResolutionScalingMode
    {
        Disabled = 0,
        FixedDpi = 1
    }

    internal enum SimulationState{ Enabled, Disabled }

    [Serializable]
    internal class SimulatorSerializationStates
    {
        public bool controlPanelHidden = false;
        public float controlPanelWidth = 300f;

        public Dictionary<string, bool> controlPanelFoldouts = new Dictionary<string, bool>();
        public List<string> controlPanelFoldoutKeys = new List<string>();
        public List<bool> controlPanelFoldoutValues = new List<bool>();

        public Dictionary<string, string> extensions = new Dictionary<string, string>();
        public List<string> extensionNames = new List<string>();
        public List<string> extensionStates = new List<string>();

        public int scale = 20;
        public bool fitToScreenEnabled = true;

        public int rotationDegree = 0;
        [NonSerialized]
        public Quaternion rotation = Quaternion.identity;

        public bool highlightSafeAreaEnabled = false;
        public string friendlyName = string.Empty;

        public NetworkReachability networkReachability = NetworkReachability.NotReachable;
        public SystemLanguage systemLanguage = SystemLanguage.English;
    }

    internal static class SimulatorUtilities
    {
        public static ScreenOrientation ToScreenOrientation(UIOrientation original)
        {
            switch (original)
            {
                case UIOrientation.Portrait:
                    return ScreenOrientation.Portrait;
                case UIOrientation.PortraitUpsideDown:
                    return ScreenOrientation.PortraitUpsideDown;
                case UIOrientation.LandscapeLeft:
                    return ScreenOrientation.LandscapeLeft;
                case UIOrientation.LandscapeRight:
                    return ScreenOrientation.LandscapeRight;
                case UIOrientation.AutoRotation:
                    return ScreenOrientation.AutoRotation;
            }
            throw new ArgumentException($"Unexpected value of UIOrientation {original}");
        }

        public static ScreenOrientation RotationToScreenOrientation(Quaternion rotation)
        {
            return RotationToScreenOrientation(rotation.eulerAngles.z);
        }

        public static ScreenOrientation RotationToScreenOrientation(float angle)
        {
            ScreenOrientation orientation = ScreenOrientation.Portrait;
            if (angle > 315 || angle <= 45)
            {
                orientation = ScreenOrientation.Portrait;
            }
            else if (angle > 45 && angle <= 135)
            {
                orientation = ScreenOrientation.LandscapeRight;
            }
            else if (angle > 135 && angle <= 225)
            {
                orientation = ScreenOrientation.PortraitUpsideDown;
            }
            else if (angle > 225 && angle <= 315)
            {
                orientation = ScreenOrientation.LandscapeLeft;
            }
            return orientation;
        }

        public static bool IsLandscape(ScreenOrientation orientation)
        {
            if (orientation == ScreenOrientation.Landscape || orientation == ScreenOrientation.LandscapeLeft ||
                orientation == ScreenOrientation.LandscapeRight)
                return true;

            return false;
        }

        public static void CheckShimmedAssemblies(List<string> shimmedAssemblies)
        {
            if (shimmedAssemblies == null || shimmedAssemblies.Count == 0)
                return;

            shimmedAssemblies.RemoveAll(string.IsNullOrEmpty);

            const string dll = ".dll";
            for (int i = 0; i < shimmedAssemblies.Count; i++)
            {
                shimmedAssemblies[i] = shimmedAssemblies[i].ToLower();
                if (!shimmedAssemblies[i].EndsWith(dll))
                {
                    shimmedAssemblies[i] += dll;
                }
            }
        }

        public static bool ShouldShim(List<string> shimmedAssemblies)
        {
            if (shimmedAssemblies == null || shimmedAssemblies.Count == 0)
                return false;

            // Here we use StackTrace to trace where the call comes from, only shim if it comes from the white listed assemblies.
            // 4 in StackTrace stands for the frames that we want to trace back up from here, as below:
            // SimulatorUtilities.ShouldShim() <-- SystemInfoSimulation/ApplicationSimulation.ShouldShim() <-- ApplicationSimulation <-- Application <-- Where the APIs are called.
            var callingAssembly = new StackTrace(4).GetFrame(0).GetMethod().Module.ToString().ToLower();
            foreach (var assembly in shimmedAssemblies)
            {
                if (callingAssembly == assembly)
                    return true;
            }
            return false;
        }
    }
}
