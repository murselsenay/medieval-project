using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Components.Tiles.Controllers;
using Components.Tiles.Enums;

namespace Components.Projectiles.Controllers
{
    public class AreaProjectile : BaseObject
    {
        [Header("Projectile")]
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _maxLifeTime = 5f;
        [SerializeField] private float _stopDistance = 0.25f;
        [SerializeField] private float _rotationSpeed = 720f;
        [Tooltip("Peak height of the parabolic arc relative to the straight line path.")]
        [SerializeField] private float _arcHeight = 2f;
        [Tooltip("Radius used to detect minions/towers around each affected hex center.")]
        [SerializeField] private float _explosionRadius = 1f;

        private float _damage = 0f;
        private string _ownerTag = null;
        private GameObject _ownerObject = null;

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private Transform _targetHexTransform;
        private float _lifeTimer;
        private bool _isLaunched;
        private float _flightTime;
        private float _elapsed;
        private bool _reachedTarget;

        // Launch overloads that accept ownerTag and owner (matches existing call sites)
        public void Launch(Transform start, Transform targetHexTransform, float? speed = null, float? maxLifeTime = null, float? arcHeight = null, float? damage = null, string ownerTag = null, GameObject owner = null)
        {
            if (start == null)
            {
                Debug.LogWarning("AreaProjectile: Launched with null start transform");
                return;
            }

            transform.position = start.position;
            _startPosition = start.position;

            if (speed.HasValue) _speed = speed.Value;
            if (maxLifeTime.HasValue) _maxLifeTime = maxLifeTime.Value;
            if (arcHeight.HasValue) _arcHeight = Mathf.Max(0f, arcHeight.Value);

            _targetHexTransform = targetHexTransform;
            _targetPosition = (_targetHexTransform != null) ? _targetHexTransform.position : _startPosition;

            _lifeTimer = 0f;
            _elapsed = 0f;
            _reachedTarget = false;

            float distance = Vector3.Distance(_startPosition, _targetPosition);
            _flightTime = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, _speed));

            _isLaunched = true;

            _damage = damage ?? 0f;
            _ownerTag = ownerTag;
            _ownerObject = owner;

            Activate();

            Vector3 desiredDir = (_targetPosition - transform.position);
            if (desiredDir.sqrMagnitude <= 0.000001f) desiredDir = start.forward;
            transform.rotation = Quaternion.LookRotation(desiredDir.normalized);
        }

        public void Launch(Transform start, Vector3 targetPosition, float? speed = null, float? maxLifeTime = null, float? arcHeight = null, float? damage = null, string ownerTag = null, GameObject owner = null)
        {
            if (start == null)
            {
                Debug.LogWarning("AreaProjectile: Launched with null start transform");
                return;
            }

            transform.position = start.position;
            _startPosition = start.position;

            if (speed.HasValue) _speed = speed.Value;
            if (maxLifeTime.HasValue) _maxLifeTime = maxLifeTime.Value;
            if (arcHeight.HasValue) _arcHeight = Mathf.Max(0f, arcHeight.Value);

            _targetHexTransform = null;
            _targetPosition = targetPosition;

            _lifeTimer = 0f;
            _elapsed = 0f;
            _reachedTarget = false;

            float distance = Vector3.Distance(_startPosition, _targetPosition);
            _flightTime = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, _speed));

            _isLaunched = true;

            _damage = damage ?? 0f;
            _ownerTag = ownerTag;
            _ownerObject = owner;

            Activate();

            Vector3 desiredDir = (_targetPosition - transform.position);
            if (desiredDir.sqrMagnitude <= 0.000001f) desiredDir = start.forward;
            transform.rotation = Quaternion.LookRotation(desiredDir.normalized);
        }

        protected virtual void Update()
        {
            if (!_isLaunched || !gameObject.activeInHierarchy) return;

            if (_targetHexTransform != null)
            {
                _targetPosition = _targetHexTransform.position;
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
                var targetRot = Quaternion.LookRotation(vel.normalized);
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

        private void Finish()
        {
            _isLaunched = false;

            if (_reachedTarget)
            {
                ApplyAreaDamageAtTarget();
            }

            Deactivate();
        }

        private void ApplyAreaDamageAtTarget()
        {
            if (_damage <= 0f) return;

            int dmg = Mathf.RoundToInt(_damage);
            if (dmg <= 0) return;

            // Only affect the targeted hex if it exists and is in an allowed state (Enabled or Spawn)
            HexagonController targetHex = null;
            if (_targetHexTransform != null)
            {
                targetHex = _targetHexTransform.GetComponent<HexagonController>();
                if (targetHex == null)
                {
                    // Fallback: try to find a hex whose transform matches
                    var grid = Object.FindObjectOfType<HexagonGridController>();
                    if (grid != null)
                    {
                        targetHex = grid.Hexagons.Find(h => h != null && h.transform == _targetHexTransform);
                    }
                }
            }

            if (targetHex == null) return;

            if (targetHex.State == EHexagonState.Enabled || targetHex.State == EHexagonState.Spawn)
            {
                targetHex.BombedHexagon(dmg);
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();
            _isLaunched = false;
            _damage = 0f;
            _ownerObject = null;
            _ownerTag = null;
            _targetHexTransform = null;
            _reachedTarget = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _isLaunched = false;
        }
    }
}
