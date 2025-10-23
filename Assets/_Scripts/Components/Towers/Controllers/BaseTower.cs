using Components.Constants;
using Components.General;
using Components.Projectiles.Controllers;
using Components.Towers.Enums;
using Components.Towers.Models;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Components.Minions.Controllers;
using Modules.ObjectPoolSystem;
using Modules.EventSystem.Managers;
using TMPro;
using Modules.TowerSystem.Managers;

namespace Components.Towers.Controllers
{
    public class BaseTower : BaseObject
    {
        [Header("UI")]
        [SerializeField] private Healthbar _healthbar;
        [SerializeField] private TMP_Text _levelText;
        [Header("Tower Settings")]
        [SerializeField] private ETowerType _towerType;
        [SerializeField] private RangeCircle _rangeCircle;
        [SerializeField] private Transform _startTransform;
        protected float _range = 5f;
        protected float _fireCooldown = 1f;
        protected string _enemyTag = GameObjectTags.Enemy;
        protected float _projectileSpeed = 15f;
        protected float _projectileDamage = 0f; // added to store projectile damage

        // Health
        private float _maxHealth = 100f;
        private float _currentHealth = 100f;
        private bool _isDead = false;

        private float _cooldownTimer;
        private readonly List<Transform> _targets = new List<Transform>();

        // Exposed progression id so external managers can link progression datasets to this tower
        public int TowerId { get; set; }

        public Transform StartTransform => _startTransform;

        // allow derived towers to disable base auto-targeting
        protected bool _useAutoTargeting = true;

        public virtual void Initialize(TowerData data)
        {
            _towerType = data.TowerType;
            _range = data.ProjectileRange;
            _fireCooldown = data.ProjectileCooldown;
            _projectileSpeed = data.ProjectileSpeed;
            _projectileDamage = data.ProjectileDamage; // assign damage

            _maxHealth = data.Health;
            _currentHealth = _maxHealth;
            _levelText.text = TowerManager.GetCurrentLevelInfo(TowerId).Level.ToString();

            _isDead = false;


            if (_healthbar != null)
            {
                _healthbar.Initialize(_maxHealth, _currentHealth);
            }

            if (_rangeCircle != null)
            {
                _rangeCircle.SetRadius(_range);
                // Ensure the range visual is hidden by default; it will be shown when requested
                _rangeCircle.SetVisible(false);
            }
        }

        private void Update()
        {
            if (!_useAutoTargeting) return;

            if (_isDead) return;
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

            RefreshTargetsInRange();

            if (_cooldownTimer <= 0f)
            {
                Transform target = GetNearestTarget();
                if (target != null)
                {
                    FireAtTarget(target).Forget();
                    _cooldownTimer = _fireCooldown;
                }
            }
        }

        private bool IsTargetValid(Transform t)
        {
            if (t == null) return false;
            var minion = t.GetComponentInParent<MinionController>();
            if (minion == null || minion.IsDead) return false;
            float sqr = (minion.transform.position - transform.position).sqrMagnitude;
            return sqr <= (_range * _range);
        }

        private void RefreshTargetsInRange()
        {
            _targets.Clear();
            Collider[] cols = Physics.OverlapSphere(transform.position, _range);
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c == null) continue;
                if (!c.CompareTag(_enemyTag)) continue;
                var minion = c.GetComponentInParent<MinionController>();
                if (minion == null || minion.IsDead) continue;
                if (minion.transform != null)
                    _targets.Add(minion.transform);
            }
        }

        private Transform GetNearestTarget()
        {
            Transform nearest = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                if (t == null) continue;
                var minion = t.GetComponentInParent<MinionController>();
                if (minion == null || minion.IsDead) continue;
                float sqr = (t.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = t;
                }
            }
            return nearest;
        }

        // Damage handling
        public void TakeDamage(int amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;
            if (_currentHealth < 0f) _currentHealth = 0f;

            if (_healthbar != null)
            {
                _healthbar.SetCurrent(_currentHealth);
            }

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        public void TakeHit(int amount)
        {
            TakeDamage(amount);
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            // TODO: play death effects/animations here

            // Deactivate the tower (returns to pool if used)
            Deactivate();
        }

        protected virtual async UniTaskVoid FireAtTarget(Transform target)
        {
            if (!IsTargetValid(target)) return;
            await UniTask.Yield();
            Debug.LogWarning($"{nameof(BaseTower)}: FireAtTarget not implemented for tower type {_towerType}.");
        }

        private void OnMouseDown()
        {
            if (!gameObject.activeInHierarchy) return;

            EventManager.DelegateTowerClicked(TowerId);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _range);
        }
        public override void Activate()
        {
            base.Activate();
            EventManager.OnTowerUpgraded += OnTowerUpgraded;
        }
        public override void Deactivate()
        {
            base.Deactivate();
            EventManager.OnTowerUpgraded -= OnTowerUpgraded;
        }

        private void OnTowerUpgraded(int towerId)
        {
            if (towerId != TowerId) return;
            _levelText.text = TowerManager.GetCurrentLevelInfo(TowerId).Level.ToString();
        }

        // New API: show/hide range visual
        public void SetRangeVisible(bool visible)
        {
            if (_rangeCircle == null) return;
            _rangeCircle.SetVisible(visible);
        }
    }
}
