using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    [EditorWindowTitle(title = "Simulator", icon = "packages/com.unity.device-simulator/SimulatorResources/Icons/UnityEditor.DeviceSimulation.SimulatorWindow.png")]
    internal class SimulatorWindow : PlayModeView, IHasCustomMenu, ISerializationCallbackReceiver
    {
        const string kResourcesPath = "packages/com.unity.device-simulator/SimulatorResources";

        private SimulationState m_State = SimulationState.Enabled;
        private ScreenSimulation m_ScreenSimulation;
        private SystemInfoSimulation m_SystemInfoSimulation;
        private ApplicationSimulation m_ApplicationSimulation;

        private InputProvider m_InputProvider;

        private DeviceDatabase m_DeviceDatabase;

        private int CurrentDeviceIndex
        {
            get => m_CurrentDeviceIndex;
            set
            {
                m_CurrentDeviceIndex = value;
                CurrentDeviceInfo = m_DeviceDatabase.GetDevice(m_CurrentDeviceIndex);
            }
        }
        private int m_CurrentDeviceIndex = -1;
        private DeviceInfo CurrentDeviceInfo;

        private string m_DeviceSearchContent = string.Empty;

        [SerializeField] private SimulatorSerializationStates m_SimulatorSerializationStates;

        private VisualElement m_DeviceListMenu;
        private TextElement m_SelectedDeviceName;

        private ToolbarButton m_DeviceRestart;

        private TwoPaneSplitView m_Splitter;
        private SimulatorControlPanel m_ControlPanel;
        private SimulatorPreviewPanel m_PreviewPanel;

        [MenuItem("Window/General/Device Simulator", false, 2000)]
        public static void ShowWindow()
        {
            SimulatorWindow window = GetWindow<SimulatorWindow>();
            window.Show();
        }

        private void LoadRenderDoc()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                RenderDoc.Load();
                ShaderUtil.RecreateGfxDevice();
            }
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (RenderDoc.IsInstalled() && !RenderDoc.IsLoaded())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent(RenderDocUtil.loadRenderDocLabel), false, LoadRenderDoc);
            }
        }

        void OnEnable()
        {
            var titleImagePath = EditorGUIUtility.isProSkin ? $"{kResourcesPath}/Icons/d_UnityEditor.DeviceSimulation.SimulatorWindow" : $"{kResourcesPath}/Icons/UnityEditor.DeviceSimulation.SimulatorWindow";
            titleImagePath += EditorGUIUtility.pixelsPerPoint > 1.5 ? "@2x.png" : ".png";
            titleContent = new GUIContent("Simulator", AssetDatabase.LoadAssetAtPath<Texture2D>(titleImagePath));

            m_InputProvider = new InputProvider();
            if (m_SimulatorSerializationStates != null)
                m_InputProvider.Rotation = m_SimulatorSerializationStates.rotation;

            autoRepaintOnSceneChange = true;

            InitDeviceInfoList();
            SetCurrentDeviceIndex(m_SimulatorSerializationStates, false);

            this.clearColor = Color.black;
            this.playModeViewName = "Device Simulator";
            this.showGizmos = false;
            this.targetDisplay = 0;
            this.renderIMGUI = true;
            this.targetSize = new Vector2(CurrentDeviceInfo.Screens[0].width, CurrentDeviceInfo.Screens[0].height);

            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>($"{kResourcesPath}/StyleSheets/styles_{(EditorGUIUtility.isProSkin ? "dark" : "light")}.uss"));

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{kResourcesPath}/UXML/ui_device_simulator.uxml");
            asset.CloneTree(rootVisualElement);

            var playerSettings = new SimulationPlayerSettings();
            InitSimulation(playerSettings);

            InitToolbar();
            m_Splitter = rootVisualElement.Q<TwoPaneSplitView>("splitter");
            m_Splitter.ApplySerializationStates(m_SimulatorSerializationStates);

            m_ControlPanel = new SimulatorControlPanel(rootVisualElement.Q<VisualElement>("control-panel"), CurrentDeviceInfo, m_SystemInfoSimulation,
                m_ScreenSimulation, m_ApplicationSimulation, playerSettings, m_SimulatorSerializationStates);
            m_ControlPanel.ApplySerializationStates(m_SimulatorSerializationStates);

            m_PreviewPanel = new SimulatorPreviewPanel(rootVisualElement.Q<VisualElement>("preview-panel"), m_InputProvider)
            {
                OnControlPanelHiddenChanged = HideControlPanel
            };
            m_PreviewPanel.Update(CurrentDeviceInfo, m_ScreenSimulation);
            m_PreviewPanel.ApplySerializationStates(m_SimulatorSerializationStates);
        }

        private void OnGUI()
        {
            if (GetMainPlayModeView() != this)
                return;

            var type = Event.current.type;

            if (type == EventType.Repaint)
            {
                var mousePositionInUICoordinates = new Vector2(m_InputProvider.PointerPosition.x, m_ScreenSimulation.Height - m_InputProvider.PointerPosition.y);
                var previewTexture = RenderView(mousePositionInUICoordinates, false);
                m_PreviewPanel.PreviewTexture = previewTexture.IsCreated() ? previewTexture : null;
                CurrentDeviceInfo?.LoadOverlayImage();
                m_PreviewPanel.OverlayTexture = CurrentDeviceInfo?.Screens[0].presentation.overlay;
            }
            else if (type != EventType.Layout && type != EventType.Used)
            {
                if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                    return;

                // MouseDown events outside game view rect are not send to scripts but MouseUp events are (see below)
                if (Event.current.rawType == EventType.MouseDown && !m_InputProvider.IsPointerInsideDeviceScreen)
                    return;

                var editorMousePosition = Event.current.mousePosition;
                Event.current.mousePosition = new Vector2(m_InputProvider.PointerPosition.x, m_ScreenSimulation.Height - m_InputProvider.PointerPosition.y);

                EditorGUIUtility.QueueGameViewInputEvent(Event.current);

                var useEvent = !(Event.current.rawType == EventType.MouseUp && !m_InputProvider.IsPointerInsideDeviceScreen);

                if (type == EventType.ExecuteCommand || type == EventType.ValidateCommand)
                    useEvent = false;

                if (useEvent)
                    Event.current.Use();
                else
                    Event.current.mousePosition = editorMousePosition;
            }
        }

        private void InitSimulation(SimulationPlayerSettings playerSettings)
        {
            m_ScreenSimulation?.Dispose();

            m_ScreenSimulation = new ScreenSimulation(CurrentDeviceInfo, m_InputProvider, playerSettings);
            targetSize = new Vector2(m_ScreenSimulation.currentResolution.width, m_ScreenSimulation.currentResolution.height);
            m_ScreenSimulation.OnResolutionChanged += (width, height) => { targetSize = new Vector2(width, height); };

            m_InputProvider.InitTouchInput(CurrentDeviceInfo.Screens[0].width, CurrentDeviceInfo.Screens[0].height, m_ScreenSimulation);

            m_SystemInfoSimulation?.Dispose();

            var settings = DeviceSimulatorProjectSettingsProvider.LoadOrCreateSettings();
            var whitelistedAssemblies = new List<string>(settings.SystemInfoAssemblies);

            if (settings.SystemInfoDefaultAssembly)
                whitelistedAssemblies.Add("Assembly-CSharp.dll");

            SimulatorUtilities.CheckShimmedAssemblies(whitelistedAssemblies);

            m_SystemInfoSimulation = new SystemInfoSimulation(CurrentDeviceInfo, playerSettings, whitelistedAssemblies);

            // No need to reinitialize ApplicationSimulation.
            if (m_ApplicationSimulation == null)
                m_ApplicationSimulation = new ApplicationSimulation(CurrentDeviceInfo, whitelistedAssemblies);

            DeviceSimulatorCallbacks.InvokeOnDeviceChange();
        }

        void Update()
        {
            bool simulationStateChanged = false;
            if (m_State == SimulationState.Disabled && GetMainPlayModeView() == this)
            {
                m_State = SimulationState.Enabled;
                simulationStateChanged = true;
                m_ScreenSimulation.Enable();
                m_SystemInfoSimulation.Enable();
                m_ApplicationSimulation.Enable();
                DeviceSimulatorCallbacks.InvokeOnDeviceChange();
            }
            else if (m_State == SimulationState.Enabled && GetMainPlayModeView() != this)
            {
                m_State = SimulationState.Disabled;
                simulationStateChanged = true;
                m_ScreenSimulation.Disable();
                m_SystemInfoSimulation.Disable();
                m_ApplicationSimulation.Disable();

                // Assumption here is that another Simulator instance will call OnDeviceChange event when it becomes MainPlayModeView
                // so we don't need to call it here, but if it's not another Simulator window then we need to call the event.
                if (GetMainPlayModeView().GetType() != typeof(SimulatorWindow))
                    DeviceSimulatorCallbacks.InvokeOnDeviceChange();
            }

            if (simulationStateChanged)
                m_PreviewPanel.OnSimulationStateChanged(m_State);
        }

        private void OnFocus()
        {
            SetFocus(true);

            // Stealing shim back in case some other system started using it. One quirky situation where this happens is unmaximizing
            // a simulator window, because for just a moment a new simulator window is created and steals shim, but the current window
            // is not aware that it lost shim.
            if (m_State == SimulationState.Enabled)
            {
                ShimManager.UseShim(m_ScreenSimulation);
                ShimManager.UseShim(m_SystemInfoSimulation);
                ShimManager.UseShim(m_ApplicationSimulation);
            }
        }

        private void OnDisable()
        {
            m_InputProvider?.Dispose();
            m_ScreenSimulation?.Dispose();
            m_SystemInfoSimulation?.Dispose();
            m_ApplicationSimulation?.Dispose();
        }

        private void BeforeSerializeStates()
        {
            m_SimulatorSerializationStates = new SimulatorSerializationStates()
            {
                friendlyName = CurrentDeviceInfo.friendlyName
            };

            m_ControlPanel.StoreSerializationStates(ref m_SimulatorSerializationStates);
            m_PreviewPanel.StoreSerializationStates(ref m_SimulatorSerializationStates);
            m_Splitter.StoreSerializationStates(ref m_SimulatorSerializationStates);

            foreach (var foldout in m_SimulatorSerializationStates.controlPanelFoldouts)
            {
                m_SimulatorSerializationStates.controlPanelFoldoutKeys.Add(foldout.Key);
                m_SimulatorSerializationStates.controlPanelFoldoutValues.Add(foldout.Value);
            }

            foreach (var extension in m_SimulatorSerializationStates.extensions)
            {
                m_SimulatorSerializationStates.extensionNames.Add(extension.Key);
                m_SimulatorSerializationStates.extensionStates.Add(extension.Value);
            }
        }

        private void AfterDeserializeStates(SimulatorSerializationStates states)
        {
            states.rotation = Quaternion.Euler(0, 0, 360 - states.rotationDegree);

            Assert.AreEqual(states.controlPanelFoldoutKeys.Count, states.controlPanelFoldoutValues.Count);
            for (int index = 0; index < states.controlPanelFoldoutKeys.Count; ++index)
            {
                states.controlPanelFoldouts.Add(states.controlPanelFoldoutKeys[index], states.controlPanelFoldoutValues[index]);
            }

            Assert.AreEqual(states.extensionNames.Count, states.extensionStates.Count);
            for (int index = 0; index < states.extensionNames.Count; ++index)
            {
                states.extensions.Add(states.extensionNames[index], states.extensionStates[index]);
            }
        }

#if UNITY_2020_1_OR_NEWER
        public void OnBeforeSerialize()
        {
            BeforeSerializeStates();
        }

        public void OnAfterDeserialize()
        {
            AfterDeserializeStates(m_SimulatorSerializationStates);
        }

#elif UNITY_2019_4
        public new void OnBeforeSerialize()
        {
            BeforeSerializeStates();
        }

        public new void OnAfterDeserialize()
        {
            AfterDeserializeStates(m_SimulatorSerializationStates);
        }

#else
        protected override string SerializeView()
        {
            BeforeSerializeStates();
            return JsonUtility.ToJson(m_SimulatorSerializationStates);
        }

        protected override void DeserializeView(string serializedView)
        {
            m_SimulatorSerializationStates = JsonUtility.FromJson<SimulatorSerializationStates>(serializedView);
            AfterDeserializeStates(m_SimulatorSerializationStates);

            SetCurrentDeviceIndex(m_SimulatorSerializationStates, true);
            m_InputProvider.Rotation = m_SimulatorSerializationStates.rotation;
            m_Splitter.ApplySerializationStates(m_SimulatorSerializationStates);
            m_ControlPanel.ApplySerializationStates(m_SimulatorSerializationStates);
            m_PreviewPanel.ApplySerializationStates(m_SimulatorSerializationStates);
        }

        public new void OnBeforeSerialize()
        {
            BeforeSerializeStates();
            base.OnBeforeSerialize();
        }

        public new void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            AfterDeserializeStates(m_SimulatorSerializationStates);
        }

#endif

        private void InitDeviceInfoList()
        {
            m_DeviceDatabase = new DeviceDatabase();

            Assert.AreNotEqual(0, m_DeviceDatabase.m_Devices.Count, "No devices found!");
            CurrentDeviceIndex = 0;
        }

        void SetCurrentDeviceIndex(SimulatorSerializationStates states, bool triggerOnDeviceSelected)
        {
            if (states == null || string.IsNullOrEmpty(states.friendlyName))
                return;

            for (int index = 0; index < m_DeviceDatabase.m_Devices.Count; ++index)
            {
                if (m_DeviceDatabase.m_Devices[index].friendlyName == states.friendlyName)
                {
                    if (triggerOnDeviceSelected)
                        OnDeviceSelected(index);
                    else
                        CurrentDeviceIndex = index;

                    break;
                }
            }
        }

        private void InitToolbar()
        {
            var playModeViewTypeMenu = rootVisualElement.Q<ToolbarMenu>("playmode-view-menu");
            playModeViewTypeMenu.text = GetWindowTitle(GetType());

            var types = GetAvailableWindowTypes();
            foreach (var type in types)
            {
                var status = type.Key == GetType() ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                playModeViewTypeMenu.menu.AppendAction(type.Value, HandleWindowSelection, HandleWindowSelection => status, type.Key);
            }

            m_DeviceRestart = rootVisualElement.Q<ToolbarButton>("reload-player-settings");
            m_DeviceRestart.clickable = new Clickable(RestartSimulation);

            m_DeviceListMenu = rootVisualElement.Q<VisualElement>("device-list-menu");
            m_DeviceListMenu.AddManipulator(new Clickable(ShowDeviceInfoList));

            m_SelectedDeviceName = m_DeviceListMenu.Q<TextElement>("selected-device-name");
            m_SelectedDeviceName.text = CurrentDeviceInfo.friendlyName;
        }

        private void HandleWindowSelection(object typeData)
        {
            var type = (Type)((DropdownMenuAction)typeData).userData;
            if (type != null)
                SwapMainWindow(type);
        }

        private void RestartSimulation()
        {
            var playerSettings = new SimulationPlayerSettings();

            InitSimulation(playerSettings);

            m_ControlPanel.Update(CurrentDeviceInfo, m_SystemInfoSimulation, m_ScreenSimulation, playerSettings);
            m_PreviewPanel.Update(CurrentDeviceInfo, m_ScreenSimulation);
        }

        private void ShowDeviceInfoList()
        {
            var rect = new Rect(m_DeviceListMenu.worldBound.position + new Vector2(1, m_DeviceListMenu.worldBound.height), new Vector2());
            var maximumVisibleDeviceCount = DeviceSimulatorUserSettingsProvider.LoadOrCreateSettings().MaximumVisibleDeviceCount;

            var deviceListPopup = new DeviceListPopup(m_DeviceDatabase.m_Devices, m_CurrentDeviceIndex, maximumVisibleDeviceCount, m_DeviceSearchContent);
            deviceListPopup.OnDeviceSelected += OnDeviceSelected;
            deviceListPopup.OnSearchInput += OnSearchInput;

            UnityEditor.PopupWindow.Show(rect, deviceListPopup);
        }

        private void OnDeviceSelected(int selectedDeviceIndex)
        {
            if (CurrentDeviceIndex == selectedDeviceIndex)
                return;

            CurrentDeviceIndex = selectedDeviceIndex;
            m_SelectedDeviceName.text = CurrentDeviceInfo.friendlyName;

            RestartSimulation();
        }

        private void OnSearchInput(string searchContent)
        {
            m_DeviceSearchContent = searchContent;
        }

        private void HideControlPanel(bool hidden)
        {
            m_Splitter.HideLeftPanel(hidden);
        }
    }
}
