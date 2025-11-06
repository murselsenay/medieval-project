using System.Collections;
using Modules.TaxiSystem.Enums;
using UnityEngine;
using Modules.Economy.Models;

namespace Modules.TaxiSystem.Models
{
    public class VipTaxi : Taxi
    {
        public VipTaxi()
        {
            Type = ETaxiType.Vip;
            Price = new Price(Modules.Economy.Enums.ECurrencyType.Gold, 1000);
            // vip not repairable - but provide RepairCost if needed
            RepairCost = new Price(Modules.Economy.Enums.ECurrencyType.Gold, Mathf.CeilToInt(Price.Amount * 0.5f));
            MaxUsage = 2000f;
        }

        protected override float WearFactor => 0.3f; // very slow
        protected override float RepairMultiplier => 0.75f;
        protected override bool CanBeRepaired => false;
        public override int GetSellAmount()
        {
            if (Price == null) return 0;
            float factor = 0.75f; // vip sells for75%
            return Mathf.CeilToInt(Price.Amount * factor);
        }
    }
}