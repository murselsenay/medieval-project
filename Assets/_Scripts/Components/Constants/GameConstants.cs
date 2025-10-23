using Components.Projectiles.Enums;
using Modules.Economy.Enums;
using System.Collections.Generic;

namespace Components.Constants
{
    public struct GameObjectTags
    {
        public static string Enemy = "Enemy";
        public static string Tower = "Tower";
    }
    public struct TowerKeys
    {
        public static string MainTower = "main-tower";
        public static string AreaTower = "area-tower";
    }
    public struct CurrencyKeys
    {
        public static string Gold = "gold";
        public static string Gem = "gem";

        public static string GetKeyForCurrency(ECurrencyType type)
        {
            return type switch
            {
                ECurrencyType.Gold => Gold,
                ECurrencyType.Gem => Gem,
                _ => Gold
            };
        }
    }
    public struct ProjectileKeys
    {
        public static string Arrow = "arrow";
        public static string Spell = "spell";

        public static string GetKeyForProjectile(EProjectileType type)
        {
            return type switch
            {
                EProjectileType.Arrow => Arrow,
                EProjectileType.Spell => Spell,
                _ => Arrow
            };
        }
    }

    public struct MinionKeys
    {
        public static string Barbarian = "minion-barbarian";
        public static string Knight = "minion-knight";
        public static string Archer = "minion-archer";
        public static string Mage = "minion-mage";


        public static string[] GetMinionKeys() => new[] { Barbarian, Knight, Archer, Mage };
    }
}
