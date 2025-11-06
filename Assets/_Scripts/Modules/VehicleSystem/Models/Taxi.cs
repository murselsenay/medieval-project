using Modules.ClientSystem.Models;
using Modules.DriverSystem.Models;
using Modules.Economy.Models;
using Modules.TaxiSystem.Enums;
using Scriptables.Singletons;
using UnityEngine;
using Modules.EventSystem.Managers;
using Modules.JobSystem.Enums;

namespace Modules.TaxiSystem.Models
{
    public class Taxi
    {
        public string Id { get; set; }
        public Driver Driver { get; set; }
        public Client Client { get; set; }
        public ETaxiType Type { get; set; }
        public bool IsLocked { get; set; }
        public bool IsAvailable => Driver != null && Client == null;
        public Price Price { get; set; }
        public Sprite Icon => ResourceWarehouse.Instance.GetVehicleSprite(Type);

        // Usage / repair
        // UsageAmount now represents accumulated kilometers (0..MaxUsage). When >= MaxUsage the taxi needs repair.
        public float UsageAmount { get; set; } =0f; // km accumulated
        public float MaxUsage { get; set; } =100f; // default km until repair
        public Price RepairCost { get; set; }

        protected virtual float WearFactor =>1.0f; // multiplier for km (higher -> wears faster)
        protected virtual float RepairMultiplier =>0.25f; // relative to Price.Amount if Price set
        protected virtual bool CanBeRepaired => true;

        // Needs repair when accumulated usage reached max
        public bool RepairNeeded => UsageAmount >= MaxUsage;

        public ETaxiStatus Status
        {
            get
            {
                if (IsLocked) return ETaxiStatus.Locked;
                if (RepairNeeded) return ETaxiStatus.NeedRepair;
                if (Driver == null) return ETaxiStatus.NoDriver;
                if (Client != null) return ETaxiStatus.Busy;
                return ETaxiStatus.Available;
            }
        }

        // Add km usage; typically called with job distance
        public void AddUsage(float km)
        {
            float delta = km * WearFactor;
            UsageAmount += delta;
            if (UsageAmount > MaxUsage) UsageAmount = MaxUsage;
        }

        public void Accident()
        {
            // severe accident sets usage to max to require repair
            UsageAmount = MaxUsage;
        }

        public virtual void Repair()
        {
            if (!CanBeRepaired) return;
            UsageAmount =0f;
        }

        public virtual int GetRepairCostAmount()
        {
            if (!CanBeRepaired) return 0;
            if (RepairCost != null) return RepairCost.Amount;
            if (Price != null) return Mathf.CeilToInt(Price.Amount * RepairMultiplier);
            return 0;
        }

        public virtual int GetSellAmount()
        {
            if (Price == null) return 0;
            float factor = (Type == ETaxiType.Vip) ?0.75f :0.25f;
            return Mathf.CeilToInt(Price.Amount * factor);
        }

        public void Sell()
        {
            // Selling logic should be handled by higher-level managers (currency, removing taxi, etc.)
            // This method just returns price via GetSellAmount when needed.
        }
        public void AssignDriver(Driver driver) => Driver = driver;
        public void RemoveDriver() => Driver = null;

        public void Send(Client client)
        {
            if (client == null) return;
            if (client.TripJob == null) return;
            if (IsLocked) return;
            if (Driver == null) return;
            if (Driver.IsOnJob) return;
            if (RepairNeeded) return; // don't send if needs repair

            Client = client;

            try
            {
                client.TripJob.SetState(EJobState.Accepted);
            }
            catch { }

            client.TripJob.IsActive = true;

            Driver.StartJob(client.TripJob);

            // accumulate km usage
            AddUsage(client.TripJob.Distance);

            EventManager.DelegateClientJobAccepted(client, client.TripJob);
        }

        public void Cancel(Client client)
        {
            // prefer provided client, fallback to assigned client
            var target = client ?? Client;
            if (target == null) return;
            if (target.TripJob == null) return;

            var job = target.TripJob;

            try
            {
                job.SetState(EJobState.Cancelled);
            }
            catch { }

            job.IsActive = false;

            // notify first so listeners can find owner by job reference
            EventManager.DelegateJobCancelled(job);

            // stop driver job if this taxi's driver was handling it
            if (Driver != null && Driver.CurrentJob != null && Driver.CurrentJob.Id == job.Id)
            {
                Driver.FinishJob();
            }

            // clear client's job reference
            try
            {
                target.TripJob = null;
            }
            catch { }

            // clear taxi's client if it was assigned
            if (Client != null && Client.Id == target.Id)
                Client = null;
        }
    }
}
