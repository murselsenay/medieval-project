using Components.Projectiles.Enums;
using Components.Towers.Enums;
using Scriptables.Singletons;
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
        public int BuildingCost;
        public Sprite Sprite => ResourceWarehouse.Instance.GetTowerSprite(TowerType);
        public TowerData(ETowerType towerType, float health, EProjectileType projectileType, float projectileDamage, float projectileRange, float projectileCooldown, float projectileSpeed, int buildingCost)
        {
            TowerType = towerType;
            Health = health;
            ProjectileType = projectileType;
            ProjectileDamage = projectileDamage;
            ProjectileRange = projectileRange;
            ProjectileCooldown = projectileCooldown;
            ProjectileSpeed = projectileSpeed;
            BuildingCost = buildingCost;
        }
    }
    public struct TowerProgressionData
    {
        public int Id;
        public int MaxLevel;
        public float HealthPerLevel;
        public float DamagePerLevel;
        public float RangePerLevel;
        public float CooldownChangePerLevel;
        public float ProjectileSpeedPerLevel;

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
