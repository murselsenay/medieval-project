using System.Collections;
using UnityEngine;
using Modules.TaxiSystem.Enums;
using Modules.Economy.Models;

namespace Modules.TaxiSystem.Models
{
    public class MetropolitanTaxi : Taxi
    {
        public MetropolitanTaxi()
        {
            Type = ETaxiType.Metropolitan;
            Price = new Price(Modules.Economy.Enums.ECurrencyType.Gold,300);
            RepairCost = new Price(Modules.Economy.Enums.ECurrencyType.Gold, Mathf.CeilToInt(Price.Amount *0.5f));
            MaxUsage =600f;
        }

        protected override float WearFactor =>0.8f; // wears slower
        protected override float RepairMultiplier =>0.5f;
    }
}