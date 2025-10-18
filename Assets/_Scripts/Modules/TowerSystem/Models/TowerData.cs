using Components.Projectiles.Enums;
using Components.Towers.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Components.Towers.Models
{
    public struct TowerData
    {
        public ETowerType TowerType;
        public float Health;
        public EProjectileType ProjectileType;
        public float ProjectileDamage;
        public float ProjectileRange;
        public float ProjectileCooldown;
        public float ProjectileSpeed;

        public TowerData(ETowerType towerType, float health, EProjectileType projectileType, float projectileDamage, float projectileRange, float projectileCooldown, float projectileSpeed)
        {
            TowerType = towerType;
            Health = health;
            ProjectileType = projectileType;
            ProjectileDamage = projectileDamage;
            ProjectileRange = projectileRange;
            ProjectileCooldown = projectileCooldown;
            ProjectileSpeed = projectileSpeed;
        }
    }

    // Progression rules for towers. Each field represents the per-level change.
    // Values can be zero if that stat should not change on upgrade.
    public struct TowerProgressionData
    {
        // Optional identifier to link progression datasets with towers explicitly.
        // If 0 the system may fall back to using GameObject instance ids.
        public int Id;
        public int MaxLevel;
        public float HealthPerLevel;
        public float DamagePerLevel;
        public float RangePerLevel;
        // Positive value means cooldown increases each level; negative means it decreases (i.e. fires faster).
        public float CooldownChangePerLevel;
        public float ProjectileSpeedPerLevel;

        // Existing constructor kept for backward compatibility (Id will be 0)
        public TowerProgressionData(int maxLevel, float healthPerLevel, float damagePerLevel, float rangePerLevel, float cooldownChangePerLevel, float projectileSpeedPerLevel)
        {
            Id = 0;
            MaxLevel = maxLevel;
            HealthPerLevel = healthPerLevel;
            DamagePerLevel = damagePerLevel;
            RangePerLevel = rangePerLevel;
            CooldownChangePerLevel = cooldownChangePerLevel;
            ProjectileSpeedPerLevel = projectileSpeedPerLevel;
        }

        // New constructor allowing explicit Id
        public TowerProgressionData(int id, int maxLevel, float healthPerLevel, float damagePerLevel, float rangePerLevel, float cooldownChangePerLevel, float projectileSpeedPerLevel)
        {
            Id = id;
            MaxLevel = maxLevel;
            HealthPerLevel = healthPerLevel;
            DamagePerLevel = damagePerLevel;
            RangePerLevel = rangePerLevel;
            CooldownChangePerLevel = cooldownChangePerLevel;
            ProjectileSpeedPerLevel = projectileSpeedPerLevel;
        }
    }

    // Unified info for a specific level: computed TowerData and a DPS value for UI
    public struct TowerLevelInfo
    {
        public int Level;
        public TowerData Data;
        public float DPS;

        public TowerLevelInfo(int level, TowerData data, float dps)
        {
            Level = level;
            Data = data;
            DPS = dps;
        }
    }
}
