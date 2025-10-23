using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Components.Minions.Enums;
using Components.Towers.Controllers;
using Components.Minions.Models;
using Modules.RewardSystem.Managers;
using Modules.EventSystem.Managers;
using Modules.Economy.Enums;
using Components.Tiles.Controllers;

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

        // Track current hex the minion stands on
        private HexagonController _currentHex;
        public HexagonController CurrentHex => _currentHex;

        // Track which hexagons are currently affecting this minion (legacy)
        private readonly List<HexagonController> _affectedHexes = new List<HexagonController>();
        public IReadOnlyList<HexagonController> AffectedHexes => _affectedHexes.AsReadOnly();

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
            _affectedHexes.Clear();
            _currentHex = null;
        }

        private void Update()
        {
            if (_isDead) return;

            // Target yoksa veya deaktif/ölmüþse yeni bir tower bul
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                // Sahnede aktif towerlarý bul
                var towers = GameObject.FindGameObjectsWithTag("Tower");
                BaseTower newTarget = null;
                float minDist = float.MaxValue;
                foreach (var t in towers)
                {
                    if (t == null) continue;
                    var bt = t.GetComponent<BaseTower>();
                    if (bt == null || !bt.gameObject.activeInHierarchy) continue;
                    // Minion ölü towerlara saldýrmasýn
                    var towerDeadProp = bt.GetType().GetProperty("IsDead");
                    bool isDead = false;
                    if (towerDeadProp != null)
                    {
                        isDead = (bool)towerDeadProp.GetValue(bt);
                    }
                    if (isDead) continue;
                    float dist = (bt.transform.position - transform.position).sqrMagnitude;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        newTarget = bt;
                    }
                }
                if (newTarget != null)
                {
                    SetTarget(newTarget);
                }
                else
                {
                    // Hiç tower yoksa Victory fonksiyonunu çaðýr
                    Victory();
                    SetRunState(false);
                    return;
                }
            }

            var dir = _target.transform.position - transform.position;
            dir.y = 0;
            var distToTarget = dir.magnitude;

            if (distToTarget > _attackRange)
            {
                var targetPos = _target.transform.position;
                targetPos.y = transform.position.y;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
                LookAtTarget();
                SetRunState(true);
            }
            else
            {
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

        // Minion zafer fonksiyonu: animasyonu idle'a geçirir, ileride doldurulabilir
        private void Victory()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(EMinionAnimationType.Idle.ToString());
            }
            else
            {
                PlayAnimation(EMinionAnimationType.Idle);
            }
            // TODO: Victory davranýþý ileride eklenecek
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

        private void OnTriggerEnter(Collider other)
        {
            // detect if we entered a hex's trigger and register
            var hex = other.GetComponentInParent<HexagonController>();
            if (hex != null)
            {
                // unregister from previous hex
                if (_currentHex != null && _currentHex != hex)
                {
                    _currentHex.RemoveMinion(this);
                }

                _currentHex = hex;
                _currentHex.AddMinion(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var hex = other.GetComponentInParent<HexagonController>();
            if (hex != null && _currentHex == hex)
            {
                _currentHex.RemoveMinion(this);
                _currentHex = null;
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

            if (_animator != null)
            {
                if (HasAnimatorBool("isRunning"))
                    _animator.SetBool("isRunning", false);

                _animator.ResetTrigger(EMinionAnimationType.Run_A.ToString());
                _animator.ResetTrigger(EMinionAnimationType.Run_B.ToString());
                _runningTriggered = false;

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

            if (_target != null && !_isDead)
            {
                var postDir = _target.transform.position - transform.position;
                postDir.y = 0;
                if (postDir.magnitude <= _attackRange)
                {
                    if (_animator != null)
                    {
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

        // Add or remove affected hex tracking (legacy kept)
        public void AddAffectedHex(HexagonController hex, float duration = 2f)
        {
            if (hex == null) return;
            if (!_affectedHexes.Contains(hex))
                _affectedHexes.Add(hex);

            // schedule removal after duration
            StartCoroutine(RemoveAffectedHexAfterDelay(hex, duration));
        }

        public void RemoveAffectedHex(HexagonController hex)
        {
            if (hex == null) return;
            _affectedHexes.Remove(hex);
        }

        private IEnumerator RemoveAffectedHexAfterDelay(HexagonController hex, float delay)
        {
            if (hex == null) yield break;
            yield return new WaitForSeconds(delay);
            _affectedHexes.Remove(hex);
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            StopAllCoroutines();
            _isAttacking = false;
            _runningTriggered = false;

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

            PlayAnimation(Random.value > 0.5f ? EMinionAnimationType.Death_A : EMinionAnimationType.Death_B);

            if (_rewardAmount > 0)
            {
                RewardManager.QueueCurrency(_rewardCurrency, _rewardAmount);
                EventManager.DelegateSpawnCurrencyRequest(transform, _rewardCurrency, _rewardAmount);
            }

            try
            {
                EventManager.DelegateMinionDied(this);
            }
            catch { }

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

            if (animType == EMinionAnimationType.Run_A || animType == EMinionAnimationType.Run_B)
            {
                if (_runningTriggered) return;
                // reset Idle trigger to avoid conflicting transitions
                _animator.ResetTrigger(EMinionAnimationType.Idle.ToString());
                _animator.SetTrigger(animType.ToString());
                _runningTriggered = true;
                return;
            }

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
            _affectedHexes.Clear();
            if (_currentHex != null) { _currentHex.RemoveMinion(this); _currentHex = null; }
        }
    }
}

