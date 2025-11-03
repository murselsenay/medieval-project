using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Modules.JobSystem.Models;
using Modules.DriverSystem.Managers;
using Modules.DriverSystem.Components;
using Modules.DriverSystem.Models;
using Cysharp.Threading.Tasks;
using Components.Constants;
using Modules.EventSystem.Managers;

namespace Modules.JobSystem.Components
{
    public class JobItem : BaseObject
    {
        [SerializeField] private RectTransform _subholder;
        [SerializeField] private TMP_Text _jobDescriptionText;
        [SerializeField] private Button _driversButton;
        [SerializeField] private Button _assignedButton;
        [SerializeField] private GameObject[] _difficulties;
        [SerializeField] private Transform _driversHolder;
        [SerializeField] private Slider _onJobProgressbar;

        [SerializeField] private LayoutElement _subholderLayoutElement;

        private float _driversClosedSize =100;
        private float _driversOpenedSize =475;
        [SerializeField] private float _driversAnimDuration =0.25f;

        private bool _driversOpen = false;

        private RectTransform _parentLayoutRect;

        private List<DriverStatusItem> _spawnedDriverItems = new List<DriverStatusItem>();
        private Job _currentJob;

        private static JobItem _openedJobItem;

        public void Init(Job job)
        {
            if (job == null) return;

            _currentJob = job;
            _driversOpen = false;

            if (_driversButton != null)
            {
                _driversButton.onClick.RemoveAllListeners();
                _driversButton.onClick.AddListener(ToggleDrivers);
            }

            if (_assignedButton != null)
            {
                _assignedButton.onClick.RemoveAllListeners();
                _assignedButton.onClick.AddListener(ToggleDrivers);
            }

            if (_subholder != null)
            {
                if (_subholderLayoutElement == null)
                {
                    _subholderLayoutElement = _subholder.GetComponent<LayoutElement>();
                    if (_subholderLayoutElement == null)
                    {
                        _subholderLayoutElement = _subholder.gameObject.AddComponent<LayoutElement>();
                    }
                }

                _subholderLayoutElement.preferredHeight = _driversClosedSize;

                var vlg = GetComponentInParent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    _parentLayoutRect = vlg.GetComponent<RectTransform>();
                }
                else
                {
                    if (transform.parent != null)
                        _parentLayoutRect = transform.parent as RectTransform;
                }

                var sd = _subholder.sizeDelta;
                sd.y = _driversClosedSize;
                _subholder.sizeDelta = sd;
            }

            // Initialize progressbar
            if (_onJobProgressbar != null)
            {
                _onJobProgressbar.minValue =0f;
                _onJobProgressbar.maxValue =1f;
                _onJobProgressbar.value =0f;
                _onJobProgressbar.gameObject.SetActive(_currentJob != null && _currentJob.IsActive);
            }

            SetDescription(GenerateDescription(job));
            SetDifficulty((int)job.Difficulty);

            UpdateAssignedUI();
        }

        public override void Activate()
        {
            // Ensure base activation behavior runs (set active, reset transforms, invoke events)
            base.Activate();

            EventManager.OnJobAssigned += OnJobAssigned;
            EventManager.OnJobUnassigned += OnJobUnassigned;
            EventManager.OnTimerTick += OnTimerTick; // subscribe to timer to drive progressbar
        }

        public override void Deactivate()
        {
            EventManager.OnJobAssigned -= OnJobAssigned;
            EventManager.OnJobUnassigned -= OnJobUnassigned;
            EventManager.OnTimerTick -= OnTimerTick; // unsubscribe

            if (_openedJobItem == this)
                _openedJobItem = null;

            ClearDriverItems();

            // Ensure base deactivation behavior runs (invoke deactivated, parent to pool holder, set inactive)
            base.Deactivate();
        }

        private void OnJobAssigned(Modules.JobSystem.Models.Job job)
        {
            if (job == null || _currentJob == null) return;
            if (job.Id != _currentJob.Id) return;

            UpdateAssignedUI();

            if (_driversOpen)
            {
                PopulateDrivers().Forget();
            }

            // show and reset progressbar when job assigned
            if (_onJobProgressbar != null)
            {
                _onJobProgressbar.gameObject.SetActive(true);
                _onJobProgressbar.value =0f;
            }
        }

        private void OnJobUnassigned(Modules.JobSystem.Models.Job job)
        {
            if (job == null || _currentJob == null) return;
            if (job.Id != _currentJob.Id) return;

            UpdateAssignedUI();

            if (_driversOpen)
            {
                PopulateDrivers().Forget();
            }

            // hide/reset progressbar when job unassigned
            if (_onJobProgressbar != null)
            {
                _onJobProgressbar.value =0f;
                _onJobProgressbar.gameObject.SetActive(false);
            }
        }

        private string GenerateDescription(Job job)
        {
            if (job == null) return string.Empty;
            return $"{job.DestinationName} · {job.Distance:F1} km · {job.PassengerCount} pax";
        }

        public void OpenDrivers()
        {
            if (_driversOpen) return;

            if (_openedJobItem != null && _openedJobItem != this)
            {
                _openedJobItem.CloseDrivers();
            }

            _openedJobItem = this;
            _driversOpen = true;

            if (_subholder == null) return;

            PopulateDrivers().Forget();

            if (_subholderLayoutElement != null)
            {
                var parentRect = _parentLayoutRect;
                DOTween.To(() => _subholderLayoutElement.preferredHeight,
                           x =>
                           {
                               _subholderLayoutElement.preferredHeight = x;
                               var sd = _subholder.sizeDelta;
                               sd.y = x;
                               _subholder.sizeDelta = sd;
                               if (parentRect != null)
                                   LayoutRebuilder.MarkLayoutForRebuild(parentRect);
                           },
                           _driversOpenedSize, _driversAnimDuration).SetUpdate(true);

                return;
            }

            Vector2 target = new Vector2(_subholder.sizeDelta.x, _driversOpenedSize);
            _subholder.DOSizeDelta(target, _driversAnimDuration).SetUpdate(true);
        }

        private async UniTask PopulateDrivers()
        {
            ClearDriverItems();

            string address = AddressableKeys.DriverStatusItem;

            Driver assignedDriver = null;
            if (_currentJob != null && _currentJob.IsActive)
            {
                assignedDriver = DriverManager.Drivers?.FirstOrDefault(d => d.CurrentJob != null && d.CurrentJob.Id == _currentJob.Id);
                if (assignedDriver != null)
                {
                    DriverStatusItem assignedItem = null;
                    try { assignedItem = await ObjectPool.GetObjectAsync<DriverStatusItem>(_driversHolder, address); }
                    catch { assignedItem = null; }

                    if (assignedItem != null)
                    {
                        assignedItem.Init(assignedDriver, _currentJob);
                        _spawnedDriverItems.Add(assignedItem);
                    }
                }
            }

            var availableDrivers = DriverManager.GetAvailableDrivers()?.Where(d => d != assignedDriver).ToList();
            if (availableDrivers == null) return;

            foreach (var d in availableDrivers)
            {
                DriverStatusItem item = null;
                try
                {
                    item = await ObjectPool.GetObjectAsync<DriverStatusItem>(_driversHolder, address);
                }
                catch
                {
                    item = null;
                }

                if (item == null) continue;

                item.Init(d, _currentJob);
                _spawnedDriverItems.Add(item);
            }

            if (_subholder != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_subholder);
                if (_parentLayoutRect != null)
                    LayoutRebuilder.MarkLayoutForRebuild(_parentLayoutRect);
            }
        }

        public void CloseDrivers()
        {
            if (!_driversOpen) return;
            _driversOpen = false;

            ClearDriverItems();

            if (_openedJobItem == this)
                _openedJobItem = null;

            if (_subholder == null) return;

            if (_subholderLayoutElement != null)
            {
                var parentRect = _parentLayoutRect;
                DOTween.To(() => _subholderLayoutElement.preferredHeight,
                           x =>
                           {
                               _subholderLayoutElement.preferredHeight = x;
                               var sd = _subholder.sizeDelta;
                               sd.y = x;
                               _subholder.sizeDelta = sd;
                               if (parentRect != null)
                                   LayoutRebuilder.MarkLayoutForRebuild(parentRect);
                           },
                           _driversClosedSize, _driversAnimDuration).SetUpdate(true);

                return;
            }

            Vector2 target = new Vector2(_subholder.sizeDelta.x, _driversClosedSize);
            _subholder.DOSizeDelta(target, _driversAnimDuration).SetUpdate(true);
        }

        private void ToggleDrivers()
        {
            if (_driversOpen) CloseDrivers(); else OpenDrivers();
        }

        private void ClearDriverItems()
        {
            for (int i =0; i < _spawnedDriverItems.Count; i++)
            {
                var it = _spawnedDriverItems[i];
                if (it == null) continue;
                try
                {
                    ObjectPool.ReturnToPool(it);
                }
                catch
                {
                    if (it.gameObject != null) UnityEngine.Object.Destroy(it.gameObject);
                }
            }
            _spawnedDriverItems.Clear();
        }

        public void SetDescription(string description)
        {
            if (_jobDescriptionText == null) return;
            _jobDescriptionText.text = description ?? string.Empty;
        }

        public void SetDifficulty(int level)
        {
            if (_difficulties == null || _difficulties.Length ==0) return;

            int clamped = Mathf.Clamp(level,0, _difficulties.Length -1);

            for (int i =0; i < _difficulties.Length; i++)
            {
                var go = _difficulties[i];
                if (go == null) continue;
                go.SetActive(i <= clamped);
            }
        }

        public void UpdateAssignedUI()
        {
            bool assigned = _currentJob != null && _currentJob.IsActive;
            if (_driversButton != null)
                _driversButton.gameObject.SetActive(!assigned);
            if (_assignedButton != null)
                _assignedButton.gameObject.SetActive(assigned);

            if (_onJobProgressbar != null)
            {
                _onJobProgressbar.gameObject.SetActive(assigned);
            }
        }

        // New: update progressbar on timer ticks
        private void OnTimerTick(long unixTime)
        {
            if (_onJobProgressbar == null) return;
            if (_currentJob == null) return;
            if (!_currentJob.IsActive)
            {
                if (_onJobProgressbar.value !=0f)
                    _onJobProgressbar.value =0f;

                return;
            }

            // find assigned driver for this job
            var assignedDriver = DriverManager.Drivers?.FirstOrDefault(d => d.CurrentJob != null && d.CurrentJob.Id == _currentJob.Id);
            if (assignedDriver == null) return;

            float elapsed = (float)(unixTime - assignedDriver.JobStartUnix);
            float duration = _currentJob.BaseDuration;
            if (duration <=0f)
            {
                _onJobProgressbar.value =1f;
                return;
            }

            float progress = Mathf.Clamp01(elapsed / duration);
            _onJobProgressbar.value = progress;
        }
    }
}