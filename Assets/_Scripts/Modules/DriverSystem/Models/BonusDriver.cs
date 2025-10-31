using Modules.DriverSystem.Models;
using Modules.JobSystem.Models;
using UnityEngine;
namespace Modules.DriverSystem.Models
{
    public class BonusDriver : Driver
    {
        public BonusDriver(string name) : base(name)
        {
            this.BaseSalary = 2000f;
        }

        public override float SpeedMultiplier => 1.0f;
        public override float FuelConsumptionMultiplier => 0.8f;
        public override float AccidentRisk => 0.0f;
        public override float BonusTipChance => 0.50f;

        public override JobResult CalculateJobResult(float baseJobReward, float baseJobFuelCost)
        {
            JobResult result = new JobResult();
            float bonus = 0;

            if (Random.Range(0f, 1f) < BonusTipChance)
            {
                bonus = baseJobReward * 0.25f;
            }

            result.HadAccident = false;
            result.MoneyEarned = baseJobReward + bonus;
            result.FuelUsed = baseJobFuelCost * FuelConsumptionMultiplier;
            result.ReputationChange = 5;
            result.SummaryMessage = $"{DriverName} kusursuz bir hizmet sundu ve cömert bir bahþiþ kazandý!";

            return result;
        }
    }
}
