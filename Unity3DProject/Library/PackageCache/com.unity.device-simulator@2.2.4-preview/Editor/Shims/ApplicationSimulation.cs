using System.Collections.Generic;
using UnityEngine;

namespace Unity.DeviceSimulator
{
    internal class ApplicationSimulation : ApplicationShimBase, ISimulatorEvents
    {
        private readonly DeviceInfo m_DeviceInfo;
        private readonly List<string> m_ShimmedAssemblies;

        public SystemLanguage ShimmedSystemLanguage { get; set; }

        public NetworkReachability ShimmedInternetReachability { get; set; }

        public ApplicationSimulation(DeviceInfo deviceInfo, List<string> shimmedAssemblies)
        {
            m_DeviceInfo = deviceInfo;
            m_ShimmedAssemblies = shimmedAssemblies;

            ShimmedSystemLanguage = SystemLanguage.English;
            ShimmedInternetReachability = NetworkReachability.ReachableViaLocalAreaNetwork;

            Enable();
        }

        public void Enable()
        {
            ShimManager.UseShim(this);
        }

        public void Disable()
        {
            ShimManager.RemoveShim(this);
        }

        public new void Dispose()
        {
            Disable();
        }

        private bool ShouldShim()
        {
            return SimulatorUtilities.ShouldShim(m_ShimmedAssemblies);
        }

        #region ApplicationShimBase Overrides

        public override bool isEditor => ShouldShim() ? false : base.isEditor;

        public override RuntimePlatform platform
        {
            get
            {
                if (m_DeviceInfo != null && ShouldShim())
                {
                    if (m_DeviceInfo.IsAndroidDevice())
                        return RuntimePlatform.Android;

                    if (m_DeviceInfo.IsiOSDevice())
                        return RuntimePlatform.IPhonePlayer;
                }

                return base.platform;
            }
        }

        public override bool isMobilePlatform => ShouldShim() ? m_DeviceInfo.IsMobileDevice() : base.isMobilePlatform;

        public override bool isConsolePlatform => ShouldShim() ? m_DeviceInfo.IsConsoleDevice() : base.isConsolePlatform;

        public override SystemLanguage systemLanguage => ShouldShim() ? ShimmedSystemLanguage : base.systemLanguage;

        public override NetworkReachability internetReachability => ShouldShim() ? ShimmedInternetReachability : base.internetReachability;

        #endregion
    }
}
