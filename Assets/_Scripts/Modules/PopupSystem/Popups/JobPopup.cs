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
    }
}