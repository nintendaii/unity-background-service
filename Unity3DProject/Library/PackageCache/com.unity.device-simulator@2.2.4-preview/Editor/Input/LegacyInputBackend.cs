using System;
using UnityEditor;
using UnityEngine;

namespace Unity.DeviceSimulator
{
    internal class LegacyInputBackend : IInputBackend
    {
        private int m_LastEventFrame = -1;
        private int m_LastSubmittedEventFrame = -1;

        private int m_NextId;
        private Vector2 m_NextPosition;
        private TouchPhase m_NextPhase = TouchPhase.Canceled;

        public LegacyInputBackend()
        {
            EditorApplication.update += SubmitTouch;
        }

        private void SubmitTouch()
        {
            if (m_LastEventFrame != m_LastSubmittedEventFrame)
            {
                Input.SimulateTouch(m_NextId, m_NextPosition, m_NextPhase);
                m_LastSubmittedEventFrame = m_LastEventFrame;
            }
            else if (m_NextPhase == TouchPhase.Moved || m_NextPhase == TouchPhase.Began)
            {
                Input.SimulateTouch(m_NextId, m_NextPosition, TouchPhase.Stationary);
            }
        }

        public void Touch(int id, Vector2 position, SimulatorTouchPhase phase)
        {
            // Input.SimulateTouch expects a single event each frame and if sent two like Moved then Ended will create a separate touch.
            // So we delay calling Input.SimulateTouch until update.
            var newPhase = ToLegacy(phase);
            if (Time.frameCount == m_LastEventFrame)
            {
                if (m_NextPhase == TouchPhase.Began)
                    return;
                else if (m_NextPhase == TouchPhase.Moved && newPhase == TouchPhase.Ended)
                {
                    m_NextPhase = TouchPhase.Ended;
                    m_NextPosition = position;
                }
            }
            else
            {
                m_NextPosition = position;
                m_NextPhase = newPhase;
                m_NextId = id;
                m_LastEventFrame = Time.frameCount;
            }
        }

        private static TouchPhase ToLegacy(SimulatorTouchPhase original)
        {
            switch (original)
            {
                case SimulatorTouchPhase.Began:
                    return TouchPhase.Began;
                case SimulatorTouchPhase.Moved:
                    return TouchPhase.Moved;
                case SimulatorTouchPhase.Ended:
                    return TouchPhase.Ended;
                case SimulatorTouchPhase.Canceled:
                    return TouchPhase.Canceled;
                case SimulatorTouchPhase.Stationary:
                    return TouchPhase.Stationary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(original), original, "None is not a supported phase with legacy input system");
            }
        }

        public void Dispose()
        {
            EditorApplication.update -= SubmitTouch;
        }
    }
}
