using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Components.Projectiles.Controllers
{
    public class ArrowProjectile : BaseObject
    {
        private enum ForwardAxis { Z_Pos, X_Pos, Z_Neg, X_Neg, Y_Pos, Y_Neg }
        [Header("Projectile")]
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _maxLifeTime = 5f;
        [SerializeField] private float _stopDistance = 0.25f;
        [SerializeField] private float _rotationSpeed = 720f;
        [Tooltip("Select model forward axis so the visible arrow points towards movement direction.")]
        [SerializeField] private ForwardAxis _modelForward = ForwardAxis.Y_Neg;
        [Tooltip("Peak height of the parabolic arc relative to the straight line path.")]
        [SerializeField] private float _arcHeight = 2f;

        private float _damage = 0f;
        private string _ownerTag = null;
        private GameObject _ownerObject = null;

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private Transform _targetTransform; 
        private float _lifeTimer;
        private bool _isLaunched;
        private float _flightTime;
        private float _elapsed;
        private bool _reachedTarget; 
        private Vector3 _direction;
        private Coroutine _orientRoutine;

        public void Launch(Transform start, Vector3 targetPosition, float? speed = null, float? maxLifeTime = null, float? arcHeight = null, float? damage = null, string ownerTag = null, GameObject owner = null)
        {
            if (start == null)
            {
                Debug.LogWarning("Launched with null start transform");
                return;
            }

            transform.position = start.position;
            _startPosition = start.position;

            if (speed.HasValue) _speed = speed.Value;
            if (maxLifeTime.HasValue) _maxLifeTime = maxLifeTime.Value;
            if (arcHeight.HasValue) _arcHeight = Mathf.Max(0f, arcHeight.Value);

            _targetPosition = targetPosition;
            _targetTransform = null;
            _lifeTimer = 0f;
            _elapsed = 0f;
            _reachedTarget = false;

            float distance = Vector3.Distance(_startPosition, _targetPosition);
            _flightTime = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, _speed));

            if (_orientRoutine != null)
            {
                StopCoroutine(_orientRoutine);
                _orientRoutine = null;
            }

            _isLaunched = true;

            _damage = damage ?? 0f;
            _ownerTag = ownerTag;
            _ownerObject = owner;

            Activate();

            Vector3 desiredDir = (_targetPosition - transform.position);
            if (desiredDir.sqrMagnitude <= 0.000001f) desiredDir = start.forward;
            Quaternion desiredRot = Quaternion.LookRotation(desiredDir.normalized) * GetModelCorrection();
            transform.rotation = desiredRot;
        }

        public void Launch(Transform start, Transform targetTransform, float? speed = null, float? maxLifeTime = null, float? arcHeight = null, float? damage = null, string ownerTag = null, GameObject owner = null)
        {
            Vector3 targetPos = (targetTransform != null) ? targetTransform.position : start.position;

            Launch(start, targetPos, speed, maxLifeTime, arcHeight, damage, ownerTag, owner);
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
            _elapsed += delta;

            float t = (_flightTime <= 0f) ? 1f : Mathf.Clamp01(_elapsed / _flightTime);

            Vector3 basePos = Vector3.Lerp(_startPosition, _targetPosition, t);
            float height = 4f * _arcHeight * t * (1f - t);
            Vector3 pos = basePos + Vector3.up * height;

            float tNext = Mathf.Clamp01(t + 0.01f);
            Vector3 baseNext = Vector3.Lerp(_startPosition, _targetPosition, tNext);
            float heightNext = 4f * _arcHeight * tNext * (1f - tNext);
            Vector3 posNext = baseNext + Vector3.up * heightNext;
            Vector3 vel = (posNext - pos);
            if (vel.sqrMagnitude > 0.000001f)
            {
                var targetRot = Quaternion.LookRotation(vel.normalized) * GetModelCorrection();
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * delta);
            }

            transform.position = pos;

            if (t >= 1f || Vector3.Distance(transform.position, _targetPosition) <= _stopDistance)
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

        private Quaternion GetModelCorrection()
        {
            switch (_modelForward)
            {
                case ForwardAxis.X_Pos:
                    return Quaternion.Euler(0f, -90f, 0f);
                case ForwardAxis.Z_Neg:
                    return Quaternion.Euler(0f, 180f, 0f);
                case ForwardAxis.X_Neg:
                    return Quaternion.Euler(0f, 90f, 0f);
                case ForwardAxis.Y_Pos:
                    return Quaternion.Euler(90f, 0f, 0f);
                case ForwardAxis.Y_Neg:
                    return Quaternion.Euler(-90f, 0f, 0f);
                case ForwardAxis.Z_Pos:
                default:
                    return Quaternion.identity;
            }
        }

        private void Finish()
        {
            _isLaunched = false;
            if (_orientRoutine != null)
            {
                StopCoroutine(_orientRoutine);
                _orientRoutine = null;
            }

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
                if (!string.IsNullOrEmpty(_ownerTag) && _targetTransform.gameObject.CompareTag(_ownerTag)) return;
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
            if (_orientRoutine != null)
            {
                StopCoroutine(_orientRoutine);
                _orientRoutine = null;
            }

            _damage = 0f;
            _ownerTag = null;
            _ownerObject = null;

            _targetTransform = null;
            _reachedTarget = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _isLaunched = false;
            if (_orientRoutine != null)
            {
                StopCoroutine(_orientRoutine);
                _orientRoutine = null;
            }
        }
    }
}