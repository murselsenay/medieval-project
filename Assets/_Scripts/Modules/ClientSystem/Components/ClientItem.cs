using Modules.ObjectPoolSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.ClientSystem.Models;
using Modules.ClientSystem.Managers;
using Modules.JobSystem.Models;
using Modules.EventSystem.Managers;
using System;
using Modules.DriverSystem.Managers;
using Modules.DriverSystem.Models;
using Scriptables.Singletons;
using System.Linq;
using Modules.JobSystem.Enums;

namespace Modules.ClientSystem.Components
{
    public class ClientItem : BaseObject
    {
        [SerializeField] private TMP_Text _clientNameText;
        [SerializeField] private TMP_Text _clientInformationText;

        [Header("Waiting")]
        [SerializeField] private TMP_Text _clientDestinationText;
        [SerializeField] private TMP_Text _clientDistanceText;
        [SerializeField] private TMP_Text _clientPrizeText;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _rejectButton;
        [SerializeField] private GameObject _newIndicator;

        [Header("Accepted")]
        [SerializeField] private TMP_Text _remainingTimeText;
        [SerializeField] private TMP_Text _assignedDriverNameText;
        [SerializeField] private Image _clientImage;
        [SerializeField] private GameObject _acceptedIndicator;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _instantFinishButton;

        private Client _client;

        public string ClientId { get; private set; }

        public void Init(Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            ClientId = client.Id;

            if (_acceptButton != null)
            {
                _acceptButton.onClick.RemoveAllListeners();
                _acceptButton.onClick.AddListener(OnAcceptClicked);
            }

            if (_rejectButton != null)
            {
                _rejectButton.onClick.RemoveAllListeners();
                _rejectButton.onClick.AddListener(OnRejectClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }

            if (_instantFinishButton != null)
            {
                _instantFinishButton.onClick.RemoveAllListeners();
                _instantFinishButton.onClick.AddListener(OnInstantFinishClicked);
            }

            UpdateVisuals();

            try
            {
                var rw = ResourceWarehouse.Instance;
                if (rw != null && _clientImage != null)
                {
                    var sprite = rw.GetClientPortrait(_client.Name);
                    if (sprite != null) _clientImage.sprite = sprite;
                }
            }
            catch { }
        }

        public override void Activate()
        {
            base.Activate();
            EventManager.OnClientJobCreated += OnClientJobCreated;
            EventManager.OnClientJobAccepted += OnClientJobAccepted;
            EventManager.OnClientJobRejected += OnClientJobRejected;
            EventManager.OnJobAssigned += OnJobAssigned;
            EventManager.OnJobUnassigned += OnJobUnassigned;
            EventManager.OnJobCancelled += OnJobCancelled;
            EventManager.OnTimerTick += OnTimerTick;
        }

        public override void Deactivate()
        {
            EventManager.OnClientJobCreated -= OnClientJobCreated;
            EventManager.OnClientJobAccepted -= OnClientJobAccepted;
            EventManager.OnClientJobRejected -= OnClientJobRejected;
            EventManager.OnJobAssigned -= OnJobAssigned;
            EventManager.OnJobUnassigned -= OnJobUnassigned;
            EventManager.OnJobCancelled -= OnJobCancelled;
            EventManager.OnTimerTick -= OnTimerTick;

            ClientId = null;
            _client = null;
            base.Deactivate();
        }

        private void OnClientJobCreated(Client client, Job job)
        {
            if (_client == null) return;
            if (client.Id != _client.Id) return;
            _client.TripJob = job;
            UpdateVisuals();
        }

        private void OnClientJobAccepted(Client client, Job job)
        {
            if (_client == null) return;
            if (client.Id != _client.Id) return;
            UpdateVisuals();
        }

        private void OnClientJobRejected(Client client)
        {
            if (_client == null) return;
            if (client.Id != _client.Id) return;
            // Trip job removed by manager
            UpdateVisuals();
        }

        private void OnJobAssigned(Job job)
        {
            if (_client == null || _client.TripJob == null) return;
            if (job.Id != _client.TripJob.Id) return;
            UpdateVisuals();
        }

        private void OnJobUnassigned(Job job)
        {
            if (_client == null || _client.TripJob == null) return;
            if (job.Id != _client.TripJob.Id) return;
            UpdateVisuals();
        }

        private void OnJobCancelled(Job job)
        {
            if (_client == null) return;
            if (_client.TripJob != null && job.Id == _client.TripJob.Id)
            {
                _client.TripJob = null; // removed by manager as well
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (_client == null) return;

            if (_clientNameText != null)
                _clientNameText.text = _client.Name ?? string.Empty;

            if (_clientInformationText != null)
                _clientInformationText.text = $"{_client.Profession} • {_client.Age}";

            var job = _client.TripJob;
            bool hasJob = job != null;
            EJobState state = hasJob ? job.State : EJobState.Waiting;

            // Waiting group
            bool showWaiting = hasJob && state == EJobState.Waiting;
            if (_clientDestinationText != null) _clientDestinationText.gameObject.SetActive(showWaiting);
            if (_clientDistanceText != null) _clientDistanceText.gameObject.SetActive(showWaiting);
            if (_clientPrizeText != null) _clientPrizeText.gameObject.SetActive(showWaiting);
            if (_acceptButton != null) _acceptButton.gameObject.SetActive(showWaiting);
            if (_rejectButton != null) _rejectButton.gameObject.SetActive(showWaiting);
            if (_newIndicator != null) _newIndicator.SetActive(showWaiting);

            if (showWaiting)
            {
                if (_clientDestinationText != null) _clientDestinationText.text = job.DestinationName;
                if (_clientDistanceText != null) _clientDistanceText.text = $"{job.Distance:F1} km";
                if (_clientPrizeText != null) _clientPrizeText.text = $"{job.BaseReward:F0}";
            }

            // Accepted group
            bool showAccepted = hasJob && state == EJobState.Accepted;
            if (_acceptedIndicator != null) _acceptedIndicator.SetActive(showAccepted);
            if (_cancelButton != null) _cancelButton.gameObject.SetActive(showAccepted);
            if (_instantFinishButton != null) _instantFinishButton.gameObject.SetActive(showAccepted);
            if (_remainingTimeText != null) _remainingTimeText.gameObject.SetActive(showAccepted && job.IsActive);
            if (_assignedDriverNameText != null) _assignedDriverNameText.gameObject.SetActive(showAccepted && job.IsActive);

            if (showAccepted && job.IsActive)
            {
                var assigned = DriverManager.Drivers?.FirstOrDefault(d => d.CurrentJob != null && d.CurrentJob.Id == job.Id);
                if (assigned != null && _assignedDriverNameText != null) _assignedDriverNameText.text = assigned.DriverName;
            }
            else
            {
                if (_assignedDriverNameText != null) _assignedDriverNameText.text = string.Empty;
                if (_remainingTimeText != null) _remainingTimeText.text = string.Empty;
            }
        }

        private void OnAcceptClicked()
        {
            if (_client == null) return;
            ClientManager.AcceptClientJob(_client);
        }

        private void OnRejectClicked()
        {
            if (_client == null) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ClientManager.RejectClientJob(_client, now);
        }

        private void OnCancelClicked()
        {
            if (_client == null || _client.TripJob == null) return;
            ClientManager.CancelAcceptedJob(_client.TripJob);
        }

        private void OnInstantFinishClicked()
        {
            if (_client == null || _client.TripJob == null) return;
            ClientManager.CancelAcceptedJob(_client.TripJob);
        }

        private void OnTimerTick(long unixTime)
        {
            if (_client == null || _client.TripJob == null) return;
            var job = _client.TripJob;
            if (!job.IsActive)
            {
                if (_remainingTimeText != null) _remainingTimeText.text = string.Empty;
                return;
            }

            var assignedDriver = DriverManager.Drivers?.FirstOrDefault(d => d.CurrentJob != null && d.CurrentJob.Id == job.Id);
            if (assignedDriver == null) return;

            float elapsed = (float)(unixTime - assignedDriver.JobStartUnix);
            float remaining = job.BaseDuration - elapsed;
            if (_remainingTimeText != null)
            {
                if (remaining <= 0)
                    _remainingTimeText.text = "0:00";
                else
                {
                    int s = Mathf.CeilToInt(remaining);
                    int mins = s / 60;
                    int secs = s % 60;
                    _remainingTimeText.text = string.Format("{0}:{1:00}", mins, secs);
                }
            }
        }
    }
}
