using Modules.JobSystem.Models;
using UnityEngine;

namespace Modules.DriverSystem.Models
{
    public class SpeedyDriver : Driver
    {
        public SpeedyDriver(string name) : base(name)
        {
            this.BaseSalary = 1200f;
        }

        public override float SpeedMultiplier => 1.5f;
        public override float FuelConsumptionMultiplier => 1.3f;
        public override float AccidentRisk => 0.15f;
        public override float BonusTipChance => 0.0f;

        public override JobResult CalculateJobResult(float baseJobReward, float baseJobFuelCost)
        {
            JobResult result = new JobResult();

            if (Random.Range(0f, 1f) < AccidentRisk)
            {
                result.HadAccident = true;
                result.MoneyEarned = 0;
                result.FuelUsed = baseJobFuelCost * FuelConsumptionMultiplier;
                result.ReputationChange = -10;
                result.SummaryMessage = $"{DriverName} kaza yaptý! Görev iptal oldu ve araç aðýr hasarlý.";
            }
            else
            {
                result.HadAccident = false;
                result.MoneyEarned = baseJobReward;
                result.FuelUsed = baseJobFuelCost * FuelConsumptionMultiplier;
                result.ReputationChange = 1;
                result.SummaryMessage = $"{DriverName} görevi rekor sürede bitirdi, ancak çok yakýt harcadý.";
            }

            return result;
        }
    }
}
