using NUnit.Framework;

namespace Unity.DeviceSimulator.Tests
{
    internal class DeviceInfoValidation
    {
        [Test]
        public void MinimalDevice()
        {
            var deviceJson = @"
{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""Screens"": [
    {
        ""width"": 1080,
        ""height"": 1920,
        ""dpi"": 450.0
    }
    ],
    ""SystemInfo"": {
        ""operatingSystem"": ""Android""
    }
}
            ";
            Assert.IsTrue(DeviceDatabase.DeviceInfoParse(deviceJson, out var parseErrors, out var deviceInfo));
            Assert.IsTrue(string.IsNullOrEmpty(parseErrors));
            Assert.NotNull(deviceInfo);

            deviceInfo.AddOptionalFields();
            Assert.IsTrue(deviceInfo.Screens[0].orientations.Length == 4);
        }

        [Test]
        public void NotJson()
        {
            var deviceJson = @"{";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void NoFriendlyName()
        {
            var deviceJson = @"{
    ""version"": 1,
    ""Screens"": [
    {
        ""width"": 1080,
        ""height"": 1920,
        ""dpi"": 450.0
    }
    ],
    ""SystemInfo"": {
        ""operatingSystem"": ""Android""
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void NoVersion()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""Screens"": [
    {
        ""width"": 1080,
        ""height"": 1920,
        ""dpi"": 450.0
    }
    ],
    ""SystemInfo"": {
        ""operatingSystem"": ""Android""
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void NoSystemInfo()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""Screens"": [
    {
        ""width"": 1080,
        ""height"": 1920,
        ""dpi"": 450.0
    }
    ]
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void OperatingSystemEmpty()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""Screens"": [
    {
        ""width"": 1080,
        ""height"": 1920,
        ""dpi"": 450.0
    }
    ],
    ""SystemInfo"": {
        ""operatingSystem"": """"
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void NoScreens()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""SystemInfo"": {
        ""operatingSystem"": ""Android""
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void EmptyScreens()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""Screens"": [
    ],
    ""SystemInfo"": {
        ""operatingSystem"": ""Android""
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void NoWidth()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""Screens"": [
    {
        ""height"": 1920,
        ""dpi"": 450.0
    }
    ],
    ""SystemInfo"": {
        ""operatingSystem"": ""Android""
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void NoHeight()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""Screens"": [
    {
        ""width"": 1080,
        ""dpi"": 450.0
    }
    ],
    ""SystemInfo"": {
        ""operatingSystem"": ""Android""
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void NoDpi()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""Screens"": [
    {
        ""width"": 1080,
        ""height"": 1920,
    }
    ],
    ""SystemInfo"": {
        ""operatingSystem"": ""Android""
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        [Test]
        public void NoOperatingSystem()
        {
            var deviceJson = @"{
    ""friendlyName"": ""Minimal Device"",
    ""version"": 1,
    ""Screens"": [
    {
        ""width"": 1080,
        ""height"": 1920,
        ""dpi"": 450.0
    }
    ],
    ""SystemInfo"": {
    }
}";
            MakeSureParsingFailed(deviceJson);
        }

        private static void MakeSureParsingFailed(string deviceJson)
        {
            Assert.IsFalse(DeviceDatabase.DeviceInfoParse(deviceJson, out var parseErrors, out var deviceInfo));
            Assert.IsFalse(string.IsNullOrEmpty(parseErrors));
            Assert.Null(deviceInfo);
        }
    }
}
