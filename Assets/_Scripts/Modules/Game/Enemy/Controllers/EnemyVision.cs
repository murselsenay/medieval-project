using System;
using System.Collections;
using System.Collections.Generic;
using Modules.Game.Constants;
using UnityEngine;

namespace Modules.Game.Enemy.Controllers
{
    [DisallowMultipleComponent]
    public class EnemyVision : MonoBehaviour
    {
        [BHeader("FOV")]
        [SerializeField] private float _viewAngle = 45f;
        [Tooltip("Maximum view distance")]
        [SerializeField] private float _viewDistance = 10f;
        [SerializeField] private Transform _viewOrigin;
        [BHeader("Debug Draw")]
        [SerializeField] private bool _showFovMesh = true;
        [SerializeField, Range(8, 128)] private int _fovSegments = 64;
        [SerializeField] private Material _fovMaterial;
        [SerializeField] private Material _fovFillMaterial;
        [SerializeField] private float _fillDuration = 1.5f;
        [SerializeField] private bool _debugStartFill = false;

        [BHeader("Detection")]
        [SerializeField] private LayerMask _targetMask = ~0;
        [SerializeField] private LayerMask _obstacleMask = 0;
        [SerializeField] private float _scanInterval = 0.15f;
        [BHeader("Target Filter")]
        [SerializeField] private bool _useTagFilter = true;
        [BHeader("Instant Fill")]
        [Tooltip("If target is this close (world units) the fill completes instantly and triggers spotted.")]
        [SerializeField] private float _instantFillDistance = 1.5f;

        public Transform CurrentTarget { get; private set; }

        // events
        public event Action<Transform> OnTargetFound;
        public event Action OnTargetLost;
        public event Action OnFillComplete;
        public event Action OnFillCleared;

        private Coroutine _scanCoroutine;


        //FOV Detection Refs
        private GameObject _fovMeshObj;
        private MeshFilter _fovMeshFilter;
        private MeshFilter _fovFillFilter;
        private GameObject _fovFillObj;
        private MeshRenderer _fovMeshRenderer;
        private MeshRenderer _fovFillRenderer;

        private Coroutine _animateFillCoroutine;
        
        private bool _isFilling = false;
        private float _currentFill = 0f;
        private bool _debugFillRequestedPrev = false;

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
            if (_useTagFilter && t != null && !t.CompareTag(CollisionTags.Player)) return;
            Transform origin = _viewOrigin != null ? _viewOrigin : transform;
            if (t != null && Vector3.Distance(origin.position, t.position) <= _instantFillDistance)
            {
                InstantFill();
                return;
            }
            AnimateFillTo(1f);
        }
        
        private void HandleTargetLost()
        {
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

            if (Mathf.Approximately(_currentFill, 0f) && _fovFillObj != null)
                _fovFillObj.SetActive(false);

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

            float radius = Mathf.Clamp01(progress) * _viewDistance;
            if (radius <= 0f)
            {
                _fovFillFilter.sharedMesh = new Mesh();
                if (_fovFillObj != null) _fovFillObj.SetActive(false);
                return;
            }

            Vector3[] verts = new Vector3[seg + 2];
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
            Transform origin = _viewOrigin != null ? _viewOrigin : transform;
            Collider[] hits = Physics.OverlapSphere(origin.position, _viewDistance, _targetMask, QueryTriggerInteraction.Ignore);

            Transform found = null;
            float bestDist = float.MaxValue;

            Vector3 forwardFlat = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
            if (forwardFlat.sqrMagnitude < 0.0001f) forwardFlat = origin.forward;

            foreach (var c in hits)
            {
                if (c == null) continue;

                Vector3 closest = c.ClosestPoint(origin.position);
                Vector3 dir = closest - origin.position;
                float dist = dir.magnitude;
                if (dist <= 0.001f) continue;

                Vector3 dirFlat = Vector3.ProjectOnPlane(dir, Vector3.up);
                if (dirFlat.sqrMagnitude < 0.0001f)
                {
                    dirFlat = dir.normalized;
                }

                float angle = Vector3.Angle(forwardFlat.normalized, dirFlat.normalized);
                if (angle > _viewAngle) continue;

                bool blocked = false;
                RaycastHit hitInfo;
                if (Physics.Raycast(origin.position, dir.normalized, out hitInfo, dist - 0.01f, _obstacleMask))
                {
                    blocked = true;
                }
                if (blocked) continue;

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

            _fovFillObj.transform.localPosition = new Vector3(0, 0.01f, 0.01f);

            if (_fovFillMaterial != null)
            {
                string sname = _fovFillMaterial.shader != null ? _fovFillMaterial.shader.name : string.Empty;
                if (sname.IndexOf("Unlit", StringComparison.OrdinalIgnoreCase) >= 0 || sname.IndexOf("FOVFill", StringComparison.OrdinalIgnoreCase) >= 0 || sname.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _fovFillRenderer.material = new Material(_fovFillMaterial);
                }
                else
                {
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

            _fovFillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _fovFillRenderer.receiveShadows = false;
            _fovFillRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _fovFillRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            if (_fovFillFilter != null)
                _fovFillFilter.sharedMesh = new Mesh();

            if (_fovMeshRenderer != null && _fovMeshRenderer.sharedMaterial != null)
                _fovFillRenderer.material.renderQueue = Mathf.Max(3100, _fovMeshRenderer.sharedMaterial.renderQueue + 1);

            _fovFillObj.SetActive(false);
        }

        private void UpdateFovMesh()
        {
            if (!_showFovMesh || _fovMeshFilter == null) return;

            int seg = Mathf.Clamp(_fovSegments, 3, 256);
            int vertsCount = seg + 2;
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
