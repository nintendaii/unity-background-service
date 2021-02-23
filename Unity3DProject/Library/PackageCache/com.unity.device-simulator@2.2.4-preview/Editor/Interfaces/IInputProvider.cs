using System;
using UnityEngine;

namespace Unity.DeviceSimulator
{
    internal interface IInputProvider
    {
        Action<Quaternion> OnRotation { get; set; }
        Quaternion Rotation { get; }
    }
}
