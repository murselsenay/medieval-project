using UnityEngine;
using Modules.Economy.Enums;
using Modules.PopupSystem.Components;
using System;

namespace Modules.EventSystem.Managers
{
    public static class EventManager
    {
        #region Delegates
        public delegate void SpawnCurrencyRequest(Transform spawnPoint, ECurrencyType type, int amount);
        public delegate void CurrencyChanged(ECurrencyType type, int amount);
        public delegate void CurrencyRequestCompleted(ECurrencyType type);
        public delegate void PopupEvent(BasePopup popup);
        #endregion

        #region Events
        public static event SpawnCurrencyRequest OnSpawnCurrencyRequest;
        public static event CurrencyChanged OnCurrencyChanged;
        public static event CurrencyRequestCompleted OnCurrencyRequestCompleted;
        public static event PopupEvent OnPopupShowed;
        public static event PopupEvent OnPopupClosed;

        // New timer tick event
        public static event Action<long> OnTimerTick;

        // Job assignment events
        public static event Action<Modules.JobSystem.Models.Job> OnJobAssigned;
        public static event Action<Modules.JobSystem.Models.Job> OnJobUnassigned;
        #endregion

        #region Methods
        public static void DelegateSpawnCurrencyRequest(Transform spawnPoint, ECurrencyType type, int amount)
        {
            OnSpawnCurrencyRequest?.Invoke(spawnPoint, type, amount);
        }

        public static void DelegateCurrencyChanged(ECurrencyType type, int amount)
        {
            OnCurrencyChanged?.Invoke(type, amount);
        }

        public static void DelegateCurrencyRequestCompleted(ECurrencyType type)
        {
            OnCurrencyRequestCompleted?.Invoke(type);
        }

        public static void DelegatePopupShowed(BasePopup popup)
        {
            OnPopupShowed?.Invoke(popup);
        }

        public static void DelegatePopupClosed(BasePopup popup)
        {
            OnPopupClosed?.Invoke(popup);
        }

        public static void TriggerTimerTick(long unixTime)
        {
            try
            {
                OnTimerTick?.Invoke(unixTime);
            }
            catch { }
        }

        public static void TriggerJobAssigned(Modules.JobSystem.Models.Job job)
        {
            try
            {
                OnJobAssigned?.Invoke(job);
            }
            catch { }
        }

        public static void TriggerJobUnassigned(Modules.JobSystem.Models.Job job)
        {
            try
            {
                OnJobUnassigned?.Invoke(job);
            }
            catch { }
        }
        #endregion
    }
}
