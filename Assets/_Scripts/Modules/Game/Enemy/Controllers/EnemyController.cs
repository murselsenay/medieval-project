using System.Collections;
using System.Collections.Generic;
using Modules.EventSystem.Managers;
using Modules.Game.Constants;
using UnityEngine;
using UnityEngine.AI;

namespace Modules.Game.Enemy.Controllers
{
    [DisallowMultipleComponent]
    public class EnemyController : MonoBehaviour
    {
        [BHeader("Patrol")]
        [SerializeField] private EnemyPatrol _patrol;
        [BHeader("Vision")]
        [SerializeField] private EnemyVision _vision;
        [BHeader("Animation")]
        [SerializeField] private EnemyAnimationController _animController;
        [BHeader("Nav Mesh")]
        [SerializeField] private NavMeshAgent _navAgent;
        [BHeader("Spotted Settings")]
        [SerializeField] private float _spottedCooldown = 2f;

        //Spotting Refs
        private bool _spottedActive = false;
        private float _lastSpottedTime = -999f;

        //Player Refs
        private GameObject _playerObj;
        private NavMeshObstacle _playerObstacle;
        private bool _playerPrevCarving = false;
        private NavMeshAgent _playerNavAgent;

        //Avoidance Refs
        private ObstacleAvoidanceType _playerPrevAvoidanceType;
        private int _playerPrevAvoidancePriority;
        private ObstacleAvoidanceType _prevAvoidanceType;
        private int _prevAvoidancePriority;

        private void Awake()
        {
            _navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            _navAgent.avoidancePriority = 0;

            try
            {
                var players = GameObject.FindGameObjectsWithTag(CollisionTags.Player);
                foreach (var player in players)
                {
                    if (player == null) continue;
                    var pAgent = player.GetComponent<NavMeshAgent>();
                    if (pAgent != null)
                    {
                        pAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                        pAgent.avoidancePriority = 99;
                    }
                    var pObs = player.GetComponent<NavMeshObstacle>();
                    if (pObs != null)
                    {
                        pObs.carving = false;
                        pObs.enabled = false;
                    }
                }
                try
                {
                    var agent = GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
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
            EventManager.DelegateEnemySetSpeed(transform, 0f);
        }

        private void OnDestroy()
        {
            if (_vision != null)
            {
                _vision.OnFillComplete -= HandleFillComplete;
                _vision.OnFillCleared -= HandleFillCleared;
            }
            EventManager.OnEnemySetSpeed -= HandleEnemySetSpeed;
        }

        private void HandleEnemySetSpeed(Transform enemy, float normalized)
        {
            if (enemy != transform) return;
            if (_spottedActive) return;
            if (_animController != null)
            {
                _animController.SetMovementSpeed(normalized);
            }
        }

        private void HandleFillComplete()
        {
            if (_spottedActive) return;
            if (Time.time - _lastSpottedTime < _spottedCooldown) return;
            _spottedActive = true;
            _lastSpottedTime = Time.time;

            _patrol.StopPatrol();

            _animController.ForceSetSpeedZero();
            _animController.EnableMovementUpdates(false);
            _animController.TriggerSpotted();

            try
            {
                var t = _vision != null ? _vision.CurrentTarget : null;
                if (t != null)
                {
                    _playerObj = t.gameObject;
                    _playerObstacle = _playerObj.GetComponent<NavMeshObstacle>();
                    if (_playerObstacle != null)
                    {
                        _playerPrevCarving = _playerObstacle.carving;
                        _playerObstacle.carving = false;
                    }
                    _playerNavAgent = _playerObj.GetComponent<NavMeshAgent>();
                    if (_playerNavAgent != null)
                    {
                        _playerPrevAvoidanceType = _playerNavAgent.obstacleAvoidanceType;
                        _playerPrevAvoidancePriority = _playerNavAgent.avoidancePriority;

                        _playerNavAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                        _playerNavAgent.avoidancePriority = 99;
                    }
                    if (_navAgent != null)
                    {
                        _prevAvoidanceType = _navAgent.obstacleAvoidanceType;
                        _prevAvoidancePriority = _navAgent.avoidancePriority;
                        _navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                        _navAgent.avoidancePriority = 0;
                    }
                }
            }
            catch { }
        }

        private void HandleFillCleared()
        {
            _patrol.StartPatrol();

            _animController.ClearSpotted();

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

            _spottedActive = false;

            _animController.EnableMovementUpdates(true);
        }

        public void SetPatrolTargetsParent(Transform parent)
        {
            _patrol.SetTargetsParent(parent);
        }

        private void OnEnable()
        {
            _vision.OnFillComplete += HandleFillComplete;
            _vision.OnFillCleared += HandleFillCleared;

            EventManager.OnEnemySetSpeed += HandleEnemySetSpeed;
        }

        private void OnDisable()
        {
            _vision.OnFillComplete -= HandleFillComplete;
            _vision.OnFillCleared -= HandleFillCleared;

            EventManager.OnEnemySetSpeed -= HandleEnemySetSpeed;
        }
    }
}

