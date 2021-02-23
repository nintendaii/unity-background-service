using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    internal class SimulatorPreviewPanel
    {
        private VisualElement m_RootElement;
        private InputProvider m_InputProvider;
        private DeviceInfo m_DeviceInfo;
        private ScreenSimulation m_ScreenSimulation;

        public RenderTexture PreviewTexture
        {
            set => m_PreviewRenderer.PreviewTexture = value;
        }

        public Texture OverlayTexture
        {
            set => m_PreviewRenderer.OverlayTexture = value;
        }

        private int m_RotationDegree = 0; // Value from [0, 360), counted as CCW(convert to CW in the future?).
        private int Rotation
        {
            get => m_RotationDegree;
            set
            {
                m_RotationDegree = value;
                m_PreviewRenderer.Rotation = Quaternion.Euler(0, 0, 360 - m_RotationDegree);
            }
        }

        private int m_Scale = 20; // Value from (0, 100].
        private int Scale
        {
            get => m_Scale;
            set
            {
                m_Scale = value;
                m_PreviewRenderer.Scale = m_Scale / 100f;
            }
        }

        private bool m_HighlightSafeArea;
        private bool HighlightSafeArea
        {
            get => m_HighlightSafeArea;
            set
            {
                m_HighlightSafeArea = value;
                m_PreviewRenderer.ShowSafeArea = m_HighlightSafeArea;
            }
        }

        public Action<bool> OnControlPanelHiddenChanged { get; set; }

        private bool m_ControlPanelHidden;
        private const int kScaleMin = 10;
        private const int kScaleMax = 100;
        private bool m_FitToScreenEnabled = true;

        // Controls for preview toolbar.
        private ToolbarButton m_HideControlPanel;
        private SliderInt m_ScaleSlider;
        private Label m_ScaleValueLabel;
        private ToolbarToggle m_FitToScreenToggle;
        private ToolbarToggle m_HighlightSafeAreaToggle;

        // Controls for inactive message.
        private VisualElement m_InactiveMsgContainer;

        // Controls for preview.
        private VisualElement m_ScrollViewContainer;
        private ScrollView m_ScrollView;
        private VisualElement m_PreviewRendererContainer;
        private DeviceView m_PreviewRenderer;

        private TouchEventManipulator m_TouchEventManipulator;

        public SimulatorPreviewPanel(VisualElement rootElement, InputProvider inputProvider)
        {
            m_RootElement = rootElement;
            m_InputProvider = inputProvider;
            InitPreviewToolbar();
            InitInactiveMsg();
            InitPreview();
        }

        public void StoreSerializationStates(ref SimulatorSerializationStates states)
        {
            states.scale = Scale;
            states.fitToScreenEnabled = m_FitToScreenEnabled;
            states.rotationDegree = Rotation;
            states.highlightSafeAreaEnabled = m_HighlightSafeArea;
        }

        public void ApplySerializationStates(SimulatorSerializationStates states)
        {
            if (states != null)
            {
                m_ControlPanelHidden = states.controlPanelHidden;
                Scale = states.scale;
                m_FitToScreenEnabled = states.fitToScreenEnabled;
                Rotation = states.rotationDegree;
                HighlightSafeArea = states.highlightSafeAreaEnabled;
                m_HideControlPanel.style.backgroundImage = (Texture2D)EditorGUIUtility.Load($"Icons/d_tab_{(m_ControlPanelHidden ? "next" : "prev")}@2x.png");
                m_ScaleSlider.SetValueWithoutNotify(Scale);
                m_ScaleValueLabel.text = Scale.ToString();
                m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);
                m_HighlightSafeAreaToggle.SetValueWithoutNotify(HighlightSafeArea);
                UpdateScrollbars();
            }
        }

        private void InitPreviewToolbar()
        {
            m_HideControlPanel = m_RootElement.Q<ToolbarButton>("hide-control-panel");
            m_HideControlPanel.style.backgroundImage = (Texture2D)EditorGUIUtility.Load($"Icons/d_tab_{(m_ControlPanelHidden ? "next" : "prev")}@2x.png");
            m_HideControlPanel.clickable = new Clickable(HideControlPanel);

            #region Scale
            m_ScaleSlider = m_RootElement.Q<SliderInt>("scale-slider");
            m_ScaleSlider.lowValue = kScaleMin;
            m_ScaleSlider.highValue = kScaleMax;
            m_ScaleSlider.SetValueWithoutNotify(Scale);
            m_ScaleSlider.RegisterCallback<ChangeEvent<int>>(SetScale);

            m_ScaleValueLabel = m_RootElement.Q<Label>("scale-value-label");
            m_ScaleValueLabel.text = Scale.ToString();

            m_FitToScreenToggle = m_RootElement.Q<ToolbarToggle>("fit-to-screen");
            m_FitToScreenToggle.RegisterValueChangedCallback(FitToScreen);
            m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);
            #endregion

            #region Rotate
            var namePostfix = EditorGUIUtility.isProSkin ? "_dark" : "_light";
            const string iconPath = "packages/com.unity.device-simulator/SimulatorResources/Icons";

            m_RootElement.Q<Image>("rotate-cw-image").image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{iconPath}/rotate_cw{namePostfix}.png");
            m_RootElement.Q<VisualElement>("rotate-cw").AddManipulator(new Clickable(RotateDeviceCW));

            m_RootElement.Q<Image>("rotate-ccw-image").image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{iconPath}/rotate_ccw{namePostfix}.png");
            m_RootElement.Q<VisualElement>("rotate-ccw").AddManipulator(new Clickable(RotateDeviceCCW));
            #endregion

            // Highlight safe area.
            m_HighlightSafeAreaToggle = m_RootElement.Q<ToolbarToggle>("highlight-safe-area");
            m_HighlightSafeAreaToggle.RegisterValueChangedCallback((evt) => {
                HighlightSafeArea = evt.newValue;
            });
            m_HighlightSafeAreaToggle.SetValueWithoutNotify(HighlightSafeArea);
        }

        public void Update(DeviceInfo deviceInfo, ScreenSimulation screenSimulation)
        {
            m_DeviceInfo = deviceInfo;
            m_ScreenSimulation = screenSimulation;

            m_DeviceInfo.LoadOverlayImage();
            var screen = m_DeviceInfo.Screens[0];

            m_PreviewRenderer.SetDevice(screen.width, screen.height, screen.presentation.borderSize);
            m_PreviewRenderer.ScreenOrientation = m_ScreenSimulation.orientation;
            m_PreviewRenderer.ScreenInsets = m_ScreenSimulation.Insets;
            m_PreviewRenderer.SafeArea = m_ScreenSimulation.ScreenSpaceSafeArea;

            m_ScreenSimulation.OnOrientationChanged += autoRotate => m_PreviewRenderer.ScreenOrientation = m_ScreenSimulation.orientation;
            m_ScreenSimulation.OnInsetsChanged += insets => m_PreviewRenderer.ScreenInsets = insets;
            m_ScreenSimulation.OnScreenSpaceSafeAreaChanged += safeArea => m_PreviewRenderer.SafeArea = safeArea;

            SetScrollViewTopPadding();

            if (m_FitToScreenEnabled)
                FitToScreenScale();
        }

        private void HideControlPanel()
        {
            m_ControlPanelHidden = !m_ControlPanelHidden;

            m_HideControlPanel.style.backgroundImage = (Texture2D)EditorGUIUtility.Load($"Icons/d_tab_{(m_ControlPanelHidden ? "next" : "prev")}@2x.png");
            OnControlPanelHiddenChanged?.Invoke(m_ControlPanelHidden);
        }

        private void InitInactiveMsg()
        {
            m_InactiveMsgContainer = m_RootElement.Q<VisualElement>("inactive-msg-container");
            var closeInactiveMsg = m_RootElement.Q<Image>("close-inactive-msg");
            closeInactiveMsg.image = AssetDatabase.LoadAssetAtPath<Texture2D>($"packages/com.unity.device-simulator/SimulatorResources/Icons/close_button.png");
            closeInactiveMsg.AddManipulator(new Clickable(CloseInactiveMsg));

            SetInactiveMsgState(false);
        }

        private void InitPreview()
        {
            m_ScrollViewContainer = m_RootElement.Q<VisualElement>("scrollview-container");
            m_ScrollViewContainer.RegisterCallback<WheelEvent>(OnScrollWheel, TrickleDown.TrickleDown);
            m_ScrollViewContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_ScrollView = m_RootElement.Q<ScrollView>("preview-scroll-view");

            m_PreviewRenderer = new DeviceView(Quaternion.Euler(0, 0, 360 - Rotation), Scale / 100f) {ShowSafeArea = HighlightSafeArea};
            m_PreviewRenderer.AddManipulator(m_TouchEventManipulator = new TouchEventManipulator(m_InputProvider));
            m_PreviewRenderer.OnViewToScreenChanged += () => { m_TouchEventManipulator.PreviewImageRendererSpaceToScreenSpace = m_PreviewRenderer.ViewToScreen; };
            m_PreviewRendererContainer = m_RootElement.Q<VisualElement>("preview-container");
            m_PreviewRendererContainer.Add(m_PreviewRenderer);
            var userSettings = DeviceSimulatorUserSettingsProvider.LoadOrCreateSettings();
            m_PreviewRenderer.SafeAreaColor = userSettings.SafeAreaHighlightColor;
            m_PreviewRenderer.SafeAreaLineWidth = userSettings.SafeAreaHighlightLineWidth;
        }

        private void SetScale(ChangeEvent<int> e)
        {
            UpdateScale(e.newValue);

            m_FitToScreenEnabled = false;
            m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);
        }

        private void FitToScreen(ChangeEvent<bool> evt)
        {
            m_FitToScreenEnabled = evt.newValue;
            if (m_FitToScreenEnabled)
                FitToScreenScale();
            UpdateScrollbars();
        }

        private void FitToScreenScale()
        {
            Vector2 screenSize = m_ScrollViewContainer.worldBound.size;
            var x = screenSize.x / m_PreviewRenderer.style.width.value.value;
            var y = screenSize.y / m_PreviewRenderer.style.height.value.value;

            UpdateScale(ClampScale(Mathf.FloorToInt(Scale * Math.Min(x, y))));
        }

        private void UpdateScale(int newScale)
        {
            Scale = newScale;

            m_ScaleValueLabel.text = newScale.ToString();
            m_ScaleSlider.SetValueWithoutNotify(newScale);

            SetScrollViewTopPadding();
        }

        private void SetRotationDegree(int newValue)
        {
            Rotation = newValue;
            var rotationQuaternion = Quaternion.Euler(0, 0, 360 - Rotation);
            m_InputProvider.Rotation = rotationQuaternion;

            SetScrollViewTopPadding();

            if (m_FitToScreenEnabled)
                FitToScreenScale();
        }

        private void RotateDeviceCW()
        {
            // Always rotate to 0/90/180/270 degrees if clicking rotation buttons.
            if (m_RotationDegree % 90 != 0)
                m_RotationDegree = Convert.ToInt32(Math.Ceiling(m_RotationDegree / 90f)) * 90;
            m_RotationDegree = (m_RotationDegree + 270) % 360;

            SetRotationDegree(m_RotationDegree);
        }

        private void RotateDeviceCCW()
        {
            // Always rotate to 0/90/180/270 degrees if clicking rotation buttons.
            if (m_RotationDegree % 90 != 0)
                m_RotationDegree = Convert.ToInt32(Math.Floor(m_RotationDegree / 90f)) * 90;
            m_RotationDegree = (m_RotationDegree + 90) % 360;

            SetRotationDegree(m_RotationDegree);
        }

        private void CloseInactiveMsg()
        {
            SetInactiveMsgState(false);
        }

        private void SetInactiveMsgState(bool shown)
        {
            m_InactiveMsgContainer.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
            m_InactiveMsgContainer.style.position = shown ? Position.Relative : Position.Absolute;
        }

        private void OnScrollWheel(WheelEvent evt)
        {
            var newScale = (int)(Scale - evt.delta.y);
            UpdateScale(ClampScale(newScale));
            evt.StopPropagation();

            m_FitToScreenEnabled = false;
            m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);
        }

        private int ClampScale(int scale)
        {
            if (scale < kScaleMin)
                return kScaleMin;
            if (scale > kScaleMax)
                return kScaleMax;

            return scale;
        }

        public void OnSimulationStateChanged(SimulationState simulationState)
        {
            SetInactiveMsgState(simulationState == SimulationState.Disabled);
        }

        // Fix Case 1227475, something's wrong with preview window size calculations when scroll bars are visible
        // Thus during FitToScreen the preview window goes out of bounds
        // Disabling scrollbars when FitToScreen is true fixes out of bounds issue
        private void UpdateScrollbars()
        {
            m_ScrollView.showHorizontal = m_ScrollView.showVertical = !m_FitToScreenEnabled;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // The reason why we call UpdateScrollbars here and not tie to m_FitToScreenEnabled setter
            // is related to yet another bug in ScrollView.OnGeometryChanged
            // it seems it doesn't respect what's set in showHorizontal/showVertical
            // and calls UpdateScrollers with unexpected values on next OnGeometryChanged
            // UI guys told me that showHorizontal/showVertical are used to force show scrollbars, which is unexpected
            // since 'false' is also an acceptable value
            UpdateScrollbars();
            if (m_FitToScreenEnabled)
                FitToScreenScale();

            SetScrollViewTopPadding();
        }

        // This is a workaround to fix https://github.com/Unity-Technologies/com.unity.device-simulator/issues/79.
        private void SetScrollViewTopPadding()
        {
            var scrollViewHeight = m_ScrollView.worldBound.height;
            if (float.IsNaN(scrollViewHeight))
                return;

            m_ScrollView.style.paddingTop = scrollViewHeight > m_PreviewRenderer.style.height.value.value ? (scrollViewHeight - m_PreviewRenderer.style.height.value.value) / 2 : 0;
        }
    }
}
