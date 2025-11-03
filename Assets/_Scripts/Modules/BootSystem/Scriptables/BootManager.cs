using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Modules.BootSystem.Models;
using Modules.Logger;
using Cysharp.Threading.Tasks;

namespace Modules.BootSystem.Scriptables
{
    [CreateAssetMenu(menuName = "Boot/BootManager")]
    public class BootManager : ScriptableObject
    {
        [Tooltip("Steps to run on boot. The list order shown in inspector will be used.")]
        public List<BootStep> Steps = new List<BootStep>();

        public bool IsRunning { get; private set; }
        public bool HasRun { get; private set; }

        public async UniTask RunBootAsync()
        {
            if (IsRunning || HasRun) return;
            IsRunning = true;

            // Use inspector list order directly
            var ordered = Steps.Where(s => s != null).ToList();

            foreach (var step in ordered)
            {
                if (step == null) continue;

                try
                {
                    var res = await step.ExecuteAsync();
                    if (res == null) continue;
                    if (!res.Success)
                    {
                        DebugLogger.LogError($"Boot step '{step.name}' failed: {res.Message}");
                    }
                }
                catch (System.Exception ex)
                {
                    DebugLogger.LogException(ex);
                }
            }

            IsRunning = false;
            HasRun = true;
        }
    }
}
