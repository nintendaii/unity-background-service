using System;
using UnityEngine;

namespace Unity.DeviceSimulator
{
    internal enum SimulatorTouchPhase
    {
        None,
        Began,
        Moved,
        Ended,
        Canceled,
        Stationary
    }

    internal interface IInputBackend : IDisposable
    {
        void Touch(int touchId, Vector2 position, SimulatorTouchPhase phase);
    }
}
