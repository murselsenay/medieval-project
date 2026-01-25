using UnityEngine;
namespace Character.Input.Interfaces
{
    /// <summary>
    /// Abstract input provider interface. Implement this to supply movement input from
    /// any source (keyboard, gamepad, joystick, AI, etc.).
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Returns a direction vector where X and Z represent horizontal movement in world space.
        /// Y should be 0; gravity is handled by the movement system.
        /// </summary>
        Vector3 GetInputDirection();
    }
}