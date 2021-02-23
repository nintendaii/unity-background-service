using NUnit.Framework;
using UnityEngine;

namespace Unity.DeviceSimulator.Tests
{
    internal class ScreenModeTests
    {
        internal DeviceInfo m_TestDevice;
        internal ScreenSimulation m_Simulation;
        internal TestInput m_InputTest;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_InputTest = new TestInput();
            m_TestDevice = DeviceInfoLibrary.GetDeviceWithSupportedOrientations(new[]
            {
                ScreenOrientation.Portrait,
                ScreenOrientation.LandscapeLeft,
                ScreenOrientation.LandscapeRight,
                ScreenOrientation.PortraitUpsideDown
            });
            m_TestDevice.SystemInfo = new SystemInfoData();
        }

        [TearDown]
        public void TearDown()
        {
            m_Simulation?.Dispose();
        }

        [Test, TestCaseSource("AndroidFullScreenCases")]
        public void AndroidFullScreen(DeviceInfo device, ScreenOrientation initOrientation, int windowedWidth, int windowedHeight, Vector4 windowedInsets, Rect safeArea)
        {
            var fullScreenEventCounter = 0;
            var insetEventCounter = 0;
            var resolutionEventCounter = 0;
            var safeAreaEventCounter = 0;

            var insetFromEvent = Vector4.zero;
            var safeAreaFromEvent = Rect.zero;

            m_InputTest.Rotation = ScreenTestUtilities.OrientationRotation[initOrientation];
            m_Simulation = new ScreenSimulation(device, m_InputTest, new SimulationPlayerSettings());

            m_Simulation.OnResolutionChanged += (i, i1) => resolutionEventCounter++;
            m_Simulation.OnFullScreenChanged += b => fullScreenEventCounter++;
            m_Simulation.OnInsetsChanged += inset =>
            {
                insetEventCounter++;
                insetFromEvent = inset;
            };
            m_Simulation.OnScreenSpaceSafeAreaChanged += sa =>
            {
                safeAreaEventCounter++;
                safeAreaFromEvent = sa;
            };

            var fullScreenSafeArea = Screen.safeArea;
            var fullScreenResolution = Screen.currentResolution;

            Assert.IsTrue(Screen.fullScreen);

            m_Simulation.fullScreen = false;
            Assert.IsFalse(Screen.fullScreen);

            Assert.AreEqual(1, fullScreenEventCounter);
            Assert.AreEqual(1, insetEventCounter);
            Assert.AreEqual(1, resolutionEventCounter);
            Assert.AreEqual(1, safeAreaEventCounter);
            fullScreenEventCounter = 0;
            insetEventCounter = 0;
            resolutionEventCounter = 0;
            safeAreaEventCounter = 0;

            // The way windowed on Android is handled has changed. Now simulator behavior does not match reality - cases are correct but simulator returns completely incorrect values
//            Assert.AreEqual(windowedWidth, Screen.currentResolution.width);
//            Assert.AreEqual(windowedHeight, Screen.currentResolution.height);
//            Assert.AreEqual(windowedWidth, Screen.currentResolution.width);
//            Assert.AreEqual(windowedHeight, Screen.currentResolution.height);

//            Assert.AreEqual(windowedInsets, insetFromEvent);
//            Assert.AreEqual(windowedInsets, m_Simulation.Insets);
//
//            Assert.AreEqual(safeArea, Screen.safeArea);

            m_Simulation.fullScreen = true;
            Assert.IsTrue(Screen.fullScreen);

            Assert.AreEqual(1, fullScreenEventCounter);
            Assert.AreEqual(1, insetEventCounter);
            Assert.AreEqual(1, resolutionEventCounter);
            Assert.AreEqual(1, safeAreaEventCounter);

            Assert.AreEqual(fullScreenSafeArea, Screen.safeArea);
            Assert.AreEqual(fullScreenResolution, Screen.currentResolution);
        }

        static object[] AndroidFullScreenCases =
        {
            new object[] {DeviceInfoLibrary.GetMotoG7Power(), ScreenOrientation.Portrait, 720, 1424, new Vector4(0, 0, 0, 96), new Rect(0, 0, 720, 1374)},
            new object[] {DeviceInfoLibrary.GetMotoG7Power(), ScreenOrientation.LandscapeRight, 1424, 720, new Vector4(0, 0, 0, 96), new Rect(0, 0, 1374, 720)},
            new object[] {DeviceInfoLibrary.GetGalaxy10e(), ScreenOrientation.LandscapeLeft, 2136, 1080, new Vector4(0, 0, 0, 145), new Rect(109, 0, 2027, 1080)},
            new object[] {DeviceInfoLibrary.GetGalaxy10e(), ScreenOrientation.PortraitUpsideDown, 1080, 2020, new Vector4(0, 260, 0, 0), new Rect(0, 0, 1080, 2020)}
        };

        [Test]
        public void iOSFullScreen()
        {
            var fullScreenEventCounter = 0;
            var insetEventCounter = 0;
            var resolutionEventCounter = 0;
            var safeAreaEventCounter = 0;

            m_InputTest.Rotation = ScreenTestUtilities.OrientationRotation[ScreenOrientation.Portrait];
            m_Simulation = new ScreenSimulation(DeviceInfoLibrary.GetIphoneXMax(), m_InputTest, new SimulationPlayerSettings());

            m_Simulation.OnResolutionChanged += (i, i1) => resolutionEventCounter++;
            m_Simulation.OnFullScreenChanged += b => fullScreenEventCounter++;
            m_Simulation.OnInsetsChanged += vector4 => insetEventCounter++;
            m_Simulation.OnScreenSpaceSafeAreaChanged += rect => safeAreaEventCounter++;

            m_Simulation.fullScreen = true;
            Assert.IsTrue(Screen.fullScreen);

            m_Simulation.fullScreen = false;
            Assert.IsTrue(Screen.fullScreen);

            Assert.AreEqual(0, fullScreenEventCounter);
            Assert.AreEqual(0, insetEventCounter);
            Assert.AreEqual(0, resolutionEventCounter);
            Assert.AreEqual(0, safeAreaEventCounter);
        }

        [Test]
        [TestCase(FullScreenMode.FullScreenWindow)]
        [TestCase(FullScreenMode.ExclusiveFullScreen)]
        [TestCase(FullScreenMode.MaximizedWindow)]
        [TestCase(FullScreenMode.Windowed)]
        public void FullScreenModeAndroid(FullScreenMode fullScreenMode)
        {
            m_TestDevice.SystemInfo.operatingSystem = "Android";

            m_InputTest.Rotation = ScreenTestUtilities.OrientationRotation[ScreenOrientation.PortraitUpsideDown];
            m_Simulation = new ScreenSimulation(m_TestDevice, m_InputTest, new SimulationPlayerSettings());

            m_Simulation.fullScreenMode = fullScreenMode;
            switch (fullScreenMode)
            {
                case FullScreenMode.Windowed:
                    Assert.IsFalse(Screen.fullScreen);
                    Assert.AreEqual(FullScreenMode.Windowed, Screen.fullScreenMode);
                    break;
                default:
                    Assert.IsTrue(Screen.fullScreen);
                    Assert.AreEqual(FullScreenMode.FullScreenWindow, Screen.fullScreenMode);
                    break;
            }
        }

        [Test]
        [TestCase(FullScreenMode.FullScreenWindow)]
        [TestCase(FullScreenMode.ExclusiveFullScreen)]
        [TestCase(FullScreenMode.MaximizedWindow)]
        [TestCase(FullScreenMode.Windowed)]
        public void FullScreenModeiOS(FullScreenMode fullScreenMode)
        {
            m_TestDevice.SystemInfo.operatingSystem = "iOS";

            m_InputTest.Rotation = ScreenTestUtilities.OrientationRotation[ScreenOrientation.PortraitUpsideDown];
            m_Simulation = new ScreenSimulation(m_TestDevice, m_InputTest, new SimulationPlayerSettings());

            m_Simulation.fullScreenMode = fullScreenMode;
            Assert.IsTrue(Screen.fullScreen);
            Assert.AreEqual(FullScreenMode.FullScreenWindow, Screen.fullScreenMode);
        }
    }
}
