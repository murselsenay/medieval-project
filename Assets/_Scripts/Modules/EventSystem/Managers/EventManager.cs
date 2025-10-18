using UnityEngine;
using Modules.Economy.Enums;
using Modules.WaveSystem.Models;

namespace Modules.EventSystem.Managers
{
    public static class EventManager
    {
        #region Delegates
        public delegate void SpawnCurrencyRequest(Transform spawnPoint, ECurrencyType type, int amount);
        public delegate void CurrencyChanged(ECurrencyType type, int amount);
        public delegate void CurrencyRequestCompleted(ECurrencyType type);

        // Wave-related delegates
        public delegate void PreparePhaseStarted(int waveIndex, WaveDefinition wave);
        public delegate void PreparePhaseEnded(int waveIndex, WaveDefinition wave);
        public delegate void WaveStarted(int waveIndex, WaveDefinition wave);
        public delegate void WaveCompleted(int waveIndex, WaveDefinition wave);
        public delegate void AllWavesCompleted();

        // Tower click delegate
        public delegate void TowerClicked(int towerId);
        #endregion

        #region Events
        public static event SpawnCurrencyRequest OnSpawnCurrencyRequest;
        public static event CurrencyChanged OnCurrencyChanged;
        public static event CurrencyRequestCompleted OnCurrencyRequestCompleted;

        // Wave-related events
        public static event PreparePhaseStarted OnPreparePhaseStarted;
        public static event PreparePhaseEnded OnPreparePhaseEnded;
        public static event WaveStarted OnWaveStarted;
        public static event WaveCompleted OnWaveCompleted;
        public static event AllWavesCompleted OnAllWavesCompleted;

        // Tower clicked event
        public static event TowerClicked OnTowerClicked;
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

        // Wave-related delegate callers
        public static void DelegatePreparePhaseStarted(int waveIndex, WaveDefinition wave)
        {
            OnPreparePhaseStarted?.Invoke(waveIndex, wave);
        }

        public static void DelegatePreparePhaseEnded(int waveIndex, WaveDefinition wave)
        {
            OnPreparePhaseEnded?.Invoke(waveIndex, wave);
        }

        public static void DelegateWaveStarted(int waveIndex, WaveDefinition wave)
        {
            OnWaveStarted?.Invoke(waveIndex, wave);
        }

        public static void DelegateWaveCompleted(int waveIndex, WaveDefinition wave)
        {
            OnWaveCompleted?.Invoke(waveIndex, wave);
        }

        public static void DelegateAllWavesCompleted()
        {
            OnAllWavesCompleted?.Invoke();
        }

        // Tower clicked delegator
        public static void DelegateTowerClicked(int towerId)
        {
            OnTowerClicked?.Invoke(towerId);
        }
        #endregion
    }
}
