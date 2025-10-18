using Modules.ObjectPoolSystem;
using System.Collections;
using UnityEngine;
using Components.Minions.Enums;
using Components.Towers.Controllers;
using Components.Minions.Models;
using Modules.RewardSystem.Managers;
using Modules.EventSystem.Managers;
using Modules.Economy.Enums;

namespace Components.Minions.Controllers
{
    public class MinionController : BaseObject
    {
        [Header("Minion Settings")]
        [SerializeField] protected EMinionType _minionType = EMinionType.Barbarian;
        [SerializeField] protected int _maxHealth = 100;
        [SerializeField] protected int _damage = 10;
        [SerializeField] protected float _moveSpeed = 2f;
        [SerializeField] protected float _attackRange = 1.5f;
        [SerializeField] protected float _attackCooldown = 1.25f;

        [Header("Reward")]
        [SerializeField] private int _rewardAmount = 1;
        [SerializeField] private ECurrencyType _rewardCurrency = ECurrencyType.Gold;

        [Header("References")]
        [SerializeField] protected Animator _animator;

        protected BaseTower _target;
        private int _currentHealth;
        private bool _isAttacking;
        private bool _isDead;

        private bool _runningTriggered;

        public EMinionType MinionType => _minionType;
        public Animator Animator => _animator;
        public int Damage => _damage;
        public bool IsDead => _isDead;

        protected override void Awake()
        {
            base.Awake();
            _currentHealth = _maxHealth;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _isDead = false;
            _isAttacking = false;
            _runningTriggered = false;
        }

        private void Update()
        {
            if (_isDead) return;

            if (_target == null)
            {
                SetRunState(false);
                return;
            }

            // ignore vertical difference when calculating distance so spawn height doesn't delay attacks
            var dir = _target.transform.position - transform.position;
            dir.y = 0;
            var dist = dir.magnitude;

            if (dist > _attackRange)
            {
                // move towards target position but keep current Y to avoid vertical drifting
                var targetPos = _target.transform.position;
                targetPos.y = transform.position.y;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
                LookAtTarget();
                SetRunState(true);
            }
            else
            {
                // Start attacking immediately when in range. Avoid forcing an Idle state first which
                // can introduce animator transition delays (Exit Time). If already attacking, keep run false.
                if (!_isAttacking)
                {
                    StartCoroutine(AttackRoutine());
                }
                else
                {
                    SetRunState(false);
                }
            }
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

        public void SetTarget(BaseTower target)
        {
            _target = target;
            if (_target != null)
            {
                var dir = _target.transform.position - transform.position;
                dir.y = 0;
                Debug.Log($"MinionController: SetTarget to {_target.name} at horizontal distance {dir.magnitude:F2}");
            }
            else
            {
                Debug.Log("MinionController: SetTarget null");
            }
        }

        public void Initialize(MinionData data)
        {
            _minionType = data.MinionType;

            _maxHealth = Mathf.Max(1, Mathf.RoundToInt(data.Health));
            _currentHealth = _maxHealth;
            _damage = data.Damage;
        }

        private IEnumerator AttackRoutine()
        {
            if (_isDead) yield break;

            _isAttacking = true;

            Debug.Log($"MinionController: AttackRoutine started for {name} at time {Time.time:F2}");

            var attackAnim = GetAttackAnimationForType(_minionType);

            // Clear running flags and ensure animator uses trigger-based attack to avoid long idle->attack transitions
            if (_animator != null)
            {
                // If animator uses an "isRunning" bool, clear it so transitions to Idle don't block
                if (HasAnimatorBool("isRunning"))
                    _animator.SetBool("isRunning", false);

                // reset run triggers/flags
                _animator.ResetTrigger(EMinionAnimationType.Run_A.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Run_B.ToString());
                _runningTriggered = false;

                // Trigger attack immediately (use trigger similar to MageController)
                _animator.SetTrigger(attackAnim.ToString());
            }
            else
            {
                PlayAnimation(attackAnim);
            }

            if (_target != null && !_isDead)
            {
                Debug.Log($"MinionController: Dealing {_damage} damage to {_target.name}");
                _target.TakeDamage(_damage);
            }

            yield return new WaitForSeconds(_attackCooldown);

            if (_isDead)
            {
                _isAttacking = false;
                yield break;
            }

            _isAttacking = false;

            // After finishing attack, ensure minion returns to Idle if still in range
            if (_target != null && !_isDead)
            {
                var postDir = _target.transform.position - transform.position;
                postDir.y = 0;
                if (postDir.magnitude <= _attackRange)
                {
                    if (_animator != null)
                    {
                        // use trigger for idle
                        _animator.SetTrigger(EMinionAnimationType.Idle.ToString());
                    }
                    else
                    {
                        PlayAnimation(EMinionAnimationType.Idle);
                    }
                }
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

        private EMinionAnimationType GetAttackAnimationForType(EMinionType type)
        {
            switch (type)
            {
                case EMinionType.Archer:
                    return EMinionAnimationType.Ranged;
                case EMinionType.Mage:
                    return EMinionAnimationType.Magic;
                case EMinionType.Knight:
                case EMinionType.Barbarian:
                default:
                    return EMinionAnimationType.Melee;
            }
        }

        public void TakeDamage(int amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            // Stop all ongoing behaviours (e.g., AttackRoutine) to prevent them from forcing Idle
            StopAllCoroutines();
            _isAttacking = false;
            _runningTriggered = false;

            // Clear any animator triggers / booleans that could transition back to Idle or Run
            if (_animator != null)
            {
                if (HasAnimatorBool("isRunning"))
                    _animator.SetBool("isRunning", false);

                _animator.ResetTrigger(EMinionAnimationType.Idle.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Run_A.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Run_B.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Melee.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Magic.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Ranged.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Hit_A.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Hit_B.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Cheer.ToString());
            }

            // Play one of the death animations and remain in that state until deactivation
            PlayAnimation(Random.value > 0.5f ? EMinionAnimationType.Death_A : EMinionAnimationType.Death_B);

            if (_rewardAmount > 0)
            {
                RewardManager.QueueCurrency(_rewardCurrency, _rewardAmount);
                EventManager.DelegateSpawnCurrencyRequest(transform, _rewardCurrency, _rewardAmount);
            }

            StartCoroutine(DelayedDeactivate(2f));
        }

        private IEnumerator DelayedDeactivate(float delay)
        {
            yield return new WaitForSeconds(delay);
            Deactivate();
        }

        private void PlayAnimation(EMinionAnimationType animType)
        {
            if (_animator == null) return;

            // Run animations use triggers to prevent repeated retriggering
            if (animType == EMinionAnimationType.Run_A || animType == EMinionAnimationType.Run_B)
            {
                if (_runningTriggered) return;
                // reset Idle trigger to avoid conflicting transitions
                _animator.ResetTrigger(EMinionAnimationType.Idle.ToString());
                _animator.SetTrigger(animType.ToString());
                _runningTriggered = true;
                return;
            }

            // Non-run animations: try Play for immediate transition then CrossFade fallback and clear running flag
            _runningTriggered = false;
            try
            {
                _animator.Play(animType.ToString());
            }
            catch
            {
                try { _animator.CrossFade(animType.ToString(), 0f); }
                catch { _animator.SetTrigger(animType.ToString()); }
            }
        }

        protected void SetRunState(bool running)
        {
            if (_animator == null || _isDead)
            {
                return;
            }

            if (running)
            {
                if (!_runningTriggered)
                {
                    PlayAnimation(EMinionAnimationType.Run_A);
                    _runningTriggered = true;
                }
            }
            else
            {
                if (_runningTriggered)
                {
                    PlayAnimation(EMinionAnimationType.Idle);
                    _runningTriggered = false;
                }
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();
            StopAllCoroutines();
            _target = null;
            _isAttacking = false;
            _isDead = false;
            _currentHealth = _maxHealth;
            _runningTriggered = false;
        }
    }
}

