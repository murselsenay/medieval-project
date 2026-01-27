using System.Collections;
using UnityEngine;

namespace Modules.Enemy.Controllers
{
    /// <summary>
    /// Central animation controller for an enemy.
    /// Single responsibility: talk only to Animator and expose simple methods for other systems.
    /// - Set movement speed (normalized 0..1)
    /// - Force set speed zero
    /// - Trigger / monitor spotted state via a bool parameter
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyAnimationController : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private float _speedDamp = 0.1f;

        [Header("Spotted")]
        [SerializeField] private string _spottedBool = "IsSpotted";
        [SerializeField] private string _spottedStateName = "Spotted";
        [SerializeField] private string _idleStateName = "Idle";

        private bool _movementUpdatesEnabled = true;
        private Coroutine _monitorCoroutine;
        private bool _isSpottedPlaying = false;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        public void SetMovementSpeed(float normalized)
        {
            if (_animator == null) return;
            if (!_movementUpdatesEnabled) return;
            _animator.SetFloat(_speedParam, Mathf.Clamp01(normalized), _speedDamp, Time.deltaTime);
        }

        public void EnableMovementUpdates(bool enable)
        {
            _movementUpdatesEnabled = enable;
            if (!enable) ForceSetSpeedZero();
        }

        public void ForceSetSpeedZero()
        {
            if (_animator == null) return;
            _animator.SetFloat(_speedParam, 0f);
        }

        public void TriggerSpotted()
        {
            if (_animator == null || string.IsNullOrEmpty(_spottedBool) || _isSpottedPlaying) return;
            _isSpottedPlaying = true;
            Debug.Log($"[EnemyAnimationController] TriggerSpotted on '{name}'");
            // set bool true and force-play spotted state to avoid AnyState re-entry
            _animator.SetBool(_spottedBool, true);
            // try to immediately play the spotted state on layer 0
            int layer = 0;
            try
            {
                _animator.Play(_spottedStateName, layer, 0f);
            }
            catch { }
            if (_monitorCoroutine != null) StopCoroutine(_monitorCoroutine);
            _monitorCoroutine = StartCoroutine(MonitorSpottedState());
            // notify global event
            try { Modules.EventSystem.Managers.EventManager.DelegateEnemySpotted(transform); } catch { }
            // also temporarily subscribe to block external speed updates for a short window
            Modules.EventSystem.Managers.EventManager.OnEnemySetSpeed += TempBlockSpeed;
        }

        private void TempBlockSpeed(Transform enemy, float speed)
        {
            if (enemy != transform) return;
            // swallow external speed updates while spotted plays
        }

        public void ClearSpotted()
        {
            if (_animator == null || string.IsNullOrEmpty(_spottedBool)) return;
            _animator.SetBool(_spottedBool, false);
            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
                _monitorCoroutine = null;
            }
            // ensure movement updates are re-enabled after manual clear
            EnableMovementUpdates(true);
        }

        private IEnumerator MonitorSpottedState()
        {
            if (_animator == null) yield break;
            int layer = 0;

            Debug.Log($"[EnemyAnimationController] MonitorSpottedState start for '{name}'");

            // wait up to 1s for the animator to transition into the spotted state
            float timeout = 1f;
            float t = 0f;
            while (t < timeout)
            {
                var info = _animator.GetCurrentAnimatorStateInfo(layer);
                if (info.IsName(_spottedStateName)) break;
                t += Time.deltaTime;
                yield return null;
            }

            // wait until the state has played once (normalizedTime >= 1)
            while (true)
            {
                var info = _animator.GetCurrentAnimatorStateInfo(layer);
                if (!info.IsName(_spottedStateName)) break; // left state
                if (info.normalizedTime >= 1f) break; // finished playback
                yield return null;
            }

            // clear flag so animator can transit back
            Debug.Log($"[EnemyAnimationController] MonitorSpottedState end for '{name}' - clearing spotted");
            _animator.SetBool(_spottedBool, false);
            // ensure animator speed is zero and force idle to avoid blend-tree resuming walk
            try
            {
                ForceSetSpeedZero();
                if (!string.IsNullOrEmpty(_idleStateName))
                {
                    _animator.Play(_idleStateName, layer, 0f);
                    // force update one frame to apply
                    _animator.Update(0f);
                }
            }
            catch { }

            _isSpottedPlaying = false;
            _monitorCoroutine = null;
            // notify global event cleared
            try { Modules.EventSystem.Managers.EventManager.DelegateEnemySpottedCleared(transform); } catch { }
            // remove temporary speed blocker
            Modules.EventSystem.Managers.EventManager.OnEnemySetSpeed -= TempBlockSpeed;
        }
    }
}
