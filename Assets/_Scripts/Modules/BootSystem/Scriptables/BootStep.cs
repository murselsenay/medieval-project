using System.Threading.Tasks;
using UnityEngine;
using Modules.BootSystem.Models;

namespace Modules.BootSystem.Scriptables
{
    public abstract class BootStep : ScriptableObject
    {
        [Tooltip("Optional display name for the step")]
        public string StepName;

        // Override this to perform sync or async initialization. Return a BootStepResult with success/failure info.
        public virtual Task<BootStepResult> ExecuteAsync()
        {
            return Task.FromResult(new BootStepResult { Success = true, Message = "No-op" });
        }
    }
}
