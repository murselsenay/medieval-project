using Modules.JobSystem.Models;
using UnityEngine;
namespace Modules.DriverSystem.Models
{
    public class SafeDriver : Driver
    {
        public SafeDriver(string name) : base(name)
        {
            this.BaseSalary = 800f;
        }

        public override float SpeedMultiplier => 0.8f;
        public override float FuelConsumptionMultiplier => 0.7f;
        public override float AccidentRisk => 0.0f;
        public override float BonusTipChance => 0.05f;

        public override JobResult CalculateJobResult(float baseJobReward, float baseJobFuelCost)
        {
            JobResult result = new JobResult();
            float bonus = 0;

            if (Random.Range(0f, 1f) < BonusTipChance)
            {
                bonus = baseJobReward * 0.1f;
            }

            result.HadAccident = false;
            result.MoneyEarned = baseJobReward + bonus;
            result.FuelUsed = baseJobFuelCost * FuelConsumptionMultiplier;
            result.ReputationChange = 2;
            result.SummaryMessage = $"{DriverName} görevi güvenle tamamladý ve yakýttan tasarruf etti.";

            return result;
        }
    }
}
