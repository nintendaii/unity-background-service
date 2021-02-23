using UnityEngine;

namespace Unity.DeviceSimulator.Tests
{
    internal class OnLowMemoryTest
    {
        public int Counter = 0;

        public OnLowMemoryTest()
        {
            Application.lowMemory += OnLowMemory;
            Application.lowMemory += OnLowMemory_1;
        }

        void OnLowMemory()
        {
            Counter++;
        }

        void OnLowMemory_1()
        {
            Counter++;
        }
    }
}
