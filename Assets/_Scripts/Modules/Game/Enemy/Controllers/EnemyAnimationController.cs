using System.Collections;
using Modules.EventSystem.Managers;
using Modules.Logger;
using UnityEngine;

namespace Modules.Game.Enemy.Controllers
{
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
        private bool _isSpottedPlaying = false;

        private Coroutine _monitorCoroutine;

        public void SetMovementSpeed(float normalized)
        {
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
            _animator.SetFloat(_speedParam, 0f);
        }

        public void TriggerSpotted()
        {
            if (string.IsNullOrEmpty(_spottedBool) || _isSpottedPlaying) return;
            _isSpottedPlaying = true;
            DebugLogger.Log($"[EnemyAnimationController] TriggerSpotted on '{name}'");

            _animator.SetBool(_spottedBool, true);

            int layer = 0;
            try
            {
                _animator.Play(_spottedStateName, layer, 0f);
            }
            catch { }
            if (_monitorCoroutine != null) StopCoroutine(_monitorCoroutine);
            _monitorCoroutine = StartCoroutine(MonitorSpottedState());
            try { EventManager.DelegateEnemySpotted(transform); } catch { }
            EventManager.OnEnemySetSpeed += TempBlockSpeed;
        }

        private void TempBlockSpeed(Transform enemy, float speed)
        {
            if (enemy != transform) return;
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
            EnableMovementUpdates(true);
        }

        private IEnumerator MonitorSpottedState()
        {
            if (_animator == null) yield break;
            int layer = 0;

            DebugLogger.Log($"[EnemyAnimationController] MonitorSpottedState start for '{name}'");

            float timeout = 1f;
            float t = 0f;
            while (t < timeout)
            {
                var info = _animator.GetCurrentAnimatorStateInfo(layer);
                if (info.IsName(_spottedStateName)) break;
                t += Time.deltaTime;
                yield return null;
            }

            while (true)
            {
                var info = _animator.GetCurrentAnimatorStateInfo(layer);
                if (!info.IsName(_spottedStateName)) break;
                if (info.normalizedTime >= 1f) break;

                yield return null;
            }

            DebugLogger.Log($"[EnemyAnimationController] MonitorSpottedState end for '{name}' - clearing spotted");
            _animator.SetBool(_spottedBool, false);

            try
            {
                ForceSetSpeedZero();
                if (!string.IsNullOrEmpty(_idleStateName))
                {
                    _animator.Play(_idleStateName, layer, 0f);
                    _animator.Update(0f);
                }
            }
            catch { }

            _isSpottedPlaying = false;
            _monitorCoroutine = null;
            try { EventManager.DelegateEnemySpottedCleared(transform); } catch { }
            EventManager.OnEnemySetSpeed -= TempBlockSpeed;
        }
    }
}
