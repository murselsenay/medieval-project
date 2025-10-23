using Components.Tiles.Controllers;
using Components.Tiles.Enums;
using Components.Constants;
using Cysharp.Threading.Tasks;
using Modules.ObjectPoolSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using Components.Towers.Controllers;
using Components.Minions.Models;
using Components.Minions.Enums;
using TMPro;
using DG.Tweening;
using Modules.WaveSystem;
using Modules.EventSystem.Managers;
using System;
using Components.Projectiles.Controllers;
using Modules.RewardSystem.Models;
using Modules.WaveSystem.Models;
using Modules.WaveSystem.Managers;
using Utilities;
using Modules.GameState.Managers;
using Modules.GameState.Enums;

namespace Components.Minions.Controllers
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] private HexagonGridController _hexagonGridController;
        [SerializeField] private Transform _minionParent;
        [SerializeField] private int _spawnCount = 5;
        [SerializeField] private int _spawnDelayMs = 200;
        [Header("UI Components")]
        [SerializeField] private TMP_Text _waveText;
        [SerializeField] private Transform _wave;
        [SerializeField] private TMP_Text _waveDurationText;
        [SerializeField] private ButtonAnimate _startWaveButton;

        private void Initialize()
        {
            SetButtons();

            WaveManager.Initialize(this);
        }
        private async void StartWave()
        {
            _wave.transform.localScale = Vector3.zero;

            try
            {
                ObjectPool.ReturnAllActiveOfType<ArrowProjectile>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WaveController: failed to clean projectiles before wave start: {ex.Message}");
            }

            try
            {
                // Ensure no leftover area projectiles finish at the start of a new wave
                ObjectPool.ReturnAllActiveOfType<AreaProjectile>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WaveController: failed to clean area projectiles before wave start: {ex.Message}");
            }

            // Also clear any stale per-hex minion references
            ClearAllHexMinionLists();

            // change game state to InWave so UI and interactions are updated
            GameStateManager.SetState(EGameState.InWave);

            WaveManager.StartAllWavesAsync();

            WaveManager.RequestStartWave();

            _startWaveButton.gameObject.SetActive(false);
        }

        private void SetButtons()
        {
            _startWaveButton.gameObject.SetActive(true);

            _startWaveButton.RemoveAllListeners();
            _startWaveButton.AddListener(StartWave);
        }


        private async void ShowWaveInformation(string text)
        {
            _waveText.text = text;
            _wave.transform.localScale = Vector3.zero;
            _wave.DOScale(1, 0.5f).SetEase(Ease.OutBack);
            await UniTask.Delay(3000);
            _wave.DOScale(0, 0.5f).SetEase(Ease.InBack);
        }

        private void ClearAllHexMinionLists()
        {
            if (_hexagonGridController == null || _hexagonGridController.Hexagons == null) return;
            var hexes = _hexagonGridController.Hexagons;
            for (int i = 0; i < hexes.Count; i++)
            {
                var hex = hexes[i];
                if (hex == null) continue;
                var copy = new List<MinionController>(hex.MinionsOnHex);
                for (int m = 0; m < copy.Count; m++)
                {
                    hex.RemoveMinion(copy[m]);
                }
            }
        }

        public async UniTask SpawnWaveAsync(SubWave sub)
        {
            if (_hexagonGridController == null) return;
            if (sub.Minions == null || sub.Minions.Count == 0) return;

            var flat = new List<SubWaveMinion>();
            for (int i = 0; i < sub.Minions.Count; i++)
            {
                var entry = sub.Minions[i];
                for (int c = 0; c < entry.Count; c++) flat.Add(entry);
            }


            if (flat.Count == 0) return;

            var hexes = _hexagonGridController.Hexagons;
            if (hexes == null || hexes.Count == 0) return;

            var spawnHexes = hexes.Where(h => h != null && h.State == EHexagonState.Spawn).ToList();
            if (spawnHexes.Count == 0) return;

            var rnd = new System.Random();
            spawnHexes = spawnHexes.OrderBy(x => rnd.Next()).ToList();
            flat = flat.OrderBy(x => rnd.Next()).ToList();

            int toSpawn = Mathf.Min(flat.Count, spawnHexes.Count);

            for (int i = 0; i < toSpawn; i++)
            {
                var hex = spawnHexes[i];
                if (hex == null) continue;

                var entry = flat[i];

                MinionController minion = null;

                switch (entry.Data.MinionType)
                {
                    case EMinionType.Barbarian:
                        minion = await ObjectPool.GetObjectAsync<BarbarianController>(_minionParent, entry.Key);
                        break;
                    case EMinionType.Archer:
                        minion = await ObjectPool.GetObjectAsync<ArcherController>(_minionParent, entry.Key);
                        break;
                    case EMinionType.Mage:
                        minion = await ObjectPool.GetObjectAsync<MageController>(_minionParent, entry.Key);
                        break;
                    case EMinionType.Knight:
                        minion = await ObjectPool.GetObjectAsync<KnightController>(_minionParent, entry.Key);
                        break;
                    default:
                        minion = await ObjectPool.GetObjectAsync<MinionController>(_minionParent, entry.Key);
                        break;
                }

                if (minion == null) continue;

                minion.Initialize(entry.Data);

                // Notify that a minion was spawned so WaveManager can track active minion count
                try
                {
                    Modules.EventSystem.Managers.EventManager.DelegateMinionSpawned(minion);
                }
                catch { }

                minion.transform.position = CalculateSpawnPosition(hex, minion);

                var towerGo = FindNearestTower(minion.transform);
                if (towerGo != null)
                {
                    var towerComp = towerGo.GetComponent<BaseTower>();
                    if (towerComp != null)
                        minion.SetTarget(towerComp);
                }

                await UniTask.Delay(_spawnDelayMs);
            }
        }

        private Vector3 CalculateSpawnPosition(HexagonController hex, MinionController minion)
        {
            Vector3 basePos = hex.transform.position;

            float hexHalf = 0.0f;
            var hexCol = hex.GetComponent<Collider>();
            if (hexCol != null)
            {
                hexHalf = hexCol.bounds.extents.y;
            }
            else
            {
                var hexR = hex.GetComponentInChildren<Renderer>();
                if (hexR != null) hexHalf = hexR.bounds.extents.y;
            }

            float minionHalf = 0.5f;
            var minCol = minion.GetComponent<Collider>();
            if (minCol != null)
            {
                minionHalf = minCol.bounds.extents.y;
            }
            else
            {
                var minR = minion.GetComponentInChildren<Renderer>();
                if (minR != null) minionHalf = minR.bounds.extents.y;
            }

            const float margin = 0.02f;
            return basePos + Vector3.up * (hexHalf + minionHalf + margin);
        }

        private GameObject FindNearestTower(Transform from)
        {
            if (from == null) return null;

            var towers = GameObject.FindGameObjectsWithTag(GameObjectTags.Tower);
            if (towers == null || towers.Length == 0) return null;

            GameObject best = null;
            float bestDist = float.MaxValue;
            foreach (var t in towers)
            {
                if (t == null) continue;
                float d = Vector3.SqrMagnitude(t.transform.position - from.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }

            return best;
        }
        private void OnWaveStarted(int waveIndex, WaveDefinition wave)
        {
            ShowWaveInformation($"Wave {waveIndex + 1}");
            // Clean any stale hex-minion mapping just in case
            ClearAllHexMinionLists();
        }

        private void OnWaveCompleted(int waveIndex, WaveDefinition wave)
        {
            ShowWaveInformation($"Wave Ended");
            _startWaveButton.gameObject.SetActive(true);

            // set state to Build so player can build
            GameStateManager.SetState(EGameState.Build);
        }
        private void OnEnable()
        {
            Initialize();

            EventManager.OnWaveStarted += OnWaveStarted;
            EventManager.OnWaveCompleted += OnWaveCompleted;

            // subscribe to game state changes to update UI
            GameStateManager.OnStateChanged += OnGameStateChanged;

            // ensure button visibility matches current state
            OnGameStateChanged(GameStateManager.CurrentState);
        }
        private void OnDisable()
        {
            EventManager.OnWaveStarted -= OnWaveStarted;
            EventManager.OnWaveCompleted -= OnWaveCompleted;

            GameStateManager.OnStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(EGameState state)
        {
            // hide start wave button while in wave
            if (_startWaveButton != null)
            {
                _startWaveButton.gameObject.SetActive(state != EGameState.InWave);
            }
        }
    }
}
