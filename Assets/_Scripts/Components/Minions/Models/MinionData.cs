using Components.Minions.Enums;
using Modules.RewardSystem.Models;

namespace Components.Minions.Models
{
    public struct MinionData
    {
        public EMinionType MinionType;
        public float Health;
        public int Level;
        public int Damage;
        public Reward Reward;

        public MinionData(EMinionType minionType, float health, int level, int damage, Reward reward)
        {
            MinionType = minionType;
            Health = health;
            Level = level;
            Damage = damage;
            Reward = reward;
        }
    }
}
