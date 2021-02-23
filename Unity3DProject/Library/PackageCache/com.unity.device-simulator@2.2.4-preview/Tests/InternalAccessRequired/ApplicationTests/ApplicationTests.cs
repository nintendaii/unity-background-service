using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Unity.DeviceSimulator.Tests
{
    internal class ApplicationTests
    {
        private DeviceInfo m_Device;
        private ApplicationSimulation m_ApplicationSimulation;
        private List<string> m_WhitelistedAssemblies = new List<string>() { "Assembly-CSharp-Editor-firstpass-testable.dll".ToLower() };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Device = new DeviceInfo()
            {
                SystemInfo = new SystemInfoData()
            };
        }

        [SetUp]
        public void SetUp()
        {
            m_ApplicationSimulation = new ApplicationSimulation(m_Device, m_WhitelistedAssemblies);
        }

        [TearDown]
        public void TearDown()
        {
            m_ApplicationSimulation?.Dispose();
        }

        [Test]
        [TestCase(RuntimePlatform.Android)]
        [TestCase(RuntimePlatform.IPhonePlayer)]
        public void TestPlatform(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    m_Device.SystemInfo.operatingSystem = "Android";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    m_Device.SystemInfo.operatingSystem = "iOS";
                    break;
            }

            Assert.AreEqual(platform, Application.platform);
            Assert.AreEqual(true, Application.isMobilePlatform);
            Assert.AreEqual(false, Application.isConsolePlatform);
            Assert.AreEqual(false, Application.isEditor);
        }

        [Test]
        [TestCase(NetworkReachability.NotReachable)]
        [TestCase(NetworkReachability.ReachableViaCarrierDataNetwork)]
        [TestCase(NetworkReachability.ReachableViaLocalAreaNetwork)]
        public void TestInternetReachability(NetworkReachability internetReachability)
        {
            m_ApplicationSimulation.ShimmedInternetReachability = internetReachability;

            Assert.AreEqual(internetReachability, Application.internetReachability);
        }

        [Test]
        public void TestSystemLanguage()
        {
            var languages = Enum.GetValues(typeof(SystemLanguage));
            foreach (var language in languages)
            {
                m_ApplicationSimulation.ShimmedSystemLanguage = (SystemLanguage)language;
                Assert.AreEqual(language, Application.systemLanguage);
            }
        }

        [Test]
        public void TestOnLowMemory()
        {
            var onLowMemoryTest = new OnLowMemoryTest();

            Assert.AreEqual(0, onLowMemoryTest.Counter);
            m_ApplicationSimulation.OnLowMemory();
            Assert.AreEqual(2, onLowMemoryTest.Counter);
        }
    }
}
