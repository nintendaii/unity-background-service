using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Unity.DeviceSimulator
{
    internal class SystemInfoSimulation : SystemInfoShimBase, ISimulatorEvents
    {
        private readonly DeviceInfo m_DeviceInfo;
        private readonly GraphicsSystemInfoData m_GraphicsDeviceType;
        private readonly List<string> m_ShimmedAssemblies;

        public GraphicsSystemInfoData GraphicsDependentData => m_GraphicsDeviceType;

        public SystemInfoSimulation(DeviceInfo deviceInfo, SimulationPlayerSettings playerSettings, List<string> shimmedAssemblies)
        {
            m_ShimmedAssemblies = shimmedAssemblies;

            m_DeviceInfo = deviceInfo;
            if (m_DeviceInfo?.SystemInfo?.graphicsDependentData?.Length > 0)
            {
                if (deviceInfo.IsAndroidDevice())
                {
                    m_GraphicsDeviceType = (
                        from selected in playerSettings.androidGraphicsAPIs
                        from device in m_DeviceInfo.SystemInfo.graphicsDependentData
                        where selected == device.graphicsDeviceType select device).FirstOrDefault();
                }
                else if (deviceInfo.IsiOSDevice())
                {
                    m_GraphicsDeviceType = (
                        from selected in playerSettings.iOSGraphicsAPIs
                        from device in m_DeviceInfo.SystemInfo.graphicsDependentData
                        where selected == device.graphicsDeviceType select device).FirstOrDefault();
                }
                if (m_GraphicsDeviceType == null)
                {
                    Debug.LogWarning("Could not pick GraphicsDeviceType, the game would fail to launch");
                }
            }
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

        public void Dispose()
        {
            Disable();
        }

        private bool ShouldShim()
        {
            return SimulatorUtilities.ShouldShim(m_ShimmedAssemblies);
        }

        #region General SystemInfo Overrides

        public override string unsupportedIdentifier => ShouldShim() ? m_DeviceInfo.SystemInfo.unsupportedIdentifier : base.unsupportedIdentifier;
//        public override float batteryLevel => ShouldShim() ? m_DeviceInfo.SystemInfo.batteryLevel : base.batteryLevel;
//        public override BatteryStatus batteryStatus => ShouldShim() ? m_DeviceInfo.SystemInfo.batteryStatus : base.batteryStatus;
        public override string operatingSystem => ShouldShim() ? m_DeviceInfo.SystemInfo.operatingSystem : base.operatingSystem;
        public override OperatingSystemFamily operatingSystemFamily => ShouldShim() ? m_DeviceInfo.SystemInfo.operatingSystemFamily : base.operatingSystemFamily;
        public override string processorType => ShouldShim() ? m_DeviceInfo.SystemInfo.processorType : base.processorType;
        public override int processorFrequency => ShouldShim() ? m_DeviceInfo.SystemInfo.processorFrequency : base.processorFrequency;
        public override int processorCount => ShouldShim() ? m_DeviceInfo.SystemInfo.processorCount : base.processorCount;
        public override int systemMemorySize => ShouldShim() ? m_DeviceInfo.SystemInfo.systemMemorySize : base.systemMemorySize;
//        public override string deviceUniqueIdentifier => ShouldShim() ? m_DeviceInfo.SystemInfo.deviceUniqueIdentifier : base.deviceUniqueIdentifier;
//        public override string deviceName => ShouldShim() ? m_DeviceInfo.SystemInfo.deviceName : base.deviceName;
        public override string deviceModel => ShouldShim() ? m_DeviceInfo.SystemInfo.deviceModel : base.deviceModel;
        public override bool supportsAccelerometer => ShouldShim() ? m_DeviceInfo.SystemInfo.supportsAccelerometer : base.supportsAccelerometer;
        public override bool supportsGyroscope => ShouldShim() ? m_DeviceInfo.SystemInfo.supportsGyroscope : base.supportsGyroscope;
        public override bool supportsLocationService => ShouldShim() ? m_DeviceInfo.SystemInfo.supportsLocationService : base.supportsLocationService;
        public override bool supportsVibration => ShouldShim() ? m_DeviceInfo.SystemInfo.supportsVibration : base.supportsVibration;
        public override bool supportsAudio => ShouldShim() ? m_DeviceInfo.SystemInfo.supportsAudio : base.supportsAudio;
        public override DeviceType deviceType => ShouldShim() ? m_DeviceInfo.SystemInfo.deviceType : base.deviceType;

        #endregion

        #region Graphics Backend Dependent SystemInfo Overrides

        public override GraphicsDeviceType graphicsDeviceType => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsDeviceType : base.graphicsDeviceType;
        public override int graphicsMemorySize  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsMemorySize : base.graphicsMemorySize;
        public override string graphicsDeviceName  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsDeviceName : base.graphicsDeviceName;
        public override string graphicsDeviceVendor  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsDeviceVendor : base.graphicsDeviceVendor;
        public override int graphicsDeviceID  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsDeviceID : base.graphicsDeviceID;
        public override int graphicsDeviceVendorID  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsDeviceVendorID : base.graphicsDeviceVendorID;
        public override bool graphicsUVStartsAtTop  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsUVStartsAtTop : base.graphicsUVStartsAtTop;
        public override string graphicsDeviceVersion  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsDeviceVersion : base.graphicsDeviceVersion;
        public override int graphicsShaderLevel  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsShaderLevel : base.graphicsShaderLevel;
        public override bool graphicsMultiThreaded  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.graphicsMultiThreaded : base.graphicsMultiThreaded;
        public override RenderingThreadingMode renderingThreadingMode  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.renderingThreadingMode : base.renderingThreadingMode;
        public override bool hasHiddenSurfaceRemovalOnGPU  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.hasHiddenSurfaceRemovalOnGPU : base.hasHiddenSurfaceRemovalOnGPU;
        public override bool hasDynamicUniformArrayIndexingInFragmentShaders  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.hasDynamicUniformArrayIndexingInFragmentShaders : base.hasDynamicUniformArrayIndexingInFragmentShaders;
        public override bool supportsShadows  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsShadows : base.supportsShadows;
        public override bool supportsRawShadowDepthSampling  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsRawShadowDepthSampling : base.supportsRawShadowDepthSampling;
        public override bool supportsMotionVectors  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsMotionVectors : base.supportsMotionVectors;
        public override bool supports3DTextures  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supports3DTextures : base.supports3DTextures;
        public override bool supports2DArrayTextures  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supports2DArrayTextures : base.supports2DArrayTextures;
        public override bool supports3DRenderTextures  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supports3DRenderTextures : base.supports3DRenderTextures;
        public override bool supportsCubemapArrayTextures  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsCubemapArrayTextures : base.supportsCubemapArrayTextures;
        public override CopyTextureSupport copyTextureSupport  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.copyTextureSupport : base.copyTextureSupport;
        public override bool supportsComputeShaders  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsComputeShaders : base.supportsComputeShaders;
        public override bool supportsGeometryShaders  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsGeometryShaders : base.supportsGeometryShaders;
        public override bool supportsTessellationShaders  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsTessellationShaders : base.supportsTessellationShaders;
        public override bool supportsInstancing  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsInstancing : base.supportsInstancing;
        public override bool supportsHardwareQuadTopology  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsHardwareQuadTopology : base.supportsHardwareQuadTopology;
        public override bool supports32bitsIndexBuffer  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supports32bitsIndexBuffer : base.supports32bitsIndexBuffer;
        public override bool supportsSparseTextures  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsSparseTextures : base.supportsSparseTextures;
        public override int supportedRenderTargetCount  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportedRenderTargetCount : base.supportedRenderTargetCount;
        public override bool supportsSeparatedRenderTargetsBlend  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsSeparatedRenderTargetsBlend : base.supportsSeparatedRenderTargetsBlend;
        public override int supportedRandomWriteTargetCount  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportedRandomWriteTargetCount : base.supportedRandomWriteTargetCount;
        public override int supportsMultisampledTextures  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsMultisampledTextures : base.supportsMultisampledTextures;
        public override bool supportsMultisampleAutoResolve  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsMultisampleAutoResolve : base.supportsMultisampleAutoResolve;
        public override int supportsTextureWrapMirrorOnce  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsTextureWrapMirrorOnce : base.supportsTextureWrapMirrorOnce;
        public override bool usesReversedZBuffer  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.usesReversedZBuffer : base.usesReversedZBuffer;
        public override NPOTSupport npotSupport  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.npotSupport : base.npotSupport;
        public override int maxTextureSize  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxTextureSize : base.maxTextureSize;
        public override int maxCubemapSize  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxCubemapSize : base.maxCubemapSize;
        public override int maxComputeBufferInputsVertex  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeBufferInputsVertex : base.maxComputeBufferInputsVertex;
        public override int maxComputeBufferInputsFragment  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeBufferInputsFragment : base.maxComputeBufferInputsFragment;
        public override int maxComputeBufferInputsGeometry  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeBufferInputsGeometry : base.maxComputeBufferInputsGeometry;
        public override int maxComputeBufferInputsDomain  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeBufferInputsDomain : base.maxComputeBufferInputsDomain;
        public override int maxComputeBufferInputsHull  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeBufferInputsHull : base.maxComputeBufferInputsHull;
        public override int maxComputeBufferInputsCompute  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeBufferInputsCompute : base.maxComputeBufferInputsCompute;
        public override int maxComputeWorkGroupSize  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeWorkGroupSize : base.maxComputeWorkGroupSize;
        public override int maxComputeWorkGroupSizeX  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeWorkGroupSizeX : base.maxComputeWorkGroupSizeX;
        public override int maxComputeWorkGroupSizeY  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeWorkGroupSizeY : base.maxComputeWorkGroupSizeY;
        public override int maxComputeWorkGroupSizeZ  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.maxComputeWorkGroupSizeZ : base.maxComputeWorkGroupSizeZ;
        public override bool supportsAsyncCompute  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsAsyncCompute : base.supportsAsyncCompute;
        public override bool supportsGraphicsFence  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsGraphicsFence : base.supportsGraphicsFence;
        public override bool supportsAsyncGPUReadback  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsAsyncGPUReadback : base.supportsAsyncGPUReadback;
        public override bool supportsRayTracing  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsRayTracing : base.supportsRayTracing;
        public override bool supportsSetConstantBuffer  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsSetConstantBuffer : base.supportsSetConstantBuffer;
        public override bool hasMipMaxLevel  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.hasMipMaxLevel : base.hasMipMaxLevel;
        public override bool supportsMipStreaming  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.supportsMipStreaming : base.supportsMipStreaming;
        public override bool usesLoadStoreActions  => m_GraphicsDeviceType != null && ShouldShim() ? m_GraphicsDeviceType.usesLoadStoreActions : base.usesLoadStoreActions;

        public override bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            return base.SupportsRenderTextureFormat(format);
        }

        public override bool SupportsBlendingOnRenderTextureFormat(RenderTextureFormat format)
        {
            return base.SupportsBlendingOnRenderTextureFormat(format);
        }

        public override bool SupportsTextureFormat(TextureFormat format)
        {
            return base.SupportsTextureFormat(format);
        }

        public override bool SupportsVertexAttributeFormat(VertexAttributeFormat format, int dimension)
        {
            return base.SupportsVertexAttributeFormat(format, dimension);
        }

        public override bool IsFormatSupported(GraphicsFormat format, FormatUsage usage)
        {
            return base.IsFormatSupported(format, usage);
        }

        public override GraphicsFormat GetCompatibleFormat(GraphicsFormat format, FormatUsage usage)
        {
            return base.GetCompatibleFormat(format, usage);
        }

        public override GraphicsFormat GetGraphicsFormat(DefaultFormat format)
        {
            return base.GetGraphicsFormat(format);
        }

        #endregion
    }
}
