using UnityEngine;
using Modules.Economy.Enums;
using Modules.PopupSystem.Components;
using System;
using Modules.ClientSystem.Models;

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

        // Client/Trip job events
        public static event Action<Client, Modules.JobSystem.Models.Job> OnClientJobCreated;
        public static event Action<Client, Modules.JobSystem.Models.Job> OnClientJobAccepted;
        public static event Action<Client, Modules.JobSystem.Models.Job> OnClientJobCancelled;
        public static event Action<Client> OnClientJobRejected;

        // Job lifecycle
        public static event Action<Modules.JobSystem.Models.Job> OnJobCancelled;

        // UI requests
        public static event Action<Client> OnShowTaxisRequested;
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

        public static void DelegateClientJobCreated(Client client, Modules.JobSystem.Models.Job job)
        {
            try
            {
                OnClientJobCreated?.Invoke(client, job);
            }
            catch { }
        }

        public static void DelegateClientJobAccepted(Client client, Modules.JobSystem.Models.Job job)
        {
            try
            {
                OnClientJobAccepted?.Invoke(client, job);
            }
            catch { }
        }

        public static void DelegateClientJobCancelled(Client client, Modules.JobSystem.Models.Job job)
        {
            try
            {
                OnClientJobCancelled?.Invoke(client, job);
            }
            catch { }
        }

        public static void DelegateClientJobRejected(Client client)
        {
            try
            {
                OnClientJobRejected?.Invoke(client);
            }
            catch { }
        }

        public static void DelegateJobCancelled(Modules.JobSystem.Models.Job job)
        {
            try
            {
                OnJobCancelled?.Invoke(job);
            }
            catch { }
        }

        public static void DelegateShowTaxisRequested(Client client)
        {
            try
            {
                OnShowTaxisRequested?.Invoke(client);
            }
            catch { }
        }

        #endregion
    }
}
