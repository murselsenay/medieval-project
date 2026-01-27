using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Modules.Enemy.Controllers
{
    [DisallowMultipleComponent]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyPatrol _patrol;
        [Header("Vision")]
        [SerializeField] private Modules.Enemy.Controllers.EnemyVision _vision;
        [SerializeField] private EnemyAnimationController _animController;
        [Header("Reaction")]
        // animations handled by EnemyAnimationController
        [Header("Spotted Settings")]
        [Tooltip("Minimum seconds between consecutive spotted reactions")]
        [SerializeField] private float _spottedCooldown = 2f;

        private bool _spottedActive = false;
        private float _lastSpottedTime = -999f;
        private UnityEngine.AI.NavMeshAgent _navAgent;
        private GameObject _playerObj;
        private UnityEngine.AI.NavMeshObstacle _playerObstacle;
        private bool _playerPrevCarving = false;
        private UnityEngine.AI.NavMeshAgent _playerNavAgent;
        private UnityEngine.AI.ObstacleAvoidanceType _playerPrevAvoidanceType;
        private int _playerPrevAvoidancePriority;
        private UnityEngine.AI.ObstacleAvoidanceType _prevAvoidanceType;
        private int _prevAvoidancePriority;
        [Header("Player Interaction")]
        [Tooltip("Tag used to find player object to disable avoidance on.")]
        [SerializeField] private string _playerTag = "Player";

        private void Awake()
        {
            if (_patrol == null)
                _patrol = GetComponent<EnemyPatrol>();

            if (_vision == null)
                _vision = GetComponentInChildren<EnemyVision>();

            if (_vision != null)
            {
                _vision.OnFillComplete += HandleFillComplete;
                _vision.OnFillCleared += HandleFillCleared;
            }
            // subscribe to patrol reported events
            Modules.EventSystem.Managers.EventManager.OnEnemySetSpeed += HandleEnemySetSpeed;
            if (_animController == null)
                _animController = GetComponentInChildren<EnemyAnimationController>();
            _navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            // Permanent: make enemy agents ignore local obstacle avoidance so they don't path around player/other agents.
            if (_navAgent != null)
            {
                _navAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
                _navAgent.avoidancePriority = 0; // highest priority so others yield (they won't avoid)
            }

            // Also ensure player(s) do not act as NavMesh obstacles or use local avoidance.
            try
            {
                var players = GameObject.FindGameObjectsWithTag(_playerTag);
                foreach (var player in players)
                {
                    if (player == null) continue;
                    var pAgent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (pAgent != null)
                    {
                        pAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
                        pAgent.avoidancePriority = 99;
                    }
                    var pObs = player.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                    if (pObs != null)
                    {
                        // disable obstacle behaviour permanently - player should not carve NavMesh
                        pObs.carving = false;
                        pObs.enabled = false;
                    }
                }
            // ensure NavMeshAgent stops immediately and zero velocity to avoid sliding
            try
            {
                var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                    // directly zero velocity via next update
                    agent.velocity = Vector3.zero;
                }
            }
            catch { }
            }
            catch { }
        }

        private void Start()
        {
            if (_patrol != null)
                _patrol.StartPatrol();
            // ensure animation updates re-enabled via event
            Modules.EventSystem.Managers.EventManager.DelegateEnemySetSpeed(transform, 0f);
        }

        private void OnDestroy()
        {
            if (_vision != null)
            {
                _vision.OnFillComplete -= HandleFillComplete;
                _vision.OnFillCleared -= HandleFillCleared;
            }
            Modules.EventSystem.Managers.EventManager.OnEnemySetSpeed -= HandleEnemySetSpeed;
        }

        private void HandleEnemySetSpeed(Transform enemy, float normalized)
        {
            if (enemy != transform) return; // only react for this enemy instance
            if (_spottedActive) return; // ignore movement updates while spotted
            if (_animController != null)
            {
                _animController.SetMovementSpeed(normalized);
            }
        }

        private void HandleFillComplete()
        {
            // guard: avoid re-entering spotted while already handling it
            if (_spottedActive) return;
            // optional cooldown to avoid spam
            if (Time.time - _lastSpottedTime < _spottedCooldown) return;
            _spottedActive = true;
            _lastSpottedTime = Time.time;

            // stop patrolling
            if (_patrol != null)
                _patrol.StopPatrol();

            // immediately force animator speed zero and disable movement updates to prevent races
            if (_animController != null)
            {
                _animController.ForceSetSpeedZero();
                _animController.EnableMovementUpdates(false);
                _animController.TriggerSpotted();
                // if the current target has a NavMeshObstacle, disable carving so agent pathing doesn't avoid the player
                try
                {
                    var t = _vision != null ? _vision.CurrentTarget : null;
                    if (t != null)
                    {
                        _playerObj = t.gameObject;
                        _playerObstacle = _playerObj.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                        if (_playerObstacle != null)
                        {
                            _playerPrevCarving = _playerObstacle.carving;
                            _playerObstacle.carving = false;
                        }
                        _playerNavAgent = _playerObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        if (_playerNavAgent != null)
                        {
                            _playerPrevAvoidanceType = _playerNavAgent.obstacleAvoidanceType;
                            _playerPrevAvoidancePriority = _playerNavAgent.avoidancePriority;
                            // set player to low-priority so enemies don't avoid it; alternatively, disable avoidance
                            _playerNavAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
                            _playerNavAgent.avoidancePriority = 99;
                        }
                        if (_navAgent != null)
                        {
                            _prevAvoidanceType = _navAgent.obstacleAvoidanceType;
                            _prevAvoidancePriority = _navAgent.avoidancePriority;
                            _navAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
                            _navAgent.avoidancePriority = 0;
                        }
                    }
                }
                catch { }
            }
        }

        private void HandleFillCleared()
        {
            // resume patrol when fill cleared
            if (_patrol != null)
                _patrol.StartPatrol();
            // clear spotted via anim controller
            if (_animController != null)
                _animController.ClearSpotted();

            // restore player's NavMeshObstacle carving if we disabled it
            try
            {
                if (_playerObstacle != null)
                {
                    _playerObstacle.carving = _playerPrevCarving;
                    _playerObstacle = null;
                    _playerObj = null;
                }
                if (_playerNavAgent != null)
                {
                    _playerNavAgent.obstacleAvoidanceType = _playerPrevAvoidanceType;
                    _playerNavAgent.avoidancePriority = _playerPrevAvoidancePriority;
                    _playerNavAgent = null;
                }
                if (_navAgent != null)
                {
                    _navAgent.obstacleAvoidanceType = _prevAvoidanceType;
                    _navAgent.avoidancePriority = _prevAvoidancePriority;
                }
            }
            catch { }

            // allow future spotted
            _spottedActive = false;
            // re-enable movement updates so animation controller accepts speed events again
            if (_animController != null)
                _animController.EnableMovementUpdates(true);
        }

        // animation controller monitors spotted state; no need to duplicate here

        public void SetPatrolTargetsParent(Transform parent)
        {
            if (_patrol != null)
                _patrol.SetTargetsParent(parent);
        }
    }
}

