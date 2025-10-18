using System.Collections.Generic;
using Components.Towers.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Components.Constants;
using Components.Towers.Controllers;
using Modules.ObjectPoolSystem;

namespace Modules.TowerSystem.Managers
{
    public static class TowerManager
    {
        // Manage tower progression rules per tower id and track levels per tower id
        private static readonly Dictionary<int, int> _towerLevels = new Dictionary<int, int>();
        private static readonly Dictionary<int, TowerProgressionData> _progressions = new Dictionary<int, TowerProgressionData>();
        private static readonly Dictionary<int, TowerData> _towerBaseDatas = new Dictionary<int, TowerData>();
        private static TowerProgressionData _defaultProgression;
        private static bool _hasDefaultProgression = false;

        // Keep a default progression (optional) for towers that don't have a specific progression set.
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
        }

        // Assign a progression dataset for a specific tower id
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

        // Check if tower can upgrade using its current stored level
        public static bool CanUpgrade(int towerId)
        {
            int currentLevel = GetLevelForTower(towerId);
            if (!_progressions.TryGetValue(towerId, out var prog))
            {
                if (!_hasDefaultProgression) return false;
                prog = _defaultProgression;
            }

            return currentLevel < prog.MaxLevel;
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
            level++;
            _towerLevels[towerId] = level;
            return true;
        }

        // Helper to compute DPS (damage per second) from TowerData
        private static float ComputeDPS(TowerData data)
        {
            // Avoid division by zero; if cooldown is <= 0 use damage as fallback
            if (data.ProjectileCooldown <= 0f) return data.ProjectileDamage;
            return data.ProjectileDamage / data.ProjectileCooldown;
        }

        // Calculates the stat values at a specific level based on stored base data + progression for the specified tower id
        public static TowerData GetTowerDataAtLevel(int towerId, int level)
        {
            if (!_towerBaseDatas.TryGetValue(towerId, out var baseData))
            {
                // No base data registered for this tower id; return an empty/default TowerData
                return default;
            }

            if (!_progressions.TryGetValue(towerId, out var prog))
            {
                if (!_hasDefaultProgression) return baseData;
                prog = _defaultProgression;
            }

            int levelDelta = level - 1;
            if (levelDelta <= 0) return baseData;

            return new TowerData(
                baseData.TowerType,
                baseData.Health + prog.HealthPerLevel * levelDelta,
                baseData.ProjectileType,
                baseData.ProjectileDamage + prog.DamagePerLevel * levelDelta,
                baseData.ProjectileRange + prog.RangePerLevel * levelDelta,
                baseData.ProjectileCooldown + prog.CooldownChangePerLevel * levelDelta,
                baseData.ProjectileSpeed + prog.ProjectileSpeedPerLevel * levelDelta
            );
        }

        // Returns full computed stats for current level including DPS (uses stored base data)
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

        // Returns info for the next level. If already max level or no progression, returns current level info.
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
                // No progression; return current
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

        // Create a tower at a world position. Returns the created BaseTower or null.
        // Towers will use the provided progression.Id if set; otherwise the tower's GameObject instance id is used as the tower id.
        public static async UniTask<BaseTower> CreateTowerAt(Transform parent, Vector3 position, string poolKey, TowerData baseData, TowerProgressionData? progression = null)
        {
            // Ensure object pool is initialized externally.
            BaseTower tower = null;
            try
            {
                tower = await ObjectPool.GetObjectAsync<BaseTower>(parent, poolKey);
            }
            catch
            {
            }

            if (tower == null) return null;

            // Initialize tower data
            tower.Initialize(baseData);

            // Force tower world Y so base sits at Y = 1 (use tower half-height to position pivot)
            tower.gameObject.transform.position = new Vector3(position.x, 1f, position.z);

            // ensure tag
            try { tower.gameObject.tag = GameObjectTags.Tower; } catch { }

            // determine tower id
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
                    // progression provided but no explicit id -> use gameobject id
                    prog.Id = id;
                    SetProgressionForTower(id, prog);
                }
            }
            else
            {
                // No progression supplied: use default progression and register under tower gameobject id
                if (!_hasDefaultProgression) InitializeDefaults();
                SetProgressionForTower(id, _defaultProgression);
            }

            // store progression id on the tower instance for external linking
            tower.TowerId = id;

            // store base data for this tower id so later queries can omit baseData arg
            _towerBaseDatas[id] = baseData;

            SetLevelForTower(id, 1);

            return tower;
        }

        // Overload: create tower at a hexagon transform (uses transform.position)
        public static UniTask<BaseTower> CreateTowerAt(Transform parent, Transform hexTransform, string poolKey, TowerData baseData, TowerProgressionData? progression = null)
        {
            if (hexTransform == null) return UniTask.FromResult<BaseTower>(null);
            return CreateTowerAt(parent, hexTransform.position, poolKey, baseData, progression);
        }
    }
}
