using System;
using System.Collections.Generic;
using Components.Towers.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Components.Constants;
using Components.Towers.Controllers;
using Modules.ObjectPoolSystem;
using Modules.Economy.Managers;
using Modules.Economy.Enums;
using Modules.EventSystem.Managers;
using Components.Towers.Enums;

namespace Modules.TowerSystem.Managers
{
    public static class TowerManager
    {
        private static readonly Dictionary<int, int> _towerLevels = new Dictionary<int, int>();
        private static readonly Dictionary<int, TowerProgressionData> _progressions = new Dictionary<int, TowerProgressionData>();
        private static readonly Dictionary<int, TowerData> _towerBaseDatas = new Dictionary<int, TowerData>();
        private static TowerProgressionData _defaultProgression;
        private static bool _hasDefaultProgression = false;

        // default buildable tower datas (by type)
        private static readonly Dictionary<ETowerType, TowerData> _defaultTowerDatas = new Dictionary<ETowerType, TowerData>();
        private static bool _hasDefaultTowerDatas = false;

        public static void InitializeDefaults()
        {
            _defaultProgression = new TowerProgressionData(
                maxLevel: 5,
                healthPerLevel: 50f,
                damagePerLevel: 2f,
                rangePerLevel: 0f,
                cooldownChangePerLevel: -0.1f,
                projectileSpeedPerLevel: 0f
            );
            _hasDefaultProgression = true;

            InitializeDefaultTowerDatas();
        }

        private static void InitializeDefaultTowerDatas()
        {
            if (_hasDefaultTowerDatas) return;

            // Main tower
            var main = new TowerData(
                ETowerType.Main,
                health: 300f,
                projectileType: Components.Projectiles.Enums.EProjectileType.Arrow,
                projectileDamage: 50f,
                projectileRange: 5f,
                projectileCooldown: 1f,
                projectileSpeed: 15f,
                buildingCost: 12
            );

            // Area tower
            var area = new TowerData(
                ETowerType.Area,
                health: 150f,
                projectileType: Components.Projectiles.Enums.EProjectileType.Area,
                projectileDamage: 25f,
                projectileRange: 3f,
                projectileCooldown: 0.5f,
                projectileSpeed: 5f,
                buildingCost: 9
            );

            _defaultTowerDatas[ETowerType.Main] = main;
            _defaultTowerDatas[ETowerType.Area] = area;

            _hasDefaultTowerDatas = true;
        }

        public static void SetProgressionForTower(int towerId, TowerProgressionData progression)
        {
            _progressions[towerId] = progression;
        }

        public static void RemoveProgressionForTower(int towerId)
        {
            if (_progressions.ContainsKey(towerId))
                _progressions.Remove(towerId);
            if (_towerBaseDatas.ContainsKey(towerId))
                _towerBaseDatas.Remove(towerId);
            if (_towerLevels.ContainsKey(towerId))
                _towerLevels.Remove(towerId);
        }

        public static int GetLevelForTower(int towerId)
        {
            if (_towerLevels.TryGetValue(towerId, out var lvl)) return lvl;
            return 1;
        }

        public static void SetLevelForTower(int towerId, int level)
        {
            _towerLevels[towerId] = level;
        }

        public static bool CanUpgrade(int towerId)
        {
            int currentLevel = GetLevelForTower(towerId);
            if (!_progressions.TryGetValue(towerId, out var prog))
            {
                if (!_hasDefaultProgression) return false;
                prog = _defaultProgression;
            }

            if (currentLevel >= prog.MaxLevel) return false;

            int cost = GetNextUpgradeCost(towerId);
            if (cost <= 0) return false;
            int playerGold = CurrencyManager.GetAmount(ECurrencyType.Gold);
            return playerGold >= cost;
        }

        public static bool UpgradeTower(int towerId)
        {
            int level = GetLevelForTower(towerId);
            if (!_progressions.TryGetValue(towerId, out var prog))
            {
                if (!_hasDefaultProgression) return false;
                prog = _defaultProgression;
            }

            if (level >= prog.MaxLevel) return false;

            int cost = GetNextUpgradeCost(towerId);
            if (cost > 0)
            {
                if (!CurrencyManager.TryConsume(ECurrencyType.Gold, cost))
                    return false;
            }

            level++;
            _towerLevels[towerId] = level;

            EventManager.DelegateTowerUpgraded(towerId);

            return true;
        }
        public static int GetNextUpgradeCost(int towerId)
        {
            int currentLevel = GetLevelForTower(towerId);

            TowerProgressionData effectiveProg = default;
            if (_progressions.TryGetValue(towerId, out var prog)) effectiveProg = prog;
            else if (_hasDefaultProgression) effectiveProg = _defaultProgression;

            if (!effectiveProg.Equals(default(TowerProgressionData)))
            {
                if (currentLevel >= effectiveProg.MaxLevel) return 0;
            }

            const int baseCost = 3;
            // level1 -> 3, level2 -> 6, etc.
            return baseCost * currentLevel;
        }

        private static float RoundUpToHalf(float value)
        {
            return (float)(Math.Ceiling(value * 2.0) / 2.0);
        }

        private static float ComputeDPS(TowerData data)
        {
            float dps;
            if (data.ProjectileCooldown <= 0f) dps = data.ProjectileDamage;
            else dps = data.ProjectileDamage / data.ProjectileCooldown;
            return RoundUpToHalf(dps);
        }

        public static TowerData GetTowerDataAtLevel(int towerId, int level)
        {
            if (!_towerBaseDatas.TryGetValue(towerId, out var baseData))
            {
                return default;
            }

            if (!_progressions.TryGetValue(towerId, out var prog))
            {
                if (!_hasDefaultProgression)
                {
                    // return rounded base data
                    return new TowerData(
                        baseData.TowerType,
                        RoundUpToHalf(baseData.Health),
                        baseData.ProjectileType,
                        RoundUpToHalf(baseData.ProjectileDamage),
                        RoundUpToHalf(baseData.ProjectileRange),
                        RoundUpToHalf(baseData.ProjectileCooldown),
                        RoundUpToHalf(baseData.ProjectileSpeed),
                        baseData.BuildingCost
                    );
                }
                prog = _defaultProgression;
            }

            int levelDelta = level - 1;
            if (levelDelta <= 0)
            {
                return new TowerData(
                    baseData.TowerType,
                    RoundUpToHalf(baseData.Health),
                    baseData.ProjectileType,
                    RoundUpToHalf(baseData.ProjectileDamage),
                    RoundUpToHalf(baseData.ProjectileRange),
                    RoundUpToHalf(baseData.ProjectileCooldown),
                    RoundUpToHalf(baseData.ProjectileSpeed),
                    baseData.BuildingCost
                );
            }

            // compute raw values
            float health = baseData.Health + prog.HealthPerLevel * levelDelta;
            float damage = baseData.ProjectileDamage + prog.DamagePerLevel * levelDelta;
            float range = baseData.ProjectileRange + prog.RangePerLevel * levelDelta;
            float cooldown = baseData.ProjectileCooldown + prog.CooldownChangePerLevel * levelDelta;
            float speed = baseData.ProjectileSpeed + prog.ProjectileSpeedPerLevel * levelDelta;

            // round each stat up to nearest 0.5
            return new TowerData(
                baseData.TowerType,
                RoundUpToHalf(health),
                baseData.ProjectileType,
                RoundUpToHalf(damage),
                RoundUpToHalf(range),
                RoundUpToHalf(cooldown),
                RoundUpToHalf(speed),
                baseData.BuildingCost
            );
        }

        public static TowerLevelInfo GetCurrentLevelInfo(int towerId)
        {
            if (!_towerBaseDatas.TryGetValue(towerId, out var baseData))
            {
                return new TowerLevelInfo(GetLevelForTower(towerId), default, 0f);
            }

            int level = GetLevelForTower(towerId);
            var data = GetTowerDataAtLevel(towerId, level);
            var dps = ComputeDPS(data);
            return new TowerLevelInfo(level, data, dps);
        }

        public static TowerLevelInfo GetNextLevelInfo(int towerId)
        {
            if (!_towerBaseDatas.TryGetValue(towerId, out var baseData))
            {
                return new TowerLevelInfo(GetLevelForTower(towerId), default, 0f);
            }

            int currentLevel = GetLevelForTower(towerId);

            bool hasProg = _progressions.TryGetValue(towerId, out var prog);
            if (!hasProg && !_hasDefaultProgression)
            {
                return GetCurrentLevelInfo(towerId);
            }

            if (!hasProg) prog = _defaultProgression;
            if (currentLevel >= prog.MaxLevel)
            {
                return GetCurrentLevelInfo(towerId);
            }

            var nextLevel = currentLevel + 1;
            var nextData = GetTowerDataAtLevel(towerId, nextLevel);
            var nextDps = ComputeDPS(nextData);
            return new TowerLevelInfo(nextLevel, nextData, nextDps);
        }

        public static async UniTask<BaseTower> CreateTowerAt(Transform parent, Vector3 position, string poolKey, TowerData baseData, TowerProgressionData? progression = null)
        {
            BaseTower tower = null;
            try
            {
                tower = await ObjectPool.GetObjectAsync<BaseTower>(parent, poolKey);
            }
            catch
            {
            }

            if (tower == null) return null;

            tower.Initialize(baseData);

            tower.gameObject.transform.position = new Vector3(position.x, 1f, position.z);

            try { tower.gameObject.tag = GameObjectTags.Tower; } catch { }

            int id = tower.gameObject.GetInstanceID();

            if (progression.HasValue)
            {
                var prog = progression.Value;
                if (prog.Id != 0)
                {
                    id = prog.Id;
                    SetProgressionForTower(id, prog);
                }
                else
                {
                    prog.Id = id;
                    SetProgressionForTower(id, prog);
                }
            }
            else
            {
                if (!_hasDefaultProgression) InitializeDefaults();
                SetProgressionForTower(id, _defaultProgression);
            }

            tower.TowerId = id;

            _towerBaseDatas[id] = baseData;

            SetLevelForTower(id, 1);

            return tower;
        }

        public static UniTask<BaseTower> CreateTowerAt(Transform parent, Transform hexTransform, string poolKey, TowerData baseData, TowerProgressionData? progression = null)
        {
            if (hexTransform == null) return UniTask.FromResult<BaseTower>(null);
            return CreateTowerAt(parent, hexTransform.position, poolKey, baseData, progression);
        }

        // New: expose default buildable tower datas
        public static IReadOnlyCollection<TowerData> GetBuildableTowers()
        {
            if (!_hasDefaultTowerDatas) InitializeDefaultTowerDatas();
            return _defaultTowerDatas.Values;
        }

        public static bool TryGetDefaultTowerData(ETowerType type, out TowerData data)
        {
            if (!_hasDefaultTowerDatas) InitializeDefaultTowerDatas();
            return _defaultTowerDatas.TryGetValue(type, out data);
        }
    }
}
