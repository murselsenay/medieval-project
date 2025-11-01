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

namespace Modules.PopupSystem.Popups
{
    public class JobPopup : BasePopup
    {
        [SerializeField] private RectTransform _jobsHolder;
        [SerializeField] private ScrollRect _scrollRect; // keep field but remove direct logic from this class

        private readonly List<JobItem> _spawnedItems = new List<JobItem>();

        public override void Init()
        {
            // initial reset (keeps content top before population starts)
            if (_jobsHolder != null)
            {
                var ap = _jobsHolder.anchoredPosition;
                ap.y = 0f;
                _jobsHolder.anchoredPosition = ap;
            }

            PopulateJobs().Forget();
        }

        // Disable scrollRect before activating (so base.Activate's scale animation won't be interfered)
        public override void Activate()
        {
            if (_scrollRect != null)
            {
                _scrollRect.StopMovement();
                _scrollRect.velocity = Vector2.zero;
                _scrollRect.enabled = false;
            }

            base.Activate();
        }

        // Cleanup after close animation finishes so items stay visible during animation
        protected override void OnAfterClose()
        {
            ClearJobs();
        }

        // Re-enable scroll rect after show animation completed
        protected override void OnAfterShow()
        {
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                _scrollRect.velocity = Vector2.zero;
                _scrollRect.enabled = true;
            }
        }

        public async UniTask PopulateJobs()
        {
            // Ensure content starts at top
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

            var jobs = JobManager.AvailableJobs;
            if (jobs == null || jobs.Count == 0) return;

            // Sort jobs by Difficulty ascending: Easy -> Normal -> Hard -> VeryHard
            var orderedJobs = jobs.OrderBy(j => j.Difficulty).ToList();

            foreach (Job job in orderedJobs)
            {
                if (job == null) continue;

                JobItem item = null;

                if (!string.IsNullOrEmpty(addressToUse))
                {
                    try
                    {
                        item = await ObjectPool.GetObjectAsync<JobItem>(_jobsHolder, addressToUse);
                    }
                    catch
                    {
                        item = null;
                    }
                }

                if (item == null) continue;

                item.Init(job);
                _spawnedItems.Add(item);
            }

            // Force a single layout rebuild
            if (_jobsHolder != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_jobsHolder);
            }

            // Note: re-enabling scrollRect or adjusting its position will be handled elsewhere as requested
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
        }

        private void OnDestroy()
        {
            ClearJobs();
        }
    }
}