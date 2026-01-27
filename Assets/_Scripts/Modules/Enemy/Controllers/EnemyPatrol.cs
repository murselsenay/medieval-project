using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Modules.Enemy.Controllers
{
    /// <summary>
    /// Patrol behaviour moved out of EnemyController.
    /// Controls NavMeshAgent-based movement between child waypoints of a provided parent.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyPatrol : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _targetsParent;
        [SerializeField] private NavMeshAgent _navAgent;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _arriveThreshold = 0.1f;
        [SerializeField] private float _waitAtPoint = 0.25f;
        [SerializeField] private bool _faceMovementDirection = true;

        // Patrol only reports normalized speed via EventManager; animation handled by EnemyAnimationController via EnemyController

        private List<Transform> _targets = new List<Transform>();
        private int _currentIndex = 0;
        private int _step = 1;
        private Coroutine _patrolCoroutine;

        private void Awake()
        {
            BuildTargetList();
        }

        private void OnEnable()
        {
            if (_navAgent == null)
                _navAgent = GetComponent<NavMeshAgent>();
        }

        // Start does not manage animator directly anymore

        public void StartPatrol()
        {
            if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine);
            _patrolCoroutine = StartCoroutine(PatrolRoutine());
        }

        public void StopPatrol()
        {
            if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine);
            _patrolCoroutine = null;
            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
                // clear path and zero velocity to avoid sliding
                _navAgent.ResetPath();
                try { _navAgent.velocity = Vector3.zero; } catch { }
            }

            // also clear physics velocity if present
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // patrol only reports movement speed via EventManager; animation toggles are handled by EnemyAnimationController

        private void BuildTargetList()
        {
            _targets.Clear();
            if (_targetsParent == null) return;
            foreach (Transform child in _targetsParent)
            {
                if (child == null) continue;
                _targets.Add(child);
            }
        }

        private IEnumerator PatrolRoutine()
        {
            _currentIndex = Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, _targets.Count - 1));

            while (true)
            {
                if (_targets.Count == 0) yield break;

                Transform target = _targets[_currentIndex];
                if (target == null)
                {
                    BuildTargetList();
                    yield return null;
                    continue;
                }

                if (_navAgent == null)
                {
                    _navAgent = GetComponent<NavMeshAgent>();
                    if (_navAgent == null)
                    {
                        Debug.LogWarning($"EnemyPatrol on '{name}' requires a NavMeshAgent.");
                        yield break;
                    }
                }

                _navAgent.isStopped = false;
                _navAgent.speed = _moveSpeed;
                _navAgent.stoppingDistance = _arriveThreshold;
                _navAgent.SetDestination(target.position);

                while (_navAgent.pathPending)
                    yield return null;

                while (!_navAgent.pathPending && _navAgent.remainingDistance > _navAgent.stoppingDistance)
                {
                    Vector3 vel = _navAgent.velocity;
                    if (_faceMovementDirection && vel.sqrMagnitude > 0.0001f)
                    {
                        Quaternion look = Quaternion.LookRotation(vel.normalized, Vector3.up);
                        _navAgent.transform.rotation = Quaternion.Slerp(_navAgent.transform.rotation, look, 10f * Time.deltaTime);
                    }

                    {
                        float normalized = Mathf.Clamp01(vel.magnitude / Mathf.Max(0.0001f, _navAgent.speed));
                        Modules.EventSystem.Managers.EventManager.DelegateEnemySetSpeed(transform, normalized);
                    }

                    yield return null;
                }

                _navAgent.isStopped = true;
                Modules.EventSystem.Managers.EventManager.DelegateEnemySetSpeed(transform, 0f);

                yield return new WaitForSeconds(_waitAtPoint);

                if (_navAgent != null)
                    _navAgent.ResetPath();

                if (_targets.Count > 1)
                {
                    if (_currentIndex == _targets.Count - 1) _step = -1;
                    else if (_currentIndex == 0) _step = 1;
                    _currentIndex += _step;
                }
                else
                {
                    yield return new WaitForSeconds(_waitAtPoint);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_targetsParent == null) return;
            Gizmos.color = Color.yellow;
            Vector3 prev = Vector3.zero;
            bool first = true;
            foreach (Transform child in _targetsParent)
            {
                if (child == null) continue;
                Gizmos.DrawWireSphere(child.position, 0.2f);
                if (!first) Gizmos.DrawLine(prev, child.position);
                prev = child.position;
                first = false;
            }
        }

        public void SetTargetsParent(Transform parent)
        {
            _targetsParent = parent;
            BuildTargetList();
            StartPatrol();
        }
    }
}
