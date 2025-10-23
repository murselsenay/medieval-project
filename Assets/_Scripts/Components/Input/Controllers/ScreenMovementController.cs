using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Components.Input.Controllers
{
    public class ScreenMovementController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _panSpeed = 1f;
        [SerializeField] private float _smoothTime = 0.12f;

        [Header("Bounds")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private float _minX = -10f;
        [SerializeField] private float _maxX = 10f;
        [SerializeField] private float _minZ = -10f;
        [SerializeField] private float _maxZ = 10f;

        [Header("Inertia")]
        [SerializeField] private bool _useInertia = true;
        [SerializeField] private float _inertiaDamping = 2.5f;
        [SerializeField] private float _inertiaThreshold = 0.01f;

        [Header("Input")]
        [SerializeField] private float _dragDeadzone = 0.02f;
        [SerializeField, Range(0.01f, 1f)] private float _dragLerp = 1f;
        [SerializeField] private float _maxDelta = 10f;

        [SerializeField] private bool _active = true; // Hareket aktif mi?

        private bool _isPanning;

        private Vector3 _lastScreenPos;
        private Vector3 _inertiaVelocity = Vector3.zero;
        private Vector3 _lastAppliedMovement = Vector3.zero;
        private Vector3 _inertiaSmoothVel = Vector3.zero;

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        public bool Active
        {
            get => _active;
            set => _active = value;
        }

        private void Update()
        {
            // Awake ve ilk kurulum her zaman çalýþmalý, sadece input ve inertia iþlemleri _active ile kontrol edilmeli
            if (_active)
            {
                HandleMouse();
                HandleTouch();

                if (!_isPanning && _useInertia && _inertiaVelocity.sqrMagnitude > _inertiaThreshold * _inertiaThreshold)
                {
                    ApplyInertia();
                }
            }
            EnforceBoundsImmediate();
        }

        private void HandleMouse()
        {
            if (_camera == null) return;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                StartPan(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButton(0) && _isPanning)
            {
                ContinuePan(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                EndPan();
            }
        }

        private void HandleTouch()
        {
            if (_camera == null) return;

            if (UnityEngine.Input.touchCount == 1)
            {
                var touch = UnityEngine.Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    StartPan(touch.position);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (_isPanning)
                        ContinuePan(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    EndPan();
                }
            }
        }

        private void StartPan(Vector3 screenPos)
        {
            _isPanning = true;
            _lastScreenPos = screenPos;
            _inertiaVelocity = Vector3.zero;
            _lastAppliedMovement = Vector3.zero;
            _inertiaSmoothVel = Vector3.zero;
        }

        private void ContinuePan(Vector3 screenPos)
        {
            if (_camera == null) return;

            float planeY = _camera.transform.position.y;
            Vector3 currentWorld = ScreenToWorldPointOnPlane(screenPos, planeY);
            Vector3 lastWorld = ScreenToWorldPointOnPlane(_lastScreenPos, planeY);
            Vector3 delta = currentWorld - lastWorld;

            if (delta.magnitude > _maxDelta)
            {
                _lastScreenPos = screenPos;
                return;
            }

            if (delta.sqrMagnitude < _dragDeadzone * _dragDeadzone)
            {
                _lastScreenPos = screenPos;
                return;
            }

            Vector3 prevPos = _camera.transform.position;
            Vector3 expectedMove = -delta * _panSpeed;
            Vector3 desiredPos = prevPos + expectedMove;
            desiredPos.y = prevPos.y;

            if (_useBounds)
            {
                desiredPos.x = Mathf.Clamp(desiredPos.x, _minX, _maxX);
                desiredPos.z = Mathf.Clamp(desiredPos.z, _minZ, _maxZ);
            }

            Vector3 targetPos = Vector3.Lerp(prevPos, desiredPos, _dragLerp);

            if (_useBounds)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, _minX, _maxX);
                targetPos.z = Mathf.Clamp(targetPos.z, _minZ, _maxZ);
            }

            Vector3 actualMoved = targetPos - prevPos;
            _camera.transform.position = targetPos;

            if (Time.deltaTime > 0f)
            {
                Vector3 instantaneousVel = actualMoved / Time.deltaTime;
                _inertiaVelocity = Vector3.Lerp(_inertiaVelocity, instantaneousVel, 0.5f);

                if (_lastAppliedMovement.sqrMagnitude > 0.0001f)
                {
                    if (!Mathf.Approximately(_lastAppliedMovement.x, 0f) && Mathf.Sign(instantaneousVel.x) != Mathf.Sign(_lastAppliedMovement.x))
                        _inertiaVelocity.x *= 0.25f;
                    if (!Mathf.Approximately(_lastAppliedMovement.z, 0f) && Mathf.Sign(instantaneousVel.z) != Mathf.Sign(_lastAppliedMovement.z))
                        _inertiaVelocity.z *= 0.25f;
                }
            }

            if (_useBounds)
            {
                if (targetPos.x <= _minX + Mathf.Epsilon || targetPos.x >= _maxX - Mathf.Epsilon)
                    _inertiaVelocity.x = 0f;
                if (targetPos.z <= _minZ + Mathf.Epsilon || targetPos.z >= _maxZ - Mathf.Epsilon)
                    _inertiaVelocity.z = 0f;
            }

            _lastAppliedMovement = actualMoved;

            _lastScreenPos = screenPos;
        }

        private void EndPan()
        {
            _isPanning = false;
            if (!_useInertia)
                _inertiaVelocity = Vector3.zero;
        }

        private void ApplyInertia()
        {
            if (_camera == null) return;

            float decay = Mathf.Exp(-_inertiaDamping * Time.deltaTime);
            _inertiaVelocity *= decay;

            Vector3 prevPos = _camera.transform.position;
            Vector3 desiredPos = prevPos + _inertiaVelocity * Time.deltaTime;
            desiredPos.y = prevPos.y;

            if (_useBounds)
            {
                desiredPos.x = Mathf.Clamp(desiredPos.x, _minX, _maxX);
                desiredPos.z = Mathf.Clamp(desiredPos.z, _minZ, _maxZ);
            }

            _camera.transform.position = Vector3.SmoothDamp(prevPos, desiredPos, ref _inertiaSmoothVel, _smoothTime);

            if (_useBounds)
            {
                if (_camera.transform.position.x <= _minX + Mathf.Epsilon || _camera.transform.position.x >= _maxX - Mathf.Epsilon)
                    _inertiaVelocity.x = 0f;
                if (_camera.transform.position.z <= _minZ + Mathf.Epsilon || _camera.transform.position.z >= _maxZ - Mathf.Epsilon)
                    _inertiaVelocity.z = 0f;
            }

            float planeY = _camera.transform.position.y;
            Vector3 groundPoint = new Vector3(_camera.transform.position.x, planeY, _camera.transform.position.z);
            Vector3 screenForGround = _camera.WorldToScreenPoint(groundPoint);
            _lastScreenPos = new Vector3(screenForGround.x, screenForGround.y, screenForGround.z);

            if (_inertiaVelocity.sqrMagnitude <= _inertiaThreshold * _inertiaThreshold)
                _inertiaVelocity = Vector3.zero;
        }

        private void EnforceBoundsImmediate()
        {
            if (_camera == null || !_useBounds) return;

            Vector3 p = _camera.transform.position;
            float clampedX = Mathf.Clamp(p.x, _minX, _maxX);
            float clampedZ = Mathf.Clamp(p.z, _minZ, _maxZ);
            if (!Mathf.Approximately(p.x, clampedX) || !Mathf.Approximately(p.z, clampedZ))
            {
                p.x = clampedX;
                p.z = clampedZ;
                _camera.transform.position = p;

                if (!Mathf.Approximately(p.x, clampedX))
                    _inertiaVelocity.x = 0f;
                if (!Mathf.Approximately(p.z, clampedZ))
                    _inertiaVelocity.z = 0f;

                _lastScreenPos = UnityEngine.Input.mousePosition;
            }
        }

        private Vector3 ScreenToWorldPointOnPlane(Vector3 screenPos, float planeY)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return _camera.transform.position;
        }
    }
}
