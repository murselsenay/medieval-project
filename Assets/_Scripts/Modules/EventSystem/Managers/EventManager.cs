using UnityEngine;
using Modules.Economy.Enums;
using Modules.WaveSystem.Models;
using Components.Minions.Controllers;
using Components.Tiles.Controllers; // added for HexagonController

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

        // Minion-related delegates
        public delegate void MinionEvent(MinionController minion);

        // Tower click delegate
        public delegate void TowerClicked(int towerId);
        public delegate void TowerUpgraded(int towerId);

        // Hexagon selected delegate
        public delegate void HexagonSelected(HexagonController hex);
        // Hexagon deselected delegate
        public delegate void HexagonDeselected(HexagonController hex);
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

        // Minion-related events
        public static event MinionEvent OnMinionSpawned;
        public static event MinionEvent OnMinionDied;

        // Tower clicked event
        public static event TowerClicked OnTowerClicked;
        public static event TowerUpgraded OnTowerUpgraded;

        // Hexagon selected event
        public static event HexagonSelected OnHexagonSelected;
        // Hexagon deselected event
        public static event HexagonDeselected OnHexagonDeselected;
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

        // Minion-related delegators
        public static void DelegateMinionSpawned(MinionController minion)
        {
            OnMinionSpawned?.Invoke(minion);
        }

        public static void DelegateMinionDied(MinionController minion)
        {
            OnMinionDied?.Invoke(minion);
        }

        // Tower clicked delegator
        public static void DelegateTowerClicked(int towerId)
        {
            OnTowerClicked?.Invoke(towerId);
        }
        public static void DelegateTowerUpgraded(int towerId)
        {
            OnTowerUpgraded?.Invoke(towerId);
        }

        // Hexagon selected delegator
        public static void DelegateHexagonSelected(HexagonController hex)
        {
            OnHexagonSelected?.Invoke(hex);
        }

        // Hexagon deselected delegator
        public static void DelegateHexagonDeselected(HexagonController hex)
        {
            OnHexagonDeselected?.Invoke(hex);
        }
        #endregion
    }
}
