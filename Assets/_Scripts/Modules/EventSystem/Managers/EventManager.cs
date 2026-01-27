using UnityEngine;
using Modules.Economy.Enums;
using Modules.PopupSystem.Components;
using System;

namespace Modules.EventSystem.Managers
{
    public static class EventManager
    {
        #region Events
        public static event Action<Transform, ECurrencyType, int> OnSpawnCurrencyRequest;
        public static event Action<ECurrencyType, int> OnCurrencyChanged;
        public static event Action<ECurrencyType> OnCurrencyRequestCompleted;
        public static event Action<BasePopup> OnPopupShowed;
        public static event Action<BasePopup> OnPopupClosed;

        // New timer tick event
        public static event Action<long> OnTimerTick;
        // Movement input events
        public static event Action<Vector3, float> OnMoveInput; // direction (world XZ), magnitude [0..1]
        public static event Action OnMoveStarted;
        public static event Action OnMoveEnded;
        // Enemy animation / control events
        public static event Action<Transform, float> OnEnemySetSpeed; // enemy transform, normalized speed 0..1
        public static event Action<Transform> OnEnemySpotted; // enemy transform
        public static event Action<Transform> OnEnemySpottedCleared; // enemy transform
        #endregion

        #region Methods
        public static void DelegateSpawnCurrencyRequest(Transform spawnPoint, ECurrencyType type, int amount)
        {
            try
            {
                OnSpawnCurrencyRequest?.Invoke(spawnPoint, type, amount);
            }
            catch { }
        }

        public static void DelegateCurrencyChanged(ECurrencyType type, int amount)
        {
            try
            {
                OnCurrencyChanged?.Invoke(type, amount);
            }
            catch { }
        }

        public static void DelegateCurrencyRequestCompleted(ECurrencyType type)
        {
            try
            {
                OnCurrencyRequestCompleted?.Invoke(type);
            }
            catch { }
        }

        public static void DelegatePopupShowed(BasePopup popup)
        {
            try
            {
                OnPopupShowed?.Invoke(popup);
            }
            catch { }
        }

        public static void DelegatePopupClosed(BasePopup popup)
        {
            try
            {
                OnPopupClosed?.Invoke(popup);
            }
            catch { }
        }

        public static void DelegateTimerTick(long unixTime)
        {
            try
            {
                OnTimerTick?.Invoke(unixTime);
            }
            catch { }
        }

        public static void DelegateMoveInput(Vector3 direction, float magnitude)
        {
            try
            {
                OnMoveInput?.Invoke(direction, magnitude);
            }
            catch { }
        }

        public static void DelegateMoveStarted()
        {
            try
            {
                OnMoveStarted?.Invoke();
            }
            catch { }
        }

        public static void DelegateMoveEnded()
        {
            try
            {
                OnMoveEnded?.Invoke();
            }
            catch { }
        }

        public static void DelegateEnemySetSpeed(Transform enemy, float normalized)
        {
            try
            {
                OnEnemySetSpeed?.Invoke(enemy, normalized);
            }
            catch { }
        }

        public static void DelegateEnemySpotted(Transform enemy)
        {
            try
            {
                OnEnemySpotted?.Invoke(enemy);
            }
            catch { }
        }

        public static void DelegateEnemySpottedCleared(Transform enemy)
        {
            try
            {
                OnEnemySpottedCleared?.Invoke(enemy);
            }
            catch { }
        }
        #endregion
    }
}
