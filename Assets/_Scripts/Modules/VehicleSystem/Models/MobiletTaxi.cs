using System.Collections;
using Modules.TaxiSystem.Enums;
using UnityEngine;
using Modules.Economy.Models;

namespace Modules.TaxiSystem.Models
{
    public class MobiletTaxi : Taxi
    {
        public MobiletTaxi()
        {
            Type = ETaxiType.Mobilet;
            // faster wear, cheap repair
            Price = new Price(Modules.Economy.Enums.ECurrencyType.Gold,100);
            RepairCost = new Price(Modules.Economy.Enums.ECurrencyType.Gold, Mathf.CeilToInt(Price.Amount *0.25f));
            MaxUsage =200f; // low endurance
        }

        protected override float WearFactor =>1.5f;
        protected override float RepairMultiplier =>0.25f;
    }
}