using System.Threading.Tasks;
using UnityEngine;
using Modules.BootSystem.Models;
using Cysharp.Threading.Tasks;

namespace Modules.BootSystem.Scriptables
{
    public abstract class BootStep : ScriptableObject
    {
        [Tooltip("Optional display name for the step")]
        public string StepName;

        public virtual UniTask<BootStepResult> ExecuteAsync()
        {
            return UniTask.FromResult(new BootStepResult { Success = true, Message = "No-op" });
        }
    }
}
