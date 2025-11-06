using Modules.JobSystem.Enums;
using System;
using System.Linq;
using Modules.DriverSystem.Managers;
using Modules.TimerSystem.Managers;

namespace Modules.JobSystem.Models
{
    public class Job
    {
        public string Id { get; }
        public EJobDifficulty Difficulty { get; private set; }
        private EJobState _manualState = EJobState.Waiting; // set by external calls for Rejected/Cancelled/Accepted

        // Compute state dynamically in getter
        public EJobState State
        {
            get
            {
                // manual cancelled or rejected should take precedence
                if (_manualState == EJobState.Cancelled || _manualState == EJobState.Rejected)
                    return _manualState;

                // Try to find if a driver is currently assigned to this job
                try
                {
                    var drivers = DriverManager.Drivers;
                    var assigned = drivers?.FirstOrDefault(d => d != null && d.CurrentJob != null && d.CurrentJob.Id == this.Id);
                    if (assigned != null)
                    {
                        // compute elapsed using TimerManager
                        long now = TimerManager.CurrentTime;
                        float elapsed = (float)(now - assigned.JobStartUnix);
                        if (elapsed >= BaseDuration)
                            return EJobState.Completed;

                        return EJobState.Accepted;
                    }
                }
                catch { }

                // If explicitly marked accepted (but no driver yet), respect it
                if (_manualState == EJobState.Accepted)
                    return EJobState.Accepted;

                // default
                return EJobState.Waiting;
            }
        }

        public string DestinationName { get; private set; }
        public float Distance { get; private set; } // km
        public int PassengerCount { get; private set; }

        public float BaseReward { get; private set; }
        public float BaseFuelCost { get; private set; }
        public float BaseDuration { get; private set; }

        public bool IsActive { get; set; }

        public Job(EJobDifficulty difficulty, string destination, float distance, int passengers, float reward, float fuelCost, float duration)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Difficulty = difficulty;
            this.DestinationName = destination;
            this.Distance = distance;
            this.PassengerCount = passengers;
            this.BaseReward = reward;
            this.BaseFuelCost = fuelCost;
            this.BaseDuration = duration;
            this.IsActive = false;
            this._manualState = EJobState.Waiting;
        }

        // Allow controlled state changes from external managers
        public void SetState(EJobState newState)
        {
            this._manualState = newState;
        }
    }
}