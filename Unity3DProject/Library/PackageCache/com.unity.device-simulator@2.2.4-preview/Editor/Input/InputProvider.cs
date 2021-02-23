using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    internal enum MousePhase { Start, Move, End }

    internal class InputProvider : IInputProvider, IDisposable
    {
        private bool m_TouchFromMouseActive;
        private int m_ScreenWidth;
        private int m_ScreenHeight;
        private ScreenSimulation m_ScreenSimulation;
        private List<IInputBackend> m_InputBackends;

        private Quaternion m_Rotation = Quaternion.identity;

        public Action<Quaternion> OnRotation { get; set; }
        public Vector2 PointerPosition { private set; get; }
        public bool IsPointerInsideDeviceScreen { private set; get; }

        public Quaternion Rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value;
                OnRotation?.Invoke(value);
            }
        }

        public InputProvider()
        {
            PointerPosition = new Vector2(-1, -1);
            IsPointerInsideDeviceScreen = false;
            m_InputBackends = new List<IInputBackend>();
#if INPUT_SYSTEM_INSTALLED
            var playerSettings = PlayerSettings.GetSerializedObject();
#if UNITY_2020_2_OR_NEWER
            var activeInputHandler = playerSettings.FindProperty("activeInputHandler").intValue;
            var newSystemEnabled = activeInputHandler == 1 || activeInputHandler == 2;
            var oldSystemEnabled = activeInputHandler == 0 || activeInputHandler == 2;
#else
            var newSystemEnabled = playerSettings.FindProperty("enableNativePlatformBackendsForNewInputSystem").boolValue;
            var oldSystemEnabled = !playerSettings.FindProperty("disableOldInputManagerSupport").boolValue;
#endif
            if (newSystemEnabled)
                m_InputBackends.Add(new InputSystemBackend());
            if (oldSystemEnabled)
                m_InputBackends.Add(new LegacyInputBackend());
#else
            m_InputBackends.Add(new LegacyInputBackend());
#endif
        }

        public void InitTouchInput(int screenWidth, int screenHeight, ScreenSimulation screenSimulation)
        {
            m_ScreenWidth = screenWidth;
            m_ScreenHeight = screenHeight;
            m_ScreenSimulation = screenSimulation;
            CancelAllTouches();
        }

        public void TouchFromMouse(Vector2 position, MousePhase mousePhase)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return;

            // Clamping position inside the device screen. UI element that sends input events also includes the device border and we don't want to register inputs there.
            IsPointerInsideDeviceScreen = true;
            if (position.x < 0)
            {
                position.x = 0;
                IsPointerInsideDeviceScreen = false;
            }
            else if (position.x > m_ScreenWidth)
            {
                position.x = m_ScreenWidth;
                IsPointerInsideDeviceScreen = false;
            }
            if (position.y < 0)
            {
                position.y = 0;
                IsPointerInsideDeviceScreen = false;
            }
            else if (position.y > m_ScreenHeight)
            {
                position.y = m_ScreenHeight;
                IsPointerInsideDeviceScreen = false;
            }

            PointerPosition = ScreenPixelToTouchCoordinate(position);

            if (!m_TouchFromMouseActive && mousePhase != MousePhase.Start)
                return;

            var phase = SimulatorTouchPhase.None;

            if (!IsPointerInsideDeviceScreen)
            {
                switch (mousePhase)
                {
                    case MousePhase.Start:
                        return;
                    case MousePhase.Move:
                    case MousePhase.End:
                        phase = SimulatorTouchPhase.Ended;
                        m_TouchFromMouseActive = false;
                        break;
                }
            }
            else
            {
                switch (mousePhase)
                {
                    case MousePhase.Start:
                        phase = SimulatorTouchPhase.Began;
                        m_TouchFromMouseActive = true;
                        break;
                    case MousePhase.Move:
                        phase = SimulatorTouchPhase.Moved;
                        break;
                    case MousePhase.End:
                        phase = SimulatorTouchPhase.Ended;
                        m_TouchFromMouseActive = false;
                        break;
                }
            }

            foreach (var inputBackend in m_InputBackends)
            {
                inputBackend.Touch(0, PointerPosition, phase);
            }
        }

        /// <summary>
        /// Converting from screen pixel to coordinates that are returned by input. Input coordinates change depending on:
        /// current resolution, full screen or not (insets), and orientation.
        /// </summary>
        /// <param name="position">Pixel position in portrait orientation, with origin at the top left corner</param>
        /// <returns>Position dependent on current resolution, insets and orientation, with origin at the bottom left of the rendered rect in the current orientation.</returns>
        private Vector2 ScreenPixelToTouchCoordinate(Vector2 position)
        {
            // First calculating which pixel is being touched inside the pixel rect where game is rendered in portrait orientation, due to insets this might not be full screen
            var renderedAreaPortraitWidth = m_ScreenWidth - m_ScreenSimulation.Insets.x - m_ScreenSimulation.Insets.z;
            var renderedAreaPortraitHeight = m_ScreenHeight - m_ScreenSimulation.Insets.y - m_ScreenSimulation.Insets.w;

            var touchedPixelPortraitX = position.x - m_ScreenSimulation.Insets.x;
            var touchedPixelPortraitY = position.y - m_ScreenSimulation.Insets.y;

            // Converting touch so that no matter the orientation origin would be at the bottom left corner
            float touchedPixelX = 0;
            float touchedPixelY = 0;
            switch (m_ScreenSimulation.orientation)
            {
                case ScreenOrientation.Portrait:
                    touchedPixelX = touchedPixelPortraitX;
                    touchedPixelY = renderedAreaPortraitHeight - touchedPixelPortraitY;
                    break;
                case ScreenOrientation.PortraitUpsideDown:
                    touchedPixelX = renderedAreaPortraitWidth - touchedPixelPortraitX;
                    touchedPixelY = touchedPixelPortraitY;
                    break;
                case ScreenOrientation.LandscapeLeft:
                    touchedPixelX = touchedPixelPortraitY;
                    touchedPixelY = touchedPixelPortraitX;
                    break;
                case ScreenOrientation.LandscapeRight:
                    touchedPixelX = renderedAreaPortraitHeight - touchedPixelPortraitY;
                    touchedPixelY = renderedAreaPortraitWidth - touchedPixelPortraitX;
                    break;
            }

            // Scaling in case rendering resolution does not match screen pixels
            float scaleX;
            float scaleY;
            if (m_ScreenSimulation.IsRenderingLandscape)
            {
                scaleX = m_ScreenSimulation.Width / renderedAreaPortraitHeight;
                scaleY = m_ScreenSimulation.Height / renderedAreaPortraitWidth;
            }
            else
            {
                scaleX = m_ScreenSimulation.Width / renderedAreaPortraitWidth;
                scaleY = m_ScreenSimulation.Height / renderedAreaPortraitHeight;
            }

            return new Vector2(touchedPixelX * scaleX, touchedPixelY * scaleY);
        }

        public void CancelAllTouches()
        {
            if (m_TouchFromMouseActive)
            {
                m_TouchFromMouseActive = false;
                foreach (var inputBackend in m_InputBackends)
                {
                    inputBackend.Touch(0, Vector2.zero, SimulatorTouchPhase.Canceled);
                }
            }
        }

        public void Dispose()
        {
            CancelAllTouches();
            foreach (var inputBackend in m_InputBackends)
            {
                inputBackend.Dispose();
            }
        }
    }
}
