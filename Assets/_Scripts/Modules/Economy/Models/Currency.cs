using Modules.Economy.Enums;
using Scriptables.Singletons;
using UnityEngine;

namespace Modules.Economy.Models
{
    public class Currency
    {
        public int Amount;
        public ECurrencyType Type;
        public Sprite Sprite => ResourceWarehouse.Instance.GetCurrencySprite(Type);
        public Currency()
        {
            Amount = 0;
        }

        public Currency(int initialAmount, ECurrencyType type)
        {
            Amount = initialAmount;
            Type = type;
        }

        public virtual void Add()
        {
            Add(1);
        }

        public virtual void Consume()
        {
            Consume(1);
        }

        public virtual void Add(int amount)
        {
            if (amount <= 0) return;
            Amount += amount;
        }

        public virtual void Consume(int amount)
        {
            if (amount <= 0) return;
            Amount -= amount;
            if (Amount < 0) Amount = 0;
        }
    }
    public class Gold : Currency
    {
        public Gold() : base(0, ECurrencyType.Gold) { }
        public Gold(int initial) : base(initial, ECurrencyType.Gold) { }

        public override void Add() => Add(1);
        public override void Add(int amount) => base.Add(amount);

        public override void Consume() => Consume(1);
        public override void Consume(int amount) => base.Consume(amount);

        public bool TryConsume(int amount)
        {
            if (amount <= 0) return true;
            if (Amount >= amount)
            {
                Amount -= amount;
                return true;
            }
            return false;
        }
    }
    public class Gem : Currency
    {
        public Gem() : base(0, ECurrencyType.Gem) { }
        public Gem(int initial) : base(initial, ECurrencyType.Gem) { }

        public override void Add() => Add(1);
        public override void Add(int amount) => base.Add(amount);

        public override void Consume() => Consume(1);
        public override void Consume(int amount) => base.Consume(amount);

        public bool TryConsume(int amount)
        {
            if (amount <= 0) return true;
            if (Amount >= amount)
            {
                Amount -= amount;
                return true;
            }
            return false;
        }
    }
}