using Modules.JobSystem.Models;

namespace Modules.DriverSystem.Models
{
    public abstract class Driver
    {
        public string DriverName { get; private set; }
        public float BaseSalary { get; protected set; }

        public Job CurrentJob { get; private set; }
        public bool IsOnJob => CurrentJob != null;

        public abstract float SpeedMultiplier { get; }
        public abstract float FuelConsumptionMultiplier { get; }
        public abstract float AccidentRisk { get; }
        public abstract float BonusTipChance { get; }

        public Driver(string name)
        {
            this.DriverName = name;
            this.CurrentJob = null;
        }

        public abstract JobResult CalculateJobResult(float baseJobReward, float baseJobFuelCost);

        public void StartJob(Job jobToStart)
        {
            CurrentJob = jobToStart;
        }

        public void FinishJob()
        {
            CurrentJob = null;
        }
    }
}

