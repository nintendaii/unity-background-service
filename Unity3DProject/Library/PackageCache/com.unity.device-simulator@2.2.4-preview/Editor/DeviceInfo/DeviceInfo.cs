using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.DeviceSimulator
{
    [Serializable]
    internal class DeviceInfo
    {
        public string friendlyName;
        public int version;

        public ScreenData[] Screens;
        public SystemInfoData SystemInfo;

        public override string ToString()
        {
            return friendlyName;
        }

        [NonSerialized]
        public string Directory;

        public bool IsAndroidDevice()
        {
            return IsGivenDevice("android");
        }

        public bool IsiOSDevice()
        {
            return IsGivenDevice("ios");
        }

        public bool IsMobileDevice()
        {
            return IsAndroidDevice() || IsiOSDevice();
        }

        public bool IsConsoleDevice()
        {
            return false; // Return false for now, should revisit when adding console devices.
        }

        public bool IsGivenDevice(string os)
        {
            return (this.SystemInfo != null) ? this.SystemInfo.operatingSystem.ToLower().Contains(os) : false;
        }

        public bool LoadOverlayImage()
        {
            var screen = this.Screens[0];
            if (screen.presentation.overlay != null)
                return true;

            if (string.IsNullOrEmpty(screen.presentation.overlayPath))
                return false;

            var filePath = Path.Combine(this.Directory, screen.presentation.overlayPath);
            if (!File.Exists(filePath))
                return false;

            var overlayBytes = File.ReadAllBytes(filePath);
            var texture = new Texture2D(2, 2, TextureFormat.Alpha8, false)
            {
                alphaIsTransparency = true
            };

            if (!texture.LoadImage(overlayBytes, false))
                return false;

            screen.presentation.overlay = texture;
            return true;
        }

        public void AddOptionalFields()
        {
            foreach (var screen in Screens)
            {
                if (screen.orientations == null || screen.orientations.Length == 0)
                {
                    screen.orientations = new[]
                    {
                        new OrientationData {orientation = ScreenOrientation.Portrait},
                        new OrientationData {orientation = ScreenOrientation.PortraitUpsideDown},
                        new OrientationData {orientation = ScreenOrientation.LandscapeLeft},
                        new OrientationData {orientation = ScreenOrientation.LandscapeRight}
                    };
                }
                foreach (var orientation in screen.orientations)
                {
                    if (orientation.safeArea == Rect.zero)
                        orientation.safeArea = SimulatorUtilities.IsLandscape(orientation.orientation) ? new Rect(0, 0, screen.height, screen.width) : new Rect(0, 0, screen.width, screen.height);
                }
            }
        }
    }

    [Serializable]
    internal class ScreenPresentation
    {
        public string overlayPath;
        public Vector4 borderSize;
        public float cornerRadius;
        [NonSerialized] public Texture overlay;
    }

    [Serializable]
    internal class ScreenData
    {
        public int width;
        public int height;
        public int navigationBarHeight;
        public float dpi;
        public OrientationData[] orientations;
        public ScreenPresentation presentation;
    }

    [Serializable]
    internal class OrientationData
    {
        public ScreenOrientation orientation;
        public Rect safeArea;
        public Rect[] cutouts;
    }

    [Serializable]
    internal class SystemInfoData
    {
        public string deviceModel;
        public DeviceType deviceType;
        public string operatingSystem;
        public OperatingSystemFamily operatingSystemFamily;
        public int processorCount;
        public int processorFrequency;
        public string processorType;
        public bool supportsAccelerometer;
        public bool supportsAudio;
        public bool supportsGyroscope;
        public bool supportsLocationService;
        public bool supportsVibration;
        public int systemMemorySize;
        public string unsupportedIdentifier;
        public GraphicsSystemInfoData[] graphicsDependentData;
    }

    [Serializable]
    internal class GraphicsSystemInfoData
    {
        public GraphicsDeviceType graphicsDeviceType;
        public int graphicsMemorySize;
        public string graphicsDeviceName;
        public string graphicsDeviceVendor;
        public int graphicsDeviceID;
        public int graphicsDeviceVendorID;
        public bool graphicsUVStartsAtTop;
        public string graphicsDeviceVersion;
        public int graphicsShaderLevel;
        public bool graphicsMultiThreaded;
        public RenderingThreadingMode renderingThreadingMode;
        public bool hasHiddenSurfaceRemovalOnGPU;
        public bool hasDynamicUniformArrayIndexingInFragmentShaders;
        public bool supportsShadows;
        public bool supportsRawShadowDepthSampling;
        public bool supportsMotionVectors;
        public bool supports3DTextures;
        public bool supports2DArrayTextures;
        public bool supports3DRenderTextures;
        public bool supportsCubemapArrayTextures;
        public CopyTextureSupport copyTextureSupport;
        public bool supportsComputeShaders;
        public bool supportsGeometryShaders;
        public bool supportsTessellationShaders;
        public bool supportsInstancing;
        public bool supportsHardwareQuadTopology;
        public bool supports32bitsIndexBuffer;
        public bool supportsSparseTextures;
        public int supportedRenderTargetCount;
        public bool supportsSeparatedRenderTargetsBlend;
        public int supportedRandomWriteTargetCount;
        public int supportsMultisampledTextures;
        public bool supportsMultisampleAutoResolve;
        public int supportsTextureWrapMirrorOnce;
        public bool usesReversedZBuffer;
        public NPOTSupport npotSupport;
        public int maxTextureSize;
        public int maxCubemapSize;
        public int maxComputeBufferInputsVertex;
        public int maxComputeBufferInputsFragment;
        public int maxComputeBufferInputsGeometry;
        public int maxComputeBufferInputsDomain;
        public int maxComputeBufferInputsHull;
        public int maxComputeBufferInputsCompute;
        public int maxComputeWorkGroupSize;
        public int maxComputeWorkGroupSizeX;
        public int maxComputeWorkGroupSizeY;
        public int maxComputeWorkGroupSizeZ;
        public bool supportsAsyncCompute;
        public bool supportsGraphicsFence;
        public bool supportsAsyncGPUReadback;
        public bool supportsRayTracing;
        public bool supportsSetConstantBuffer;
        public bool minConstantBufferOffsetAlignment;
        public bool hasMipMaxLevel;
        public bool supportsMipStreaming;
        public bool usesLoadStoreActions;
    }
}
