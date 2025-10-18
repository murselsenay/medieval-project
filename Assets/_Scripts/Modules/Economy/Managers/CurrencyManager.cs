using System;
using Modules.Economy.Enums;
using Modules.Economy.Models;
using Modules.EventSystem.Managers;

namespace Modules.Economy.Managers
{
    public static class CurrencyManager
    {
        private static Gold _gold = new Gold(0);
        private static Gem _gem = new Gem(0);

        public static void Initialize(int startingGold = 0, int startingGems = 0)
        {
            _gold = new Gold(startingGold);
            _gem = new Gem(startingGems);

            EventManager.DelegateCurrencyRequestCompleted(ECurrencyType.Gem);
            EventManager.DelegateCurrencyRequestCompleted(ECurrencyType.Gold);
        }

        public static int GetAmount(ECurrencyType type)
        {
            return type switch
            {
                ECurrencyType.Gold => _gold.Amount,
                ECurrencyType.Gem => _gem.Amount,
                _ => 0
            };
        }

        public static void Add(ECurrencyType type, int amount)
        {
            if (amount <= 0) return;
            switch (type)
            {
                case ECurrencyType.Gold:
                    _gold.Add(amount);
                    EventManager.DelegateCurrencyChanged(type, _gold.Amount);
                    break;
                case ECurrencyType.Gem:
                    _gem.Add(amount);
                    EventManager.DelegateCurrencyChanged(type, _gem.Amount);
                    break;
            }
        }

        public static bool TryConsume(ECurrencyType type, int amount)
        {
            if (amount <= 0) return true;
            switch (type)
            {
                case ECurrencyType.Gold:
                    if (_gold.TryConsume(amount))
                    {
                        EventManager.DelegateCurrencyChanged(type, _gold.Amount);
                        return true;
                    }
                    return false;
                case ECurrencyType.Gem:
                    if (_gem.TryConsume(amount))
                    {
                        EventManager.DelegateCurrencyChanged(type, _gem.Amount);
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static void SetAmount(ECurrencyType type, int amount)
        {
            if (amount < 0) amount = 0;
            switch (type)
            {
                case ECurrencyType.Gold:
                    _gold.Amount = amount;
                    EventManager.DelegateCurrencyChanged(type, _gold.Amount);
                    break;
                case ECurrencyType.Gem:
                    _gem.Amount = amount;
                    EventManager.DelegateCurrencyChanged(type, _gem.Amount);
                    break;
            }
        }
    }
}

