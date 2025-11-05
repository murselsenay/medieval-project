using Modules.PopupSystem.Components;
using UnityEngine;
using System.Collections.Generic;
using Modules.JobSystem.Managers;
using Modules.JobSystem.Models;
using Modules.JobSystem.Components;
using Cysharp.Threading.Tasks;
using Modules.AdressableSystem;
using Components.Constants;
using Modules.ObjectPoolSystem;
using Modules.Logger;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using Modules.EventSystem.Managers;

namespace Modules.PopupSystem.Popups
{
    public class JobPopup : BasePopup
    {
        [SerializeField] private RectTransform _jobsHolder;
        [SerializeField] private ScrollRect _scrollRect;

        private readonly List<JobItem> _spawnedItems = new List<JobItem>();
        private readonly Dictionary<string, JobItem> _itemsByJobId = new Dictionary<string, JobItem>();

        public override void Init()
        {
            if (_jobsHolder != null)
            {
                var ap = _jobsHolder.anchoredPosition;
                ap.y = 0f;
                _jobsHolder.anchoredPosition = ap;
            }

            PopulateJobs().Forget();
        }

        public override void Activate()
        {
            if (_scrollRect != null)
            {
                _scrollRect.StopMovement();
                _scrollRect.velocity = Vector2.zero;
                _scrollRect.enabled = false;
            }

            base.Activate();

            EventManager.OnJobAssigned += OnJobAssigned;
            EventManager.OnJobUnassigned += OnJobUnassigned;
            EventManager.OnJobCancelled += OnJobCancelled;
            EventManager.OnClientJobAccepted += OnClientJobAccepted; // when a client job is accepted it may be added to AcceptedJobs
        }

        public override void Deactivate()
        {
            EventManager.OnJobAssigned -= OnJobAssigned;
            EventManager.OnJobUnassigned -= OnJobUnassigned;
            EventManager.OnJobCancelled -= OnJobCancelled;
            EventManager.OnClientJobAccepted -= OnClientJobAccepted;

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                _scrollRect.velocity = Vector2.zero;
                _scrollRect.enabled = true;
            }

            base.Deactivate();
        }

        protected override void OnAfterClose()
        {
            ClearJobs();
        }

        protected override void OnAfterShow()
        {
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                _scrollRect.velocity = Vector2.zero;
                _scrollRect.enabled = true;
            }
        }

        private void OnClientJobAccepted(Modules.ClientSystem.Models.Client client, Job job)
        {
            // a client accepted job has been added to the JobManager.AcceptedJobs — ensure it appears in popup
            if (job != null) AddOrUpdateJobItem(job).Forget();
        }

        private void OnJobAssigned(Job job)
        {
            if (job == null) return;
            UpdateJobItem(job);
        }

        private void OnJobUnassigned(Job job)
        {
            if (job == null) return;
            UpdateJobItem(job);
        }

        private void OnJobCancelled(Job job)
        {
            if (job == null) return;
            RemoveJobItem(job.Id);
        }

        public async UniTask PopulateJobs()
        {
            if (_jobsHolder != null)
            {
                var ap0 = _jobsHolder.anchoredPosition;
                ap0.y = 0f;
                _jobsHolder.anchoredPosition = ap0;
            }

            ClearJobs();

            var addressToUse = AddressableKeys.JobItem;

            if (string.IsNullOrEmpty(addressToUse))
            {
                DebugLogger.LogError("JobPopup: JobItem prefab or address is not assigned.");
                return;
            }

            // Show only accepted jobs (including cancelled ones)
            var jobs = JobManager.AcceptedJobs;
            if (jobs == null || jobs.Count == 0) return;

            var orderedJobs = jobs.OrderBy(j => j.Difficulty).ToList();

            foreach (Job job in orderedJobs)
            {
                if (job == null) continue;

                JobItem item = null;

                try
                {
                    item = await ObjectPool.GetObjectAsync<JobItem>(_jobsHolder, addressToUse);
                }
                catch
                {
                    item = null;
                }

                if (item == null) continue;

                // ensure parent
                if (item.transform.parent != _jobsHolder)
                    item.transform.SetParent(_jobsHolder, false);

                // Initialize in accepted-job mode (no drivers, only cancel)
                item.InitAccepted(job);
                _spawnedItems.Add(item);
                _itemsByJobId[job.Id] = item;
            }

            if (_jobsHolder != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_jobsHolder);
            }
        }

        private async UniTask AddOrUpdateJobItem(Job job)
        {
            if (job == null) return;
            if (_itemsByJobId.TryGetValue(job.Id, out var existing) && existing != null)
            {
                if (existing.transform.parent != _jobsHolder)
                    existing.transform.SetParent(_jobsHolder, false);
                existing.InitAccepted(job);
                return;
            }

            var addressToUse = AddressableKeys.JobItem;
            JobItem item = null;
            try
            {
                item = await ObjectPool.GetObjectAsync<JobItem>(_jobsHolder, addressToUse);
            }
            catch { item = null; }

            if (item == null) return;

            if (item.transform.parent != _jobsHolder)
                item.transform.SetParent(_jobsHolder, false);

            item.InitAccepted(job);
            _spawnedItems.Add(item);
            _itemsByJobId[job.Id] = item;

            if (_jobsHolder != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_jobsHolder);
            }
        }

        private void UpdateJobItem(Job job)
        {
            if (job == null) return;
            if (_itemsByJobId.TryGetValue(job.Id, out var existing) && existing != null)
            {
                existing.InitAccepted(job);
            }
        }

        private void RemoveJobItem(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return;
            if (_itemsByJobId.TryGetValue(jobId, out var existing) && existing != null)
            {
                try { ObjectPool.ReturnToPool(existing); } catch { if (existing.gameObject != null) UnityEngine.Object.Destroy(existing.gameObject); }
                _spawnedItems.Remove(existing);
                _itemsByJobId.Remove(jobId);

                if (_jobsHolder != null)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_jobsHolder);
                }
            }
        }

        public void ClearJobs()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                var it = _spawnedItems[i];
                if (it == null) continue;

                try
                {
                    ObjectPool.ReturnToPool(it);
                }
                catch
                {
                    if (it.gameObject != null)
                        UnityEngine.Object.Destroy(it.gameObject);
                }
            }
            _spawnedItems.Clear();
            _itemsByJobId.Clear();
        }

        private void OnDestroy()
        {
            ClearJobs();
        }
    }
}