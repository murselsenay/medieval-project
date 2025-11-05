using System;
using System.Collections.Generic;
using System.Linq;
using Modules.ClientSystem.Models;
using Modules.ClientSystem.Enums;
using Modules.JobSystem.Models;
using Modules.JobSystem.Enums;
using Modules.JobSystem.Managers;
using Modules.EventSystem.Managers;
using UnityEngine;
using Scriptables.Singletons;
using Modules.TimerSystem.Managers;

namespace Modules.ClientSystem.Managers
{
    public static class ClientManager
    {
        public static List<Client> AllClients { get; private set; }
        public static List<Client> ActiveClients => AllClients?.Where(c => c.TripJob != null).ToList();

        private const int MAX_CLIENTS = 12;

        public static void Init()
        {
            AllClients = new List<Client>();
            GenerateInitialClients(MAX_CLIENTS);

            //EventManager.OnTimerTick += Tick;
            Tick(TimerManager.CurrentTime);
        }

        private static void GenerateInitialClients(int count)
        {
            AllClients.Clear();

            var rw = ResourceWarehouse.Instance;
            var femaleNames = rw?.FemaleClientNames?.ToList() ?? new List<string>();
            var maleNames = rw?.MaleClientNames?.ToList() ?? new List<string>();

            // deterministic distribution:5 female, rest male (for count =12 ->5 female,7 male)
            int desiredFemales = Math.Min(5, count);
            int desiredMales = count - desiredFemales;

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // pick female names in order, fallback to generated unique if not enough
            for (int i = 0; i < desiredFemales; i++)
            {
                string name;
                if (i < femaleNames.Count)
                    name = femaleNames[i];
                else
                    name = $"Female{i + 1}";

                // ensure uniqueness
                if (usedNames.Contains(name))
                {
                    int suf = 1;
                    var baseName = name;
                    while (usedNames.Contains(name))
                    {
                        name = baseName + suf;
                        suf++;
                    }
                }

                usedNames.Add(name);

                var c = new Client
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Age = 25 + (i % 10),
                    Profession = (EClientProfession)(i % Enum.GetValues(typeof(EClientProfession)).Length),
                    Gender = EClientGender.Female,
                    TripJob = null,
                };
                AllClients.Add(c);
            }

            // pick male names in order, fallback to generated unique if not enough
            for (int i = 0; i < desiredMales; i++)
            {
                string name;
                if (i < maleNames.Count)
                    name = maleNames[i];
                else
                    name = $"Male{i + 1}";

                if (usedNames.Contains(name))
                {
                    int suf = 1;
                    var baseName = name;
                    while (usedNames.Contains(name))
                    {
                        name = baseName + suf;
                        suf++;
                    }
                }

                usedNames.Add(name);

                int idx = desiredFemales + i;
                var c = new Client
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Age = 28 + (i % 12),
                    Profession = (EClientProfession)(idx % Enum.GetValues(typeof(EClientProfession)).Length),
                    Gender = EClientGender.Male,
                    TripJob = null,
                };
                AllClients.Add(c);
            }

            // ensure we have exactly 'count' clients (should be true)
            while (AllClients.Count < count)
            {
                var name = $"Client{AllClients.Count + 1}";
                if (usedNames.Contains(name)) name += "_" + Guid.NewGuid().ToString("N").Substring(0, 4);
                usedNames.Add(name);

                var c = new Client
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Age = 30,
                    Profession = EClientProfession.Tourist,
                    Gender = EClientGender.Unknown,
                    TripJob = null,
                };
                AllClients.Add(c);
            }
        }

        public static void Tick(long currentUnix)
        {
            if (AllClients == null || AllClients.Count == 0) return;
            int attempts = UnityEngine.Random.Range(1, 4);
            for (int i = 0; i < attempts; i++)
            {
                var c = AllClients[UnityEngine.Random.Range(0, AllClients.Count)];
                if (c.CanGenerateJob(currentUnix))
                {
                    c.TripJob = CreateTripJobForClient(c);
                    EventManager.TriggerClientJobCreated(c, c.TripJob);
                }
            }
        }

        private static Job CreateTripJobForClient(Client client)
        {
            var dests = new[] { "Tavern", "Market", "Harbor", "Castle", "Fields" };
            var loc = dests[UnityEngine.Random.Range(0, dests.Length)];
            var difficulty = GetRandomDifficulty();
            float distance = UnityEngine.Random.Range(1f, 30f);
            int pax = 1;
            float reward = distance * 2.5f;
            float fuel = distance * 0.1f;
            float duration = (distance / 40f) * 3600f;
            var job = new Job(difficulty, loc, distance, pax, reward, fuel, duration);
            return job;
        }

        private static EJobDifficulty GetRandomDifficulty()
        {
            float r = UnityEngine.Random.value;
            if (r < 0.6f) return EJobDifficulty.Easy;
            if (r < 0.9f) return EJobDifficulty.Normal;
            return EJobDifficulty.Hard;
        }

        public static void AcceptClientJob(Client client)
        {
            if (client == null || client.TripJob == null) return;
            client.TripJob.SetState(EJobState.Accepted);
            JobManager.AddAcceptedJob(client.TripJob);
            EventManager.TriggerClientJobAccepted(client, client.TripJob);
        }

        public static void RejectClientJob(Client client, long currentUnix, int cooldownSeconds = 60)
        {
            if (client == null || client.TripJob == null) return;
            // mark job rejected and remove from client
            client.TripJob.SetState(EJobState.Rejected);
            // If the job was added to AcceptedJobs, remove it
            try { JobManager.AcceptedJobs?.RemoveAll(j => j != null && j.Id == client.TripJob.Id); } catch { }
            client.TripJob = null;
            client.CooldownUntilUnix = currentUnix + cooldownSeconds;
            EventManager.TriggerClientJobRejected(client);
        }

        public static void CancelAcceptedJob(Job job)
        {
            if (job == null) return;
            // mark cancelled and remove from accepted job list
            job.SetState(EJobState.Cancelled);
            try { JobManager.AcceptedJobs?.RemoveAll(j => j != null && j.Id == job.Id); } catch { }

            // Trigger cancellation event while owner still references the job so listeners can find owner by job id
            EventManager.TriggerJobCancelled(job);

            // remove job reference from its owner client
            var owner = AllClients?.FirstOrDefault(c => c.TripJob != null && c.TripJob.Id == job.Id);
            if (owner != null)
            {
                owner.TripJob = null;
            }
        }
    }
}
