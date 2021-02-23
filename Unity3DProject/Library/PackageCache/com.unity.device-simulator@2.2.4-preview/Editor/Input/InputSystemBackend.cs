#if INPUT_SYSTEM_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem;

namespace Unity.DeviceSimulator
{
    internal class InputSystemBackend : IInputBackend
    {
        private Touchscreen m_SimulatorTouchscreen;
        private List<InputDevice> m_DisabledDevices;

        public InputSystemBackend()
        {
            // UGUI gets confused when multiple pointers for example mouse and touchscreen are sending data at the same time.
            // @rene recommended disabling all native mice.
            m_DisabledDevices = new List<InputDevice>();
            foreach (var device in InputSystem.devices)
            {
                if (device.native && device is Mouse && device.enabled)
                {
                    InputSystem.DisableDevice(device);
                    m_DisabledDevices.Add(device);
                }
            }

            if (Touchscreen.current == null)
                m_SimulatorTouchscreen = InputSystem.AddDevice<Touchscreen>();
        }

        public void Touch(int id, Vector2 position, SimulatorTouchPhase phase)
        {
            // Input System does not accept 0 as id
            id++;

            var screen = Touchscreen.current;
            if (screen == null)
            {
                return;
            }

            InputSystem.QueueStateEvent(screen,
                new TouchState
                {
                    touchId = id,
                    phase = ToInputSystem(phase),
                    position = position
                });
        }

        private static TouchPhase ToInputSystem(SimulatorTouchPhase original)
        {
            switch (original)
            {
                case SimulatorTouchPhase.None:
                    return TouchPhase.None;
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
                    throw new ArgumentOutOfRangeException(nameof(original), original, "Unexpected value");
            }
        }

        public void Dispose()
        {
            if (m_SimulatorTouchscreen != null)
                InputSystem.RemoveDevice(m_SimulatorTouchscreen);

            foreach (var device in m_DisabledDevices)
            {
                InputSystem.EnableDevice(device);
            }
        }
    }
}

#endif
