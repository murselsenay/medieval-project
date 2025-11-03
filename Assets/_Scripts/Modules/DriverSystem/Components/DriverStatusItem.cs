using Modules.ObjectPoolSystem;
using Modules.DriverSystem.Models;
using Modules.JobSystem.Models;
using Modules.JobSystem.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.JobSystem.Components;
using Modules.EventSystem.Managers;
using Modules.TimerSystem.Managers;

namespace Modules.DriverSystem.Components
{
    public class DriverStatusItem : BaseObject
    {
        [SerializeField] private TMP_Text _driverNameText;
        [SerializeField] private Image _driverImage;
        [SerializeField] private Button _assignButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private GameObject _onJob;
        [SerializeField] private TMP_Text _onJobRemainingTimerText;

        private Driver _driver;
        private Job _job;

        public void Init(Driver driver, Job job)
        {
            _driver = driver;
            _job = job;

            if (_driverNameText != null)
                _driverNameText.text = _driver?.DriverName ?? "Unknown";

            if (_driverImage != null)
                _driverImage.sprite = _driver?.Icon;

            UpdateOnJobState();

            if (_assignButton != null)
            {
                _assignButton.onClick.RemoveAllListeners();
                _assignButton.onClick.AddListener(OnAssignClicked);
                _assignButton.interactable = _driver != null && !_driver.IsOnJob && _job != null && !_job.IsActive;
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(OnCancelClicked);
                _cancelButton.gameObject.SetActive(_driver != null && _driver.IsOnJob);
            }
        }

        public override void Activate()
        {
            EventManager.OnTimerTick += OnTimerTick;
        }

        public override void Deactivate()
        {
            // ensure handlers cleaned up
            EventManager.OnTimerTick -= OnTimerTick;
            base.OnDisable();
            if (_assignButton != null)
            {
                _assignButton.onClick.RemoveAllListeners();
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
            }

            _driver = null;
            _job = null;
        }

        private void OnTimerTick(long unixTime)
        {
            if (_driver == null) return;
            if (!_driver.IsOnJob || _driver.CurrentJob == null) return;

            float elapsed = (float)(unixTime - _driver.JobStartUnix);
            float remaining = _driver.CurrentJob.BaseDuration - elapsed;
            if (_onJobRemainingTimerText != null)
            {
                if (remaining <= 0)
                {
                    _onJobRemainingTimerText.text = "0:00";
                }
                else
                {
                    int s = Mathf.CeilToInt(remaining);
                    int mins = s / 60;
                    int secs = s % 60;
                    _onJobRemainingTimerText.text = string.Format("{0}:{1:00}", mins, secs);
                }
            }
        }

        private void UpdateOnJobState()
        {
            bool onJob = _driver != null && _driver.IsOnJob;
            if (_onJob != null) _onJob.SetActive(onJob);

            if (_onJobRemainingTimerText != null)
            {
                if (onJob && _driver.CurrentJob != null)
                {
                    float dur = _driver.CurrentJob.BaseDuration;
                    _onJobRemainingTimerText.text = FormatSeconds(dur);
                }
                else
                {
                    _onJobRemainingTimerText.text = string.Empty;
                }
            }

            if (_assignButton != null)
            {
                _assignButton.interactable = !onJob && _job != null && !_job.IsActive;
            }

            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(onJob);
            }
        }

        private string FormatSeconds(float seconds)
        {
            if (seconds <= 0) return "0:00";
            int s = Mathf.CeilToInt(seconds);
            int mins = s / 60;
            int secs = s % 60;
            return string.Format("{0}:{1:00}", mins, secs);
        }

        private void OnAssignClicked()
        {
            if (_driver == null || _job == null) return;

            if (JobManager.AssignJob(_job, _driver))
            {
                UpdateOnJobState();

                var parentJobItem = GetComponentInParent<JobItem>();
                if (parentJobItem != null)
                    parentJobItem.UpdateAssignedUI();
            }
        }

        private void OnCancelClicked()
        {
            if (_driver == null) return;

            JobManager.UnassignJob(_driver);

            UpdateOnJobState();

            var parentJobItem = GetComponentInParent<JobItem>();
            if (parentJobItem != null)
                parentJobItem.UpdateAssignedUI();
        }
    }
}
