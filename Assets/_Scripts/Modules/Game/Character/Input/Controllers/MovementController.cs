using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace Modules.Game.Character.Input.Controllers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent _agent;

        [Header("Movement")]
        [SerializeField] private float _rotationSmoothTime = 0.12f;
        [SerializeField] private float _stopThreshold = 0.1f;
        [SerializeField] private LayerMask _clickableLayers = ~0;
        [SerializeField] private float _walkSpeed = 0.8f;
        [SerializeField] private float _runSpeed = 3.5f;
        [SerializeField] private float _holdToRunThreshold = 0.25f;

        private Coroutine _holdCoroutine;

        [Header("Animation")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _animSpeedParam = "Speed";

        private float _rotationVelocity;
        private Vector3 _currentDestination;
        private bool _hasDestination;
        
        [Header("Path Visual")]
        [SerializeField] private bool _showPath = true;
        [SerializeField] private LineRenderer _pathLine;
        [SerializeField] private Color _pathColor = Color.cyan;
        [SerializeField] private float _pathWidth = 0.12f;
        [SerializeField] private bool _overrideLineWidth = false;
        [SerializeField] private float _pathYOffset = 0.05f;
        [SerializeField] private bool _useDashTexture = false;

        private Texture2D _dashTexture;
        [SerializeField] private float _dashTextureScale = 1f;
        [SerializeField] private float _pathFollowInterval = 0.12f;
        [SerializeField] private float _pathTrimThreshold = 0.5f;
        
        [Header("Target Visual")]
        [SerializeField] private GameObject _targetMarker;
        [SerializeField] private float _targetYOffset = 0.05f;
        [SerializeField] private float _targetPulseMin = 0.8f;
        [SerializeField] private float _targetPulseMax = 1.15f;
        [SerializeField] private float _targetPulseSpeed = 3f;
        [SerializeField] private float _targetPulseRunMultiplier = 1.6f;
        [SerializeField] private float _targetPulseWalkMultiplier = 1f;

        private GameObject _activeTarget;
        private Coroutine _targetPulseCoroutine;
        private bool _isRunning = false;

        private Vector3[] _pathPositions = new Vector3[0];
        private Coroutine _pathFollowCoroutine;

        private void Awake() { }

        private void Update()
        {
            HandleClickInput();
            UpdateRotation();
            UpdateAnimator();
        }

        private void HandleClickInput()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, _clickableLayers, QueryTriggerInteraction.Ignore))
                {
                        if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
                        {
                            
                            _currentDestination = navHit.position;

                            
                            if (_showPath)
                            {
                                UpdatePathImmediate(_currentDestination);
                            }

                            _agent.SetDestination(_currentDestination);
                            _hasDestination = true;

                            
                            SetAgentSpeed(_walkSpeed);

                            
                            if (_holdCoroutine != null) StopCoroutine(_holdCoroutine);
                            _holdCoroutine = StartCoroutine(HoldToRunCoroutine());

                            
                            if (_showPath)
                            {
                                UpdatePathImmediate(_currentDestination);
                                if (_pathFollowCoroutine != null) StopCoroutine(_pathFollowCoroutine);
                                _pathFollowCoroutine = StartCoroutine(PathFollowCoroutine());
                            }

                            
                            SpawnOrMoveTarget(_currentDestination);
                        }
                }
            }

            
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (_holdCoroutine != null)
                {
                    StopCoroutine(_holdCoroutine);
                    _holdCoroutine = null;
                }
                SetAgentSpeed(_walkSpeed);
                _isRunning = false;
            }
        }

        private IEnumerator HoldToRunCoroutine()
        {
            float t = 0f;
            while (UnityEngine.Input.GetMouseButton(0))
            {
                t += Time.deltaTime;
                if (t >= _holdToRunThreshold)
                {
                    SetAgentSpeed(_runSpeed);
                    _isRunning = true;
                    yield break;
                }
                yield return null;
            }
            
            SetAgentSpeed(_walkSpeed);
            _isRunning = false;
        }

        private void SetAgentSpeed(float speed)
        {
            _agent.speed = speed;
            _isRunning = speed > (_walkSpeed + 0.01f);
        }

        private void UpdateRotation()
        {
            Vector3 velocity = _agent.velocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                float targetAngle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
            else if (_hasDestination && !_agent.pathPending && _agent.remainingDistance <= _stopThreshold)
            {
                _hasDestination = false;
            }
        }

        private void UpdateAnimator()
        {
            float speed = _agent.velocity.magnitude;
            _animator.SetFloat(_animSpeedParam, speed);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_agent.transform.position, 0.1f);
            if (_hasDestination)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_currentDestination, 0.15f);
            }
        }

        private void DrawPath(Vector3[] corners)
        {
            if (!_showPath) return;

            if (corners == null || corners.Length < 2)
            {
                ClearPathVisual();
                return;
            }

            if (_pathLine == null) return;

            if (_pathPositions == null || _pathPositions.Length != corners.Length)
                _pathPositions = new Vector3[corners.Length];

            Vector3 agentPos = _agent != null ? _agent.transform.position : Vector3.zero;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 p = corners[i];
                if (i == 0)
                {
                    p = agentPos;
                }
                p.y += _pathYOffset;
                _pathPositions[i] = p;
            }

            if (_pathLine == null) return;
            _pathLine.positionCount = _pathPositions.Length;
            _pathLine.SetPositions(_pathPositions);
            if (_overrideLineWidth)
            {
                _pathLine.startWidth = _pathWidth;
                _pathLine.endWidth = _pathWidth;
            }
            _pathLine.startColor = _pathColor;
            _pathLine.endColor = _pathColor;
            if (_useDashTexture && _dashTexture != null)
            {
                var mat = _pathLine.sharedMaterial;
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Unlit/Transparent"));
                    _pathLine.sharedMaterial = mat;
                }
                mat.mainTexture = _dashTexture;
                _pathLine.textureMode = LineTextureMode.Tile;
                mat.mainTextureScale = new Vector2(_dashTextureScale, 1f);
            }
            _pathLine.enabled = true;
        }

        private void UpdatePathImmediate(Vector3 destination)
        {
            NavMeshPath calcPath = new NavMeshPath();
            bool ok = NavMesh.CalculatePath(_agent.transform.position, destination, NavMesh.AllAreas, calcPath);
            if (ok && calcPath.corners != null && calcPath.corners.Length >= 2)
            {
                float total = CalculatePathLength(calcPath.corners);
                SetupPathLineAppearance(total);
                DrawPath(calcPath.corners);
            }
            else
            {
                if (_pathLine == null) return;
                _pathPositions = new Vector3[2];
                Vector3 a = _agent.transform.position; a.y += _pathYOffset;
                Vector3 b = destination; b.y += _pathYOffset;
                _pathPositions[0] = a; _pathPositions[1] = b;
                float total = Vector3.Distance(a, b);
                SetupPathLineAppearance(total);
                DrawPath(_pathPositions);
            }
        }

        private void SetupPathLineAppearance(float pathLength = 0f)
        {
            if (!_showPath || _pathLine == null) return;

            var mat = _pathLine.sharedMaterial;
            if (mat == null)
            {
                Shader s = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
                mat = new Material(s);
                _pathLine.sharedMaterial = mat;
            }

            _pathLine.startWidth = _pathWidth;
            _pathLine.endWidth = _pathWidth;

            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(_pathColor, 0f), new GradientColorKey(_pathColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(_pathColor.a, 0f), new GradientAlphaKey(_pathColor.a, 1f) }
            );
            _pathLine.colorGradient = g;
            mat.color = _pathColor;
            mat.SetColor("_Color", _pathColor);
            mat.SetColor("_BaseColor", _pathColor);
            mat.SetColor("_TintColor", _pathColor);
            _pathLine.useWorldSpace = true;
            _pathLine.alignment = LineAlignment.View;
            _pathLine.loop = false;

            _pathLine.useWorldSpace = true;
            _pathLine.alignment = LineAlignment.View;
            if (_useDashTexture)
            {
                if (_dashTexture == null)
                {
                    _dashTexture = CreateDashTexture(64, 4, 8, 8, Color.white);
                }

                if (_dashTexture != null)
                {
                    _dashTexture.wrapMode = TextureWrapMode.Repeat;
                    _dashTexture.filterMode = FilterMode.Bilinear;
                    mat.mainTexture = _dashTexture;
                    _pathLine.textureMode = LineTextureMode.Tile;

                    if (pathLength > 0f && _dashTextureScale > 0f)
                    {
                        float repeats = Mathf.Max(1f, pathLength / _dashTextureScale);
                        mat.SetTextureScale("_MainTex", new Vector2(repeats, 1f));
                    }
                    else
                    {
                        mat.SetTextureScale("_MainTex", new Vector2(_dashTextureScale, 1f));
                    }
                }
            }
            else
            {
                _pathLine.textureMode = LineTextureMode.Stretch;
            }

            var rendererComp = _pathLine.GetComponent<Renderer>();
            if (rendererComp != null)
            {
                rendererComp.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rendererComp.receiveShadows = false;
            }

            if (!_pathLine.gameObject.activeInHierarchy) _pathLine.gameObject.SetActive(true);
            _pathLine.enabled = true;
        }

        private Texture2D CreateDashTexture(int totalWidth, int height, int dashPx, int gapPx, Color color)
        {
            var tex = new Texture2D(totalWidth, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            Color transparent = new Color(0, 0, 0, 0);
            for (int x = 0; x < totalWidth; x++)
            {
                bool dash = (x % (dashPx + gapPx)) < dashPx;
                Color c = dash ? color : transparent;
                for (int y = 0; y < height; y++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private void ClearPathVisual()
        {
            if (_pathLine != null)
            {
                _pathLine.positionCount = 0;
                _pathLine.enabled = false;
            }
            _pathPositions = new Vector3[0];
            if (_pathFollowCoroutine != null)
            {
                StopCoroutine(_pathFollowCoroutine);
                _pathFollowCoroutine = null;
            }

            // destroy target if any
            if (_activeTarget != null)
            {
                if (_targetPulseCoroutine != null) StopCoroutine(_targetPulseCoroutine);
                _activeTarget.SetActive(false);
                _activeTarget = null;
            }
            _isRunning = false;
        }

        private void SpawnOrMoveTarget(Vector3 worldPos)
        {
            _activeTarget = _targetMarker;
            _activeTarget.transform.position = worldPos + Vector3.up * _targetYOffset;
            _activeTarget.SetActive(true);

            if (_targetPulseCoroutine != null) StopCoroutine(_targetPulseCoroutine);
            _targetPulseCoroutine = StartCoroutine(TargetPulseCoroutine(_activeTarget.transform));
        }

        private System.Collections.IEnumerator TargetPulseCoroutine(Transform t)
        {
            float ttime = 0f;
            while (t != null)
            {
                float speedMul = _isRunning ? _targetPulseRunMultiplier : _targetPulseWalkMultiplier;
                ttime += Time.deltaTime * _targetPulseSpeed * speedMul;
                float s = Mathf.Lerp(_targetPulseMin, _targetPulseMax, (Mathf.Sin(ttime) + 1f) * 0.5f);
                t.localScale = Vector3.one * s;
                yield return null;
            }
        }

        private System.Collections.IEnumerator PathFollowCoroutine()
        {
            // Continuously recalculate a lightweight path at intervals and redraw the visual from the agent position.
            while (_pathLine != null && _agent != null)
            {
                // compute path from current agent position to current destination
                NavMeshPath calc = new NavMeshPath();
                bool ok = NavMesh.CalculatePath(_agent.transform.position, _currentDestination, NavMesh.AllAreas, calc);
                if (ok && calc.corners != null && calc.corners.Length >= 2)
                {
                    int len = calc.corners.Length;
                    if (_pathPositions == null || _pathPositions.Length != len) _pathPositions = new Vector3[len];
                    Vector3 agentPos = _agent.transform.position;
                    for (int i = 0; i < len; i++)
                    {
                        Vector3 p = calc.corners[i];
                        if (i == 0) p = agentPos; // start from current agent pos for visual consistency
                        p.y += _pathYOffset;
                        _pathPositions[i] = p;
                    }

                    _pathLine.positionCount = _pathPositions.Length;
                    _pathLine.SetPositions(_pathPositions);
                    _pathLine.enabled = true;
                }
                else
                {
                    // no valid path — clear and wait
                    ClearPathVisual();
                }

                if (!_agent.pathPending && _agent.remainingDistance <= _stopThreshold)
                {
                    break;
                }

                yield return new WaitForSeconds(_pathFollowInterval);
            }

            yield return new WaitForSeconds(0.25f);
            ClearPathVisual();
        }

        private float CalculatePathLength(Vector3[] corners)
        {
            if (corners == null || corners.Length < 2) return 0f;
            float acc = 0f;
            for (int i = 0; i < corners.Length - 1; i++) acc += Vector3.Distance(corners[i], corners[i + 1]);
            return acc;
        }
    }
}
