using Modules.JobSystem.Models;
using Modules.TimerSystem.Managers;
using Scriptables.Singletons;
using UnityEngine;

namespace Modules.DriverSystem.Models
{
    public abstract class Driver
    {
        public string DriverName { get; private set; }
        public float BaseSalary { get; protected set; }
        public bool IsHired { get; protected set; }

        public Job CurrentJob { get; private set; }
        public bool IsOnJob => CurrentJob != null;
        public long JobStartUnix { get; private set; }

        public abstract float SpeedMultiplier { get; }
        public abstract float FuelConsumptionMultiplier { get; }
        public abstract float AccidentRisk { get; }
        public abstract float BonusTipChance { get; }
        public Sprite Icon => ResourceWarehouse.Instance.GetDriverPortrait(DriverName);

        public Driver(string name)
        {
            this.DriverName = name;
            this.CurrentJob = null;
            this.JobStartUnix = 0;
        }

        public abstract JobResult CalculateJobResult(float baseJobReward, float baseJobFuelCost);

        public void StartJob(Job jobToStart)
        {
            CurrentJob = jobToStart;
            JobStartUnix = TimerManager.CurrentTime;
        }

        public void FinishJob()
        {
            CurrentJob = null;
            JobStartUnix = 0;
        }
    }
}

