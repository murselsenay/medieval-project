using System.Collections;
using UnityEngine;
using Modules.TaxiSystem.Enums;
using Modules.Economy.Models;

namespace Modules.TaxiSystem.Models
{
    public class LondonTaxi : Taxi
    {
        public LondonTaxi()
        {
            Type = ETaxiType.London;
            Price = new Price(Modules.Economy.Enums.ECurrencyType.Gold,500);
            RepairCost = new Price(Modules.Economy.Enums.ECurrencyType.Gold, Mathf.CeilToInt(Price.Amount *0.75f));
            MaxUsage =1000f;
        }

        protected override float WearFactor =>0.6f; // wears slowly
        protected override float RepairMultiplier =>0.75f;
    }
}