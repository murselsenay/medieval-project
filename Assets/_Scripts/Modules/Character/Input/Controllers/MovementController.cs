using Character.Input.Interfaces;
using Modules.Logger;
using Modules.EventSystem.Managers;
using UnityEngine;

namespace Modules.Character.Input.Controllers
{
    [RequireComponent(typeof(CharacterController))]
    public class MovementController : MonoBehaviour
    {
        [BHeader("References")]
        [SerializeField] private CharacterController _controller;

        [BHeader("Movement")]
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _turnSmoothTime = 0.1f;

        [BHeader("Gravity")]
        [SerializeField] private float _gravityValue = -9.81f;
        [SerializeField] private float _groundedY = -0.5f;
        [SerializeField] private bool _scaleSpeedByInput = true;
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask _groundLayers = ~0;

        [BHeader("Animations")]
        [SerializeField] private Animator _animator;

        private float _turnSmoothVelocity;
        private Vector3 _playerVelocity;
        private IInputProvider _fallbackInputProvider;

        // event-driven input state
        private Vector3 _inputDirection = Vector3.zero;
        private float _inputMagnitude = 0f;
        private bool _moveActive = false;

        private void Awake()
        {
            if (_controller == null)
                _controller = GetComponent<CharacterController>();

            if (_controller == null)
            {
                DebugLogger.LogError($"CharacterController not found on '{name}'. MovementController will be inactive.");
            }
            MonoBehaviour[] mbs = GetComponents<MonoBehaviour>();
            foreach (var mb in mbs)
            {
                if (mb is IInputProvider provider)
                {
                    _fallbackInputProvider = provider;
                    break;
                }
            }

        }
        private void OnEnable()
        {
            EventManager.OnMoveInput += HandleMoveInput;
            EventManager.OnMoveStarted += HandleMoveStarted;
            EventManager.OnMoveEnded += HandleMoveEnded;
        }

        private void OnDisable()
        {
            EventManager.OnMoveInput -= HandleMoveInput;
            EventManager.OnMoveStarted -= HandleMoveStarted;
            EventManager.OnMoveEnded -= HandleMoveEnded;
        }

        private void HandleMoveInput(Vector3 direction, float magnitude)
        {
            _inputDirection = new Vector3(direction.x, 0f, direction.z);
            _inputMagnitude = Mathf.Clamp01(magnitude);
        }

        private void HandleMoveStarted()
        {
            _moveActive = true;
        }

        private void HandleMoveEnded()
        {
            _moveActive = false;
            _inputMagnitude = 0f;
            _inputDirection = Vector3.zero;
        }

        // Auto-fit coroutine removed.

        private void OnValidate()
        {
            // Clamp values edited in inspector to safe ranges.
            _groundedY = -Mathf.Abs(_groundedY == 0f ? -0.5f : _groundedY);
            if (_gravityValue > 0f) _gravityValue = -Mathf.Abs(_gravityValue == 0f ? 9.81f : _gravityValue);
            if (_speed < 0f) _speed = 0f;
            if (_turnSmoothTime < 0f) _turnSmoothTime = 0f;
        }

        // Automatic fitting removed. Configure CharacterController manually in the Inspector to match the visual mesh.

        private void Update()
        {
            // core movement update

            // Prefer event-driven input; fall back to polling provider if any
            Vector3 rawInput = Vector3.zero;
            if (_inputDirection != Vector3.zero && _inputMagnitude > 0f)
            {
                rawInput = _inputDirection * _inputMagnitude;
            }
            else if (_fallbackInputProvider != null)
            {
                rawInput = _fallbackInputProvider.GetInputDirection();
            }

            // Movement in world X/Z plane for a top-down camera
            Vector3 inputDir = new Vector3(rawInput.x, 0f, rawInput.z);
            float inputMag = Mathf.Clamp01(inputDir.magnitude);

            // Rotation: only when there is significant input
            if (inputMag >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            // Horizontal movement scaled by input magnitude if requested
            Vector3 horizontalVelocity = Vector3.zero;
            if (inputMag >= 0.01f)
            {
                if (_scaleSpeedByInput)
                    horizontalVelocity = inputDir.normalized * _speed * inputMag;
                else
                    horizontalVelocity = inputDir.normalized * _speed;
            }

            bool isGrounded = _controller.isGrounded;
            if (_controller != null)
            {
                Vector3 controllerBottom = transform.TransformPoint(_controller.center - Vector3.up * (_controller.height * 0.5f - _controller.skinWidth));
                Ray ray = new Ray(controllerBottom + Vector3.up * 0.05f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, _groundCheckDistance + 0.05f, _groundLayers, QueryTriggerInteraction.Ignore))
                {
                    isGrounded = true;
                }
            }

            // Gravity handling: when grounded and moving downward, set a small negative velocity so the controller stays snapped to ground.
            if (isGrounded && _playerVelocity.y < 0f)
            {
                _playerVelocity.y = _groundedY;
            }

            _playerVelocity.y += _gravityValue * Time.deltaTime;

            // Combine horizontal and vertical movement into a single move so collisions/grounding are consistent
            Vector3 move = horizontalVelocity * Time.deltaTime + _playerVelocity * Time.deltaTime;
            _controller.Move(move);

            _animator.SetFloat("Speed", inputMag, 0.1f, Time.deltaTime);

            // end of Update
        }

        private void OnDrawGizmosSelected()
        {
            // Draw CharacterController capsule for debugging offset issues
            if (_controller != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 centerWorld = transform.TransformPoint(_controller.center);
                float halfHeight = Mathf.Max(0f, (_controller.height * 0.5f) - _controller.radius);
                Vector3 top = centerWorld + Vector3.up * halfHeight;
                Vector3 bottom = centerWorld - Vector3.up * halfHeight;

                Gizmos.DrawWireSphere(top, _controller.radius);
                Gizmos.DrawWireSphere(bottom, _controller.radius);
                Gizmos.DrawLine(top + transform.right * _controller.radius, bottom + transform.right * _controller.radius);
                Gizmos.DrawLine(top - transform.right * _controller.radius, bottom - transform.right * _controller.radius);
                Gizmos.DrawLine(top + transform.forward * _controller.radius, bottom + transform.forward * _controller.radius);
                Gizmos.DrawLine(top - transform.forward * _controller.radius, bottom - transform.forward * _controller.radius);
            }
        }

        // Diagnostics and auto-snap helpers removed - keep MovementController minimal.
    }
}

