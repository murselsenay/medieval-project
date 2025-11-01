namespace Modules.JobSystem.Managers
{
    using Modules.DriverSystem.Models;
    using Modules.JobSystem.Enums;
    using Modules.JobSystem.Models;
    using System.Collections.Generic;
    using UnityEngine;

    public static class JobManager
    {
        public static List<Job> AvailableJobs { get; private set; }
        private static List<JobLocationData> locationList;

        private const float FARE_PER_KM = 2.5f;
        private const float FUEL_COST_PER_KM = 0.1f;
        private const float BASE_SPEED_KMPH = 40.0f;

        public static void Init()
        {
            AvailableJobs = new List<Job>();
            locationList = new List<JobLocationData>
        {
            new JobLocationData("Taksim", 3.0f),
            new JobLocationData("Beþiktaþ", 5.2f),
            new JobLocationData("Mecidiyeköy", 8.0f),
            new JobLocationData("Kadýköy", 15.5f),
            new JobLocationData("Havalimaný", 45.0f)
        };

            GenerateInitialJobs(10);
        }

        public static void GenerateInitialJobs(int count)
        {
            AvailableJobs.Clear();
            for (int i = 0; i < count; i++)
            {
                AvailableJobs.Add(CreateNewJob());
            }
        }

        private static Job CreateNewJob()
        {
            JobLocationData randomLocation = locationList[Random.Range(0, locationList.Count)];
            JobDifficulty difficulty = GetRandomDifficulty();
            float distance = randomLocation.BaseDistance + Random.Range(-1.0f, 1.0f);
            int passengerCount = 1;
            float rewardMultiplier = 1.0f;
            float timeMultiplier = 1.0f;
            switch (difficulty)
            {
                case JobDifficulty.Easy:
                    passengerCount = Random.Range(1, 3);
                    break;
                case JobDifficulty.Normal:
                    passengerCount = Random.Range(1, 4);
                    rewardMultiplier = 1.2f;
                    break;
                case JobDifficulty.Hard:
                    passengerCount = Random.Range(1, 5);
                    rewardMultiplier = 1.5f;
                    timeMultiplier = 1.1f;
                    break;
                case JobDifficulty.VeryHard:
                    passengerCount = Random.Range(3, 5);
                    rewardMultiplier = 2.0f;
                    timeMultiplier = 1.3f;
                    break;
            }
            float baseReward = (distance * FARE_PER_KM) * rewardMultiplier;
            float baseFuelCost = (distance * FUEL_COST_PER_KM);
            float durationInHours = (distance / BASE_SPEED_KMPH) * timeMultiplier;
            float baseDuration = durationInHours * 3600;
            return new Job(difficulty, randomLocation.LocationName, distance, passengerCount, baseReward, baseFuelCost, baseDuration);
        }

        public static void AssignJob(Job job, Driver driver)
        {
            job.IsActive = true;
            driver.StartJob(job);
            AvailableJobs.Remove(job);
        }

        public static void CompleteJob(Driver driver)
        {
            driver.FinishJob();
            AvailableJobs.Add(CreateNewJob());
        }

        private static JobDifficulty GetRandomDifficulty()
        {
            // ... (Bu metodun içi ayný, deðiþiklik yok) ...
            float rand = Random.value;
            if (rand < 0.4f) return JobDifficulty.Easy;
            if (rand < 0.75f) return JobDifficulty.Normal;
            if (rand < 0.95f) return JobDifficulty.Hard;
            return JobDifficulty.VeryHard;
        }
    }
}