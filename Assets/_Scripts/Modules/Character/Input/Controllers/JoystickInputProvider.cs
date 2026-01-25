using Character.Input.Interfaces;
using Terresquall;
using UnityEngine;

namespace Modules.Character.Input.Controllers
{
    /// <summary>
    /// Reads input from Terresquall VirtualJoystick and exposes it via IInputProvider.
    /// This keeps the movement system input-agnostic.
    /// </summary>
    public class JoystickInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private VirtualJoystick joystick;
        private Vector2 lastAxis = Vector2.zero;
        private float deadzone = 0.1f;

        /// <summary>
        /// Returns the joystick axis mapped to world X/Z.
        /// </summary>
        public Vector3 GetInputDirection()
        {
            if (joystick == null)
                return Vector3.zero;
            Vector2 axis = joystick.GetAxis();
            return new Vector3(axis.x, 0f, axis.y);
        }

        private void Update()
        {
            if (joystick == null) return;

            Vector2 axis = joystick.GetAxis();
            float mag = Mathf.Clamp01(axis.magnitude);

            // Fire events when input changes or crosses deadzone boundaries
            if (mag > deadzone)
            {
                // move input
                Modules.EventSystem.Managers.EventManager.DelegateMoveInput(new Vector3(axis.x, 0f, axis.y), mag);

                if (lastAxis.magnitude <= deadzone)
                    Modules.EventSystem.Managers.EventManager.DelegateMoveStarted();
            }
            else
            {
                if (lastAxis.magnitude > deadzone)
                    Modules.EventSystem.Managers.EventManager.DelegateMoveEnded();
            }

            lastAxis = axis;
        }
    }
}