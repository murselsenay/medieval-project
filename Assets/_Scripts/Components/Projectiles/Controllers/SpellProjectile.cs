using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Components.Projectiles.Controllers
{
    public class SpellProjectile : BaseObject
    {
        [Header("Projectile")]
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _maxLifeTime = 5f;
        [SerializeField] private float _stopDistance = 0.25f;
        [SerializeField] private float _rotationSpeed = 720f;

        private float _damage = 0f;
        private string _ownerTag = null;
        private GameObject _ownerObject = null;

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private Transform _targetTransform;
        private float _lifeTimer;
        private bool _isLaunched;
        private bool _reachedTarget;

        public void Launch(Transform start, Vector3 targetPosition, float? speed = null, float? maxLifeTime = null, float? damage = null, string ownerTag = null, GameObject owner = null)
        {
            if (start == null)
            {
                Debug.LogWarning("SpellProjectile: Launched with null start transform");
                return;
            }

            transform.position = start.position;
            _startPosition = start.position;

            if (speed.HasValue) _speed = speed.Value;
            if (maxLifeTime.HasValue) _maxLifeTime = maxLifeTime.Value;

            _targetPosition = targetPosition;
            _targetTransform = null;
            _lifeTimer = 0f;
            _isLaunched = true;
            _reachedTarget = false;

            _damage = damage ?? 0f;
            _ownerTag = ownerTag;
            _ownerObject = owner;

            Activate();

            Vector3 desiredDir = (_targetPosition - transform.position);
            if (desiredDir.sqrMagnitude <= 0.000001f) desiredDir = start.forward;
            transform.rotation = Quaternion.LookRotation(desiredDir.normalized);
        }

        public void Launch(Transform start, Transform targetTransform, float? speed = null, float? maxLifeTime = null, float? damage = null, string ownerTag = null, GameObject owner = null)
        {
            Vector3 targetPos = (targetTransform != null) ? targetTransform.position : start.position;

            Launch(start, targetPos, speed, maxLifeTime, damage, ownerTag, owner);
            _targetTransform = targetTransform;
        }

        public void SetTargetTransform(Transform target)
        {
            _targetTransform = target;
        }

        protected virtual void Update()
        {
            if (!_isLaunched || !gameObject.activeInHierarchy) return;

            if (_targetTransform != null)
            {
                _targetPosition = _targetTransform.position;
            }

            float delta = Time.deltaTime;
            _lifeTimer += delta;

            Vector3 prevPos = transform.position;
            Vector3 nextPos = Vector3.MoveTowards(prevPos, _targetPosition, _speed * delta);
            Vector3 vel = nextPos - prevPos;

            if (vel.sqrMagnitude > 0.000001f)
            {
                var targetRot = Quaternion.LookRotation(vel.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * delta);
            }

            transform.position = nextPos;

            if (Vector3.Distance(transform.position, _targetPosition) <= _stopDistance || transform.position == _targetPosition)
            {
                _reachedTarget = true;
                Finish();
                return;
            }

            if (_lifeTimer >= _maxLifeTime)
            {
                _reachedTarget = false;
                Finish();
                return;
            }
        }

        private void Finish()
        {
            _isLaunched = false;

            if (_reachedTarget)
            {
                ApplyDamageOnArrival();
            }

            Deactivate();
        }

        private void ApplyDamageOnArrival()
        {
            if (_damage <= 0f) return;

            int dmg = Mathf.RoundToInt(_damage);
            if (dmg <= 0) return;

            if (_targetTransform != null)
            {
                if (_ownerObject != null && (_targetTransform.gameObject == _ownerObject || _targetTransform.IsChildOf(_ownerObject.transform))) return;

                var minion = _targetTransform.GetComponentInParent<Components.Minions.Controllers.MinionController>();
                if (minion != null)
                {
                    minion.TakeDamage(dmg);
                    return;
                }

                var tower = _targetTransform.GetComponentInParent<Components.Towers.Controllers.BaseTower>();
                if (tower != null)
                {
                    tower.TakeDamage(dmg);
                    return;
                }
            }

            Collider[] cols = Physics.OverlapSphere(_targetPosition, Mathf.Max(0.1f, _stopDistance + 0.05f));
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c == null) continue;
                if (!string.IsNullOrEmpty(_ownerTag) && c.gameObject.CompareTag(_ownerTag)) continue;
                if (_ownerObject != null && (c.gameObject == _ownerObject || c.transform.IsChildOf(_ownerObject.transform))) continue;

                var minion = c.GetComponentInParent<Components.Minions.Controllers.MinionController>();
                if (minion != null)
                {
                    minion.TakeDamage(dmg);
                    return;
                }

                var tower = c.GetComponentInParent<Components.Towers.Controllers.BaseTower>();
                if (tower != null)
                {
                    tower.TakeDamage(dmg);
                    return;
                }
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();
            _isLaunched = false;
            _damage = 0f;
            _ownerTag = null;
            _ownerObject = null;
            _targetTransform = null;
            _reachedTarget = false;
            _lifeTimer = 0f;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _isLaunched = false;
        }
    }
}