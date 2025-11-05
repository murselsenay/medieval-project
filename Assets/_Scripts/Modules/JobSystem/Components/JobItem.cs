using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Modules.JobSystem.Models;
using Cysharp.Threading.Tasks;
using Components.Constants;
using Modules.EventSystem.Managers;
using Modules.ClientSystem.Managers;

namespace Modules.JobSystem.Components
{
    public class JobItem : BaseObject
    {
        [SerializeField] private RectTransform _subholder;
        [SerializeField] private TMP_Text _jobDescriptionText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Slider _onJobProgressbar;

        private Job _currentJob;

        public void Init(Job job)
        {
            if (job == null) return;

            _currentJob = job;

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }

            if (_onJobProgressbar != null)
            {
                _onJobProgressbar.minValue =0f;
                _onJobProgressbar.maxValue =1f;
                _onJobProgressbar.value =0f;
                _onJobProgressbar.gameObject.SetActive(_currentJob != null && _currentJob.IsActive);
            }

            SetDescription(GenerateDescription(job));

            UpdateAssignedUI();
        }

        public void InitAccepted(Job job)
        {
            if (job == null) return;
            _currentJob = job;

            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(true);
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }

            if (_onJobProgressbar != null)
            {
                _onJobProgressbar.minValue =0f;
                _onJobProgressbar.maxValue =1f;
                _onJobProgressbar.value =0f;
                _onJobProgressbar.gameObject.SetActive(_currentJob != null && _currentJob.IsActive);
            }

            SetDescription(GenerateDescription(job));

            UpdateAssignedUI();
        }

        public override void Activate()
        {
            base.Activate();

            EventManager.OnJobAssigned += OnJobAssigned;
            EventManager.OnJobUnassigned += OnJobUnassigned;
            EventManager.OnTimerTick += OnTimerTick;
        }

        public override void Deactivate()
        {
            EventManager.OnJobAssigned -= OnJobAssigned;
            EventManager.OnJobUnassigned -= OnJobUnassigned;
            EventManager.OnTimerTick -= OnTimerTick;

            base.Deactivate();
        }

        private void OnJobAssigned(Modules.JobSystem.Models.Job job)
        {
            if (job == null || _currentJob == null) return;
            if (job.Id != _currentJob.Id) return;

            UpdateAssignedUI();

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

            if (_onJobProgressbar != null)
            {
                _onJobProgressbar.value =0f;
                _onJobProgressbar.gameObject.SetActive(false);
            }
        }

        private string GenerateDescription(Job job)
        {
            if (job == null) return string.Empty;
            return $"{job.DestinationName} - {job.Distance:F1} km - {job.PassengerCount} pax";
        }

        public void SetDescription(string description)
        {
            if (_jobDescriptionText == null) return;
            _jobDescriptionText.text = description ?? string.Empty;
        }

        public void UpdateAssignedUI()
        {
            bool assigned = _currentJob != null && _currentJob.IsActive;

            if (_cancelButton != null)
                _cancelButton.gameObject.SetActive(true);

            if (_onJobProgressbar != null)
            {
                _onJobProgressbar.gameObject.SetActive(assigned);
            }
        }

        private void OnCancelClicked()
        {
            if (_currentJob == null) return;

            ClientManager.CancelAcceptedJob(_currentJob);

            UpdateAssignedUI();
        }

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

            var assignedDriver = Modules.DriverSystem.Managers.DriverManager.Drivers?.FirstOrDefault(d => d.CurrentJob != null && d.CurrentJob.Id == _currentJob.Id);
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