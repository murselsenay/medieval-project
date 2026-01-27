using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.Enemy.Controllers
{
    /// <summary>
    /// Enemy field-of-view detection component.
    /// - Scans periodically for targets inside a cone in front of the GameObject.
    /// - Performs a raycast to ensure line-of-sight (obstacle check).
    /// - Raises events when a target enters/exits view.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyVision : MonoBehaviour
    {
        [Header("FOV")]
        [SerializeField] private float _viewAngle = 45f;
        [Tooltip("Maximum view distance")]
        [SerializeField] private float _viewDistance = 10f;
        [SerializeField] private Transform _viewOrigin;
        [Header("Debug Draw")]
        [SerializeField] private bool _showFovMesh = true;
        // color is taken from assigned materials; do not expose here
        [SerializeField, Range(8, 128)] private int _fovSegments = 64;
        [SerializeField] private Material _fovMaterial;
        [SerializeField] private Material _fovFillMaterial;
        [SerializeField] private float _fillDuration = 1.5f;
        [SerializeField] private bool _debugStartFill = false;

        [Header("Detection")]
        [SerializeField] private LayerMask _targetMask = ~0;
        [SerializeField] private LayerMask _obstacleMask = 0;
        [SerializeField] private float _scanInterval = 0.15f;
        [Header("Target Filter")]
        [SerializeField] private bool _useTagFilter = true;
        [SerializeField] private string _targetTag = "Player";
        [Header("Instant Fill")]
        [Tooltip("If target is this close (world units) the fill completes instantly and triggers spotted.")]
        [SerializeField] private float _instantFillDistance = 1.5f;

        // last seen target (null if none)
        public Transform CurrentTarget { get; private set; }

        // events
        public event Action<Transform> OnTargetFound;
        public event Action OnTargetLost;
        // raised when fill animation completes (reaches 1.0)
        public event Action OnFillComplete;
        // raised when fill is fully cleared (reaches 0.0)
        public event Action OnFillCleared;

        private Coroutine _scanCoroutine;

        // runtime mesh for FOV visualization
        private GameObject _fovMeshObj;
        private MeshFilter _fovMeshFilter;
        private MeshRenderer _fovMeshRenderer;
        private GameObject _fovFillObj;
        private MeshFilter _fovFillFilter;
        private MeshRenderer _fovFillRenderer;
        private Coroutine _animateFillCoroutine;
        private bool _isFilling = false;
        private bool _debugFillRequestedPrev = false;
        private float _currentFill = 0f; // 0..1

        private void OnEnable()
        {
            StartScanning();
        }

        private void OnDisable()
        {
            StopScanning();
        }

        public void StartScanning()
        {
            if (_scanCoroutine != null) return;
            _scanCoroutine = StartCoroutine(ScanRoutine());
            if (_showFovMesh) CreateFovMesh();
            // subscribe handlers
            OnTargetFound += HandleTargetFound;
            OnTargetLost += HandleTargetLost;
        }

        public void StopScanning()
        {
            if (_scanCoroutine != null)
            {
                StopCoroutine(_scanCoroutine);
                _scanCoroutine = null;
            }
            if (CurrentTarget != null)
            {
                CurrentTarget = null;
                OnTargetLost?.Invoke();
            }
            if (_fovMeshObj != null) Destroy(_fovMeshObj);
            if (_fovFillObj != null) Destroy(_fovFillObj);

            // unsubscribe handlers
            OnTargetFound -= HandleTargetFound;
            OnTargetLost -= HandleTargetLost;
        }

        private void Update()
        {
            if (_debugStartFill != _debugFillRequestedPrev)
            {
                _debugFillRequestedPrev = _debugStartFill;
                if (_debugStartFill)
                    AnimateFillTo(1f);
                else
                    AnimateFillTo(0f);
            }
        }
        
        private void HandleTargetFound(Transform t)
        {
            if (_useTagFilter && t != null && !t.CompareTag(_targetTag)) return;
            // if target is very close, instantly fill and trigger spotted
            Transform origin = _viewOrigin != null ? _viewOrigin : transform;
            if (t != null && Vector3.Distance(origin.position, t.position) <= _instantFillDistance)
            {
                InstantFill();
                return;
            }
            // otherwise start animated fill when player enters
            AnimateFillTo(1f);
        }
        
        private void HandleTargetLost()
        {
            // reverse fill when player leaves
            AnimateFillTo(0f);
        }

        private IEnumerator ScanRoutine()
        {
            var wait = new WaitForSeconds(_scanInterval);
            while (true)
            {
                ScanOnce();
                if (_showFovMesh) UpdateFovMesh();
                yield return wait;
            }
        }

        public void StartFillAnimation()
        {
            if (_fovFillObj == null) CreateFovMesh();
            if (_isFilling) return;
            if (_fovFillRenderer != null && _fovFillRenderer.material != null)
            {
                if (_fovFillRenderer.material.HasProperty("_Fill"))
                    _fovFillRenderer.material.SetFloat("_Fill", 0f);
                if (_fovFillRenderer.material.HasProperty("_Radius"))
                    _fovFillRenderer.material.SetFloat("_Radius", _viewDistance);
                if (_fovFillRenderer.material.HasProperty("_HalfAngle"))
                    _fovFillRenderer.material.SetFloat("_HalfAngle", _viewAngle);
            }
            _fovFillObj.SetActive(true);
            AnimateFillTo(1f);
        }

        private void InstantFill()
        {
            if (_fovFillObj == null) CreateFovMesh();
            _fovFillObj.SetActive(true);
            _currentFill = 1f;
            if (_fovFillRenderer != null && _fovFillRenderer.material != null && _fovFillRenderer.material.HasProperty("_Fill"))
            {
                _fovFillRenderer.material.SetFloat("_Fill", 1f);
            }
            else
            {
                UpdateFillMesh(1f);
            }
            OnFillComplete?.Invoke();
        }

        public void StopFillAnimation()
        {
            AnimateFillTo(0f);
        }

        private IEnumerator AnimateFillCoroutineImpl(float target)
        {
            _isFilling = true;
            float start = _currentFill;
            float duration = Mathf.Max(0.01f, _fillDuration * Mathf.Abs(target - start));
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                _currentFill = Mathf.Lerp(start, target, p);
                // update shader or mesh
                if (_fovFillRenderer != null && _fovFillRenderer.material != null && _fovFillRenderer.material.HasProperty("_Fill"))
                {
                    _fovFillRenderer.material.SetFloat("_Fill", _currentFill);
                }
                else
                {
                    UpdateFillMesh(_currentFill);
                }
                yield return null;
            }
            _currentFill = target;
            if (_fovFillRenderer != null && _fovFillRenderer.material != null && _fovFillRenderer.material.HasProperty("_Fill"))
                _fovFillRenderer.material.SetFloat("_Fill", _currentFill);
            else
                UpdateFillMesh(_currentFill);

            // hide when target is 0
            if (Mathf.Approximately(_currentFill, 0f) && _fovFillObj != null)
                _fovFillObj.SetActive(false);

            // notify completion/cleared
            if (Mathf.Approximately(_currentFill, 1f)) OnFillComplete?.Invoke();
            if (Mathf.Approximately(_currentFill, 0f)) OnFillCleared?.Invoke();

            _isFilling = false;
            _animateFillCoroutine = null;
        }

        public void AnimateFillTo(float target)
        {
            target = Mathf.Clamp01(target);
            if (_animateFillCoroutine != null) StopCoroutine(_animateFillCoroutine);

            if (_fovFillObj == null) CreateFovMesh();
            if (_fovFillObj != null) _fovFillObj.SetActive(true);
            _animateFillCoroutine = StartCoroutine(AnimateFillCoroutineImpl(target));
        }

        private void UpdateFillMesh(float progress)
        {
            if (_fovFillFilter == null) return;
            int seg = Mathf.Clamp(_fovSegments, 3, 256);
            float half = _viewAngle;

            // radial fill: for each angle sample, set vertex at progress * viewDistance
            float radius = Mathf.Clamp01(progress) * _viewDistance;
            if (radius <= 0f)
            {
                _fovFillFilter.sharedMesh = new Mesh();
                if (_fovFillObj != null) _fovFillObj.SetActive(false);
                return;
            }

            Vector3[] verts = new Vector3[seg + 2]; // center + seg+1
            int[] tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / (float)seg;
                float angle = Mathf.Lerp(-half, half, t);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                verts[i + 1] = dir.normalized * radius;
            }

            int triIndex = 0;
            for (int i = 0; i < seg; i++)
            {
                tris[triIndex++] = 0;
                tris[triIndex++] = i + 1;
                tris[triIndex++] = i + 2;
            }

            Mesh m = new Mesh();
            m.name = "FOV_Fill_Mesh";
            m.vertices = verts;
            m.triangles = tris;
            m.RecalculateBounds();
            m.RecalculateNormals();
            _fovFillFilter.sharedMesh = m;
            if (_fovFillObj != null) _fovFillObj.SetActive(true);
        }

        private void ScanOnce()
        {
            // Improved detection: use collider.ClosestPoint, horizontal angle, and obstacle-only raycast.
            Transform origin = _viewOrigin != null ? _viewOrigin : transform;
            Collider[] hits = Physics.OverlapSphere(origin.position, _viewDistance, _targetMask, QueryTriggerInteraction.Ignore);

            Transform found = null;
            float bestDist = float.MaxValue;

            Vector3 forwardFlat = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
            if (forwardFlat.sqrMagnitude < 0.0001f) forwardFlat = origin.forward;

            foreach (var c in hits)
            {
                if (c == null) continue;

                // use closest point on collider to avoid center-offset issues
                Vector3 closest = c.ClosestPoint(origin.position);
                Vector3 dir = closest - origin.position;
                float dist = dir.magnitude;
                if (dist <= 0.001f) continue;

                // horizontal angle check to avoid vertical/tilt issues
                Vector3 dirFlat = Vector3.ProjectOnPlane(dir, Vector3.up);
                if (dirFlat.sqrMagnitude < 0.0001f)
                {
                    // target essentially above/below origin; still allow if within small angle
                    dirFlat = dir.normalized;
                }

                float angle = Vector3.Angle(forwardFlat.normalized, dirFlat.normalized);
                if (angle > _viewAngle) continue;

                // raycast only against obstacle mask to determine blocking
                bool blocked = false;
                RaycastHit hitInfo;
                if (Physics.Raycast(origin.position, dir.normalized, out hitInfo, dist - 0.01f, _obstacleMask))
                {
                    blocked = true;
                }
                if (blocked) continue;

                // optional tag filter handled by event handler; still choose nearest
                if (dist < bestDist)
                {
                    bestDist = dist;
                    found = c.transform;
                }
            }

            if (found != null)
            {
                if (CurrentTarget == null || CurrentTarget != found)
                {
                    CurrentTarget = found;
                    OnTargetFound?.Invoke(found);
                }
            }
            else
            {
                if (CurrentTarget != null)
                {
                    CurrentTarget = null;
                    OnTargetLost?.Invoke();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = _viewOrigin != null ? _viewOrigin : transform;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin.position, _viewDistance);

            // draw cone lines
            Vector3 leftDir = Quaternion.Euler(0, -_viewAngle, 0) * origin.forward;
            Vector3 rightDir = Quaternion.Euler(0, _viewAngle, 0) * origin.forward;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawLine(origin.position, origin.position + leftDir * _viewDistance);
            Gizmos.DrawLine(origin.position, origin.position + rightDir * _viewDistance);
        }

        private void CreateFovMesh()
        {
            if (_fovMeshObj != null) return;
            _fovMeshObj = new GameObject($"{name}_FOV");
            Transform origin = _viewOrigin != null ? _viewOrigin : transform;
            _fovMeshObj.transform.SetParent(origin, false);
            _fovMeshFilter = _fovMeshObj.AddComponent<MeshFilter>();
            _fovMeshRenderer = _fovMeshObj.AddComponent<MeshRenderer>();
            _fovMeshRenderer.sharedMaterial = _fovMaterial;

            UpdateFovMesh();

            _fovFillObj = new GameObject($"{name}_FOV_Fill");
            _fovFillObj.transform.SetParent(origin, false);
            _fovFillFilter = _fovFillObj.AddComponent<MeshFilter>();
            _fovFillRenderer = _fovFillObj.AddComponent<MeshRenderer>();

            // ensure fill starts hidden; render on top of base FOV to avoid z-fighting
            _fovFillObj.transform.localPosition = new Vector3(0, 0.01f, 0.01f);

            // create a safe instance material for the fill that is unlit (prevents lighting-based fading when rotating)
            if (_fovFillMaterial != null)
            {
                // prefer an unlit instance so lighting/rotation doesn't change appearance
                string sname = _fovFillMaterial.shader != null ? _fovFillMaterial.shader.name : string.Empty;
                if (sname.IndexOf("Unlit", StringComparison.OrdinalIgnoreCase) >= 0 || sname.IndexOf("FOVFill", StringComparison.OrdinalIgnoreCase) >= 0 || sname.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _fovFillRenderer.material = new Material(_fovFillMaterial);
                }
                else
                {
                    // copy color if available, but use Unlit shader to avoid shading changes while rotating
                    Color srcCol = Color.white;
                    try { if (_fovFillMaterial.HasProperty("_Color")) srcCol = _fovFillMaterial.GetColor("_Color"); } catch { }
                    var safe = new Material(Shader.Find("Unlit/Color")) ?? new Material(Shader.Find("Sprites/Default"));
                    safe.SetColor("_Color", srcCol);
                    _fovFillRenderer.material = safe;
                }
            }
            else
            {
                var fillMat = new Material(Shader.Find("Unlit/Color")) ?? new Material(Shader.Find("Sprites/Default"));
                _fovFillRenderer.material = fillMat;
            }

            // renderer settings to avoid lighting/shadow/probe interactions that cause apparent fading
            _fovFillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _fovFillRenderer.receiveShadows = false;
            _fovFillRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _fovFillRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            // ensure fill starts with empty geometry
            if (_fovFillFilter != null)
                _fovFillFilter.sharedMesh = new Mesh();

            // render on top of base FOV to avoid z-fighting
            if (_fovMeshRenderer != null && _fovMeshRenderer.sharedMaterial != null)
                _fovFillRenderer.material.renderQueue = Mathf.Max(3100, _fovMeshRenderer.sharedMaterial.renderQueue + 1);

            _fovFillObj.SetActive(false);
        }

        private void UpdateFovMesh()
        {
            if (!_showFovMesh || _fovMeshFilter == null) return;

            int seg = Mathf.Clamp(_fovSegments, 3, 256);
            int vertsCount = seg + 2; // center + seg+1
            Vector3[] verts = new Vector3[vertsCount];
            int[] tris = new int[(seg) * 3];

            verts[0] = Vector3.zero;
            float half = _viewAngle;
            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / (float)seg;
                float angle = Mathf.Lerp(-half, half, t);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                verts[i + 1] = dir.normalized * _viewDistance;
            }

            int triIndex = 0;
            for (int i = 0; i < seg; i++)
            {
                tris[triIndex++] = 0;
                tris[triIndex++] = i + 1;
                tris[triIndex++] = i + 2;
            }

            Mesh m = new Mesh();
            m.name = "FOV_Mesh";
            m.vertices = verts;
            m.triangles = tris;
            m.RecalculateBounds();
            m.RecalculateNormals();
            _fovMeshFilter.sharedMesh = m;
        }
    }
}
