namespace Modules.JobSystem.Managers
{
    using Modules.DriverSystem.Models;
    using Modules.JobSystem.Enums;
    using Modules.JobSystem.Models;
    using System.Collections.Generic;
    using UnityEngine;
    using Modules.EventSystem.Managers;

    public static class JobManager
    {
        public static List<Job> AcceptedJobs { get; private set; }

        private const float FARE_PER_KM = 2.5f;
        private const float FUEL_COST_PER_KM = 0.1f;
        private const float BASE_SPEED_KMPH = 40.0f;

        public static void Init()
        {
            AcceptedJobs = new List<Job>();
        }

        public static void AddAcceptedJob(Job job)
        {
            if (job == null) return;
            job.IsActive = false;
            AcceptedJobs.Add(job);
        }

        public static bool AssignJob(Job job, Driver driver)
        {
            if (job == null || driver == null) return false;
            if (job.IsActive) return false;

            job.IsActive = true;
            driver.StartJob(job);

            AcceptedJobs.Remove(job);

            EventManager.DelegateJobAssigned(job);

            return true;
        }

        public static void CompleteJob(Driver driver)
        {
            if (driver == null) return;
            driver.FinishJob();
        }

        public static void UnassignJob(Driver driver)
        {
            if (driver == null) return;
            var job = driver.CurrentJob;
            if (job == null) return;

            job.IsActive = false;
            driver.FinishJob();

            AcceptedJobs.Add(job);

            EventManager.DelegateJobUnassigned(job);
        }

        private static EJobDifficulty GetRandomDifficulty()
        {
            // kept for potential reuse
            float rand = Random.value;
            if (rand < 0.4f) return EJobDifficulty.Easy;
            if (rand < 0.75f) return EJobDifficulty.Normal;
            if (rand < 0.95f) return EJobDifficulty.Hard;
            return EJobDifficulty.VeryHard;
        }
    }
}