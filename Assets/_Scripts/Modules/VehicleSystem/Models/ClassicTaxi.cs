using Modules.TaxiSystem.Enums;
using Modules.Economy.Models;
using UnityEngine;

namespace Modules.TaxiSystem.Models
{
    public class ClassicTaxi : Taxi
    {
        public ClassicTaxi()
        {
            Type = ETaxiType.Classic;
            Price = new Price(Modules.Economy.Enums.ECurrencyType.Gold,200);
            RepairCost = new Price(Modules.Economy.Enums.ECurrencyType.Gold, Mathf.CeilToInt(Price.Amount *0.25f));
            MaxUsage =400f;
        }

        protected override float WearFactor =>1.0f;
        protected override float RepairMultiplier =>0.25f;
    }
}
