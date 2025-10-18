using Modules.Economy.Enums;
using Modules.Economy.Managers;
using Modules.RewardSystem.Enums;

namespace Modules.RewardSystem.Models
{
    public abstract class Reward
    {
        public int RewardAmount;
        public ERewardType RewardType;

        protected Reward()
        {
            RewardAmount = 0;
            RewardType = ERewardType.None;
        }

        protected Reward(int amount, ERewardType type)
        {
            RewardAmount = amount;
            RewardType = type;
        }

        public virtual void Award() { }
    }

    public class CurrencyReward : Reward
    {
        public ECurrencyType CurrencyType;

        public CurrencyReward()
        {
            RewardType = ERewardType.Currency;
            CurrencyType = ECurrencyType.Gold;
            RewardAmount = 0;
        }

        public CurrencyReward(ECurrencyType currencyType, int amount)
            : base(amount, ERewardType.Currency)
        {
            CurrencyType = currencyType;
        }

        public override void Award()
        {
            if (RewardAmount <= 0) return;
            CurrencyManager.Add(CurrencyType, RewardAmount);
        }
    }

    public class GoldReward : CurrencyReward
    {
        public GoldReward() : base(ECurrencyType.Gold, 0) { }
        public GoldReward(int amount) : base(ECurrencyType.Gold, amount) { }
    }

    public class GemReward : CurrencyReward
    {
        public GemReward() : base(ECurrencyType.Gem, 0) { }
        public GemReward(int amount) : base(ECurrencyType.Gem, amount) { }
    }
}
