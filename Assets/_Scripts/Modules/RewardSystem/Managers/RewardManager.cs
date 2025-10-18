using System;
using System.Collections.Generic;
using Modules.RewardSystem.Models;
using Modules.Economy.Enums;

namespace Modules.RewardSystem.Managers
{
    public static class RewardManager
    {
        private static readonly List<Reward> _pendingRewards = new List<Reward>();

        public static event Action<Reward> OnRewardQueued;

        public static event Action<IReadOnlyList<Reward>> OnRewardsAwarded;

        public static void QueueReward(Reward reward)
        {
            if (reward == null) return;
            _pendingRewards.Add(reward);
            OnRewardQueued?.Invoke(reward);
        }

        public static void QueueCurrency(ECurrencyType currencyType, int amount)
        {
            if (amount <= 0) return;
            QueueReward(new CurrencyReward(currencyType, amount));
        }

        public static IReadOnlyList<Reward> GetPendingRewards()
        {
            return new List<Reward>(_pendingRewards).AsReadOnly();
        }

        public static void AwardAll()
        {
            if (_pendingRewards.Count == 0) return;

            var snapshot = new List<Reward>(_pendingRewards).AsReadOnly();

            foreach (var reward in snapshot)
            {
                try
                {
                    reward?.Award();
                }
                catch { }
            }

            OnRewardsAwarded?.Invoke(snapshot);
            _pendingRewards.Clear();
        }

        public static void ClearPending()
        {
            _pendingRewards.Clear();
        }
    }
}
