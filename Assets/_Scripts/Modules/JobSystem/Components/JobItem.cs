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

    }
}