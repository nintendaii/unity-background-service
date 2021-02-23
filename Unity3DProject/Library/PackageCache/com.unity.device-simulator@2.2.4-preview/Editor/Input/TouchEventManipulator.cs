using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DeviceSimulator
{
    internal class TouchEventManipulator : MouseManipulator
    {
        public Matrix4x4 PreviewImageRendererSpaceToScreenSpace { get; set; }
        private InputProvider m_InputProvider = null;

        public TouchEventManipulator(InputProvider inputProvider)
        {
            m_InputProvider = inputProvider;
            activators.Add(new ManipulatorActivationFilter() {button = MouseButton.LeftMouse});
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            SendMouseEvent(evt, MousePhase.Start);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            SendMouseEvent(evt, MousePhase.Move);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            SendMouseEvent(evt, MousePhase.End);
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            SendMouseEvent(evt, MousePhase.End);
        }

        private void SendMouseEvent(IMouseEvent evt, MousePhase phase)
        {
            if (!activators.Any(filter => filter.Matches(evt)))
                return;

            var position = PreviewImageRendererSpaceToScreenSpace.MultiplyPoint(evt.localMousePosition);
            m_InputProvider.TouchFromMouse(position, phase);
        }
    }
}
