using Modules.JobSystem.Enums;
using System;

namespace Modules.JobSystem.Models
{
    public class Job
    {
        public string Id { get; }
        public JobDifficulty Difficulty { get; private set; }
        public string DestinationName { get; private set; }
        public float Distance { get; private set; } // km
        public int PassengerCount { get; private set; }

        public float BaseReward { get; private set; } 
        public float BaseFuelCost { get; private set; }
        public float BaseDuration { get; private set; }

        public bool IsActive { get; set; }

        public Job(JobDifficulty difficulty, string destination, float distance, int passengers, float reward, float fuelCost, float duration)
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
        }
    }
}