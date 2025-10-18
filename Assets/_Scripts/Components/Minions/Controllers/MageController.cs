using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Components.Minions.Enums;
using Modules.ObjectPoolSystem;
using Components.Projectiles.Controllers;
using Cysharp.Threading.Tasks;
using Components.Constants;
using Components.Towers.Controllers;

namespace Components.Minions.Controllers
{
    public class MageController : MinionController
    {
        [Header("Mage Settings")]
        [SerializeField] private Transform _projectileStart;
        [SerializeField] private float _projectileSpeed = 15f;
        [SerializeField] private float _startHeight = 1f;
        [SerializeField] private float _targetAimHeight = 0.25f;

        [SerializeField] private float _detectionRadius = 20f;

        [SerializeField] private float _rangedAttackRange = 10f;

        private bool _isAttackingLocal;

        protected override void Awake()
        {
            base.Awake();
            _minionType = EMinionType.Mage;
            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_projectileStart == null)
            {
                var go = new GameObject("ProjectileStart");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0f, _startHeight, 0f);
                go.transform.localRotation = Quaternion.identity;
                _projectileStart = go.transform;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _isAttackingLocal = false;
        }

        private new void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            if (IsDead) return;

            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                Collider[] cols = Physics.OverlapSphere(transform.position, _detectionRadius);
                BaseTower nearest = null;
                float bestSqr = float.MaxValue;
                for (int i = 0; i < cols.Length; i++)
                {
                    var c = cols[i];
                    if (c == null) continue;

                    var t = c.GetComponentInParent<BaseTower>();
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    float sq = (t.transform.position - transform.position).sqrMagnitude;
                    if (sq < bestSqr)
                    {
                        bestSqr = sq;
                        nearest = t;
                    }
                }

                if (nearest != null)
                    _target = nearest;
            }

            if (_target == null)
            {
                SetRunState(false);
                return;
            }

            var dir = _target.transform.position - transform.position;
            var dist = dir.magnitude;

            if (dist > _rangedAttackRange)
            {
                transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, _moveSpeed * Time.deltaTime);
                LookAtTarget();
                SetRunState(true);
            }
            else
            {
                SetRunState(false);
                LookAtTarget();
                if (!_isAttackingLocal)
                {
                    AttackRoutineRanged(_target).Forget();
                }
            }
        }

        private async UniTaskVoid AttackRoutineRanged(BaseTower target)
        {
            _isAttackingLocal = true;

            if (_animator != null)
            {
                if (HasAnimatorBool("isRunning"))
                    _animator.SetBool("isRunning", false);
                _animator.SetTrigger(EMinionAnimationType.Magic.ToString());
            }

            SpellProjectile proj = null;

            proj = await ObjectPool.GetObjectAsync<SpellProjectile>(_projectileStart ?? transform);

            if (proj == null)
            {
                Debug.LogWarning("MageController: SpellProjectile not found in pool.");
            }
            else
            {
                Vector3 startPos = (_projectileStart ?? transform).position;
                Vector3 targetPos = target.transform.position;
                var targetCollider = target.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    targetPos = targetCollider.ClosestPoint(startPos);
                }

                targetPos.y += _targetAimHeight;

                proj.Launch(_projectileStart ?? transform, target.transform, _projectileSpeed, null, _damage, gameObject.tag, gameObject);

                Debug.DrawLine(startPos, targetPos, Color.cyan, 0.5f);
            }

            int ms = Mathf.Max(0, (int)(_attackCooldown * 1000f));
            await UniTask.Delay(ms);

            _isAttackingLocal = false;
        }

        private void LookAtTarget()
        {
            if (_target == null) return;
            var look = _target.transform.position - transform.position;
            look.y = 0;
            if (look.sqrMagnitude > 0.001f)
            {
                var rot = Quaternion.LookRotation(look);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10f * Time.deltaTime);
            }
        }

        private bool HasAnimatorBool(string name)
        {
            if (_animator == null) return false;
            var pars = _animator.parameters;
            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i].type == AnimatorControllerParameterType.Bool && pars[i].name == name)
                    return true;
            }
            return false;
        }

        private void SetRunState(bool running)
        {
            base.SetRunState(running);
        }
    }
}