using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Modules.JobSystem.Models;

namespace Modules.JobSystem.Components
{
    public class JobItem : BaseObject
    {
        [SerializeField] private RectTransform _subholder;
        [SerializeField] private TMP_Text _jobDescriptionText;
        [SerializeField] private Button _driversButton;
        [SerializeField] private GameObject[] _difficulties;

        private float _driversClosedSize =100;
        private float _driversOpenedSize =475;
        [SerializeField] private float _driversAnimDuration =0.25f;

        private bool _driversOpen = false;

        public void Init(Job job)
        {
            if (job == null) return;

            _driversOpen = false;

            if (_driversButton != null)
            {
                _driversButton.onClick.RemoveAllListeners();
                _driversButton.onClick.AddListener(ToggleDrivers);
            }

            if (_subholder != null)
            {
                var sd = _subholder.sizeDelta;
                sd.y = _driversClosedSize;
                _subholder.sizeDelta = sd;
            }

            SetDescription(GenerateDescription(job));
            SetDifficulty((int)job.Difficulty);
        }

        private string GenerateDescription(Job job)
        {
            if (job == null) return string.Empty;
            return $"{job.DestinationName} · {job.Distance:F1} km · {job.PassengerCount} pax";
        }

        public void OpenDrivers()
        {
            if (_driversOpen) return;
            _driversOpen = true;

            if (_subholder == null) return;

            Vector2 target = new Vector2(_subholder.sizeDelta.x, _driversOpenedSize);
            _subholder.DOSizeDelta(target, _driversAnimDuration).SetUpdate(true);
        }

        public void CloseDrivers()
        {
            if (!_driversOpen) return;
            _driversOpen = false;

            if (_subholder == null) return;

            Vector2 target = new Vector2(_subholder.sizeDelta.x, _driversClosedSize);
            _subholder.DOSizeDelta(target, _driversAnimDuration).SetUpdate(true);
        }

        public void ToggleDrivers()
        {
            if (_driversOpen) CloseDrivers(); else OpenDrivers();
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
    }
}