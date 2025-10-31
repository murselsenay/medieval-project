using UnityEngine;
using Modules.Economy.Enums;

namespace Modules.EventSystem.Managers
{
    public static class EventManager
    {
        #region Delegates
        public delegate void SpawnCurrencyRequest(Transform spawnPoint, ECurrencyType type, int amount);
        public delegate void CurrencyChanged(ECurrencyType type, int amount);
        public delegate void CurrencyRequestCompleted(ECurrencyType type);
        #endregion

        #region Events
        public static event SpawnCurrencyRequest OnSpawnCurrencyRequest;
        public static event CurrencyChanged OnCurrencyChanged;
        public static event CurrencyRequestCompleted OnCurrencyRequestCompleted;
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
        #endregion
    }
}
