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
        [Tooltip("Half-angle of the view cone in degrees (e.g. 45 = 90deg cone)")]
        [SerializeField] private float _viewAngle = 45f;
        [Tooltip("Maximum view distance")]
        [SerializeField] private float _viewDistance = 10f;
        [Tooltip("Optional transform to use as origin and forward direction for the FOV. If null, this GameObject's transform is used.")]
        [SerializeField] private Transform _viewOrigin;
        [Header("Debug Draw")]
        [SerializeField] private bool _showFovMesh = true;
        // color is taken from assigned materials; do not expose here
        [SerializeField, Range(8, 128)] private int _fovSegments = 64;
        [Tooltip("Optional material to render the FOV mesh. Assign in inspector. If null, a fallback material will be created.")]
        [SerializeField] private Material _fovMaterial;
        [Tooltip("Material used for the fill overlay (animated). Assign in inspector or leave null for fallback.)")]
        [SerializeField] private Material _fovFillMaterial;
        [SerializeField] private float _fillDuration = 1.5f;
        [Tooltip("Inspector toggle for testing: when true, starts fill animation")]
        [SerializeField] private bool _debugStartFill = false;

        [Header("Detection")]
        [Tooltip("Which layers contain potential targets (e.g. Player)")]
        [SerializeField] private LayerMask _targetMask = ~0;
        [Tooltip("Which layers block vision (walls, obstacles)")]
        [SerializeField] private LayerMask _obstacleMask = 0;
        [Tooltip("How often (seconds) to run the detection scan")]
        [SerializeField] private float _scanInterval = 0.15f;

        // last seen target (null if none)
        public Transform CurrentTarget { get; private set; }

        // events
        public event Action<Transform> OnTargetFound;
        public event Action OnTargetLost;

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
            // debug toggle handled in Update() to animate in/out
        }

        // colors are taken from assigned materials; no inspector color fields

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
        }

        private void Update()
        {
            // inspector toggle handling for testing: start/stop fill when changed
            if (_debugStartFill != _debugFillRequestedPrev)
            {
                _debugFillRequestedPrev = _debugStartFill;
                if (_debugStartFill)
                    AnimateFillTo(1f);
                else
                    AnimateFillTo(0f);
            }
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
            // ensure fill material visible (we drive geometry, not alpha)
            if (_fovFillRenderer != null && _fovFillRenderer.material != null)
            {
                // material color should come from the assigned material; ensure it's configured
                ConfigureMaterialForTransparency(_fovFillRenderer.material);
                // if shader supports _Fill, set initial to 0
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

            _isFilling = false;
            _animateFillCoroutine = null;
        }

        public void AnimateFillTo(float target)
        {
            target = Mathf.Clamp01(target);
            if (_animateFillCoroutine != null) StopCoroutine(_animateFillCoroutine);
            // ensure mesh exists
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

        // removed unused helper methods to keep script minimal

        private void ScanOnce()
        {
            // find colliders in sphere
            Transform origin = _viewOrigin != null ? _viewOrigin : transform;
            Collider[] hits = Physics.OverlapSphere(origin.position, _viewDistance, _targetMask, QueryTriggerInteraction.Ignore);

            Transform found = null;
            float bestDist = float.MaxValue;

            foreach (var c in hits)
            {
                if (c == null) continue;
                Transform t = c.transform;
                Vector3 dir = (t.position - origin.position);
                float dist = dir.magnitude;
                if (dist <= 0.001f) continue;

                // angle check
                float angle = Vector3.Angle(origin.forward, dir.normalized);
                if (angle > _viewAngle) continue;

                // line of sight
                if (Physics.Raycast(origin.position, dir.normalized, out RaycastHit hit, _viewDistance, _obstacleMask | _targetMask))
                {
                    // if raycast hits an obstacle before the target, ignore
                    if (((1 << hit.collider.gameObject.layer) & _targetMask) == 0)
                    {
                        // hit something that's not the target layer -> blocked
                        continue;
                    }
                }

                // choose nearest valid target
                if (dist < bestDist)
                {
                    bestDist = dist;
                    found = t;
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
            if (_fovMaterial != null)
            {
                _fovMeshRenderer.sharedMaterial = _fovMaterial;
            }
            else
            {
                Debug.LogWarning($"EnemyVision on '{name}': no FOV material assigned, creating fallback material.");
                var mat = new Material(Shader.Find("Unlit/Color"));
                if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
                _fovMeshRenderer.sharedMaterial = mat;
            }
            UpdateFovMesh();
            // create fill overlay object (same mesh) for animated filling
            _fovFillObj = new GameObject($"{name}_FOV_Fill");
            _fovFillObj.transform.SetParent(origin, false);
            _fovFillFilter = _fovFillObj.AddComponent<MeshFilter>();
            _fovFillRenderer = _fovFillObj.AddComponent<MeshRenderer>();
            // assign an instance material so we can animate color without modifying shared asset
            // ensure fill starts hidden; render on top of base FOV to avoid z-fighting
            _fovFillObj.transform.localPosition = new Vector3(0, 0.01f, 0.01f);
            if (_fovMeshRenderer != null && _fovMeshRenderer.sharedMaterial != null)
                _fovFillRenderer.material.renderQueue = _fovMeshRenderer.sharedMaterial.renderQueue + 1;
            if (_fovFillMaterial != null)
            {
                _fovFillRenderer.material = new Material(_fovFillMaterial);
                ConfigureMaterialForTransparency(_fovFillRenderer.material);
            }
            else
            {
                var fillMat = new Material(Shader.Find("Unlit/Color")) ?? new Material(Shader.Find("Sprites/Default"));
                _fovFillRenderer.material = fillMat;
                ConfigureMaterialForTransparency(_fovFillRenderer.material);
            }
            // initialize fill mesh as empty (so fill is not visible until animated)
            if (_fovFillFilter != null)
                _fovFillFilter.sharedMesh = new Mesh();
            // initially hide fill
            _fovFillObj.SetActive(false);
        }

        private void ConfigureMaterialForTransparency(Material mat)
        {
            if (mat == null) return;
            string sname = mat.shader != null ? mat.shader.name : string.Empty;
            // try to support Standard shader transparency
            if (sname.Contains("Standard"))
            {
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
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
