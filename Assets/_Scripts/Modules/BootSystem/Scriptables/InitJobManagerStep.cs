using System.Threading.Tasks;
using UnityEngine;
using Modules.BootSystem.Models;
using Modules.JobSystem.Managers;
using Cysharp.Threading.Tasks;

namespace Modules.BootSystem.Scriptables
{
    [CreateAssetMenu(menuName = "Boot/Steps/InitJobManager")]
    public class InitJobManagerStep : BootStep
    {
        public override UniTask<BootStepResult> ExecuteAsync()
        {
            try
            {
                JobManager.Init();
                return UniTask.FromResult(new BootStepResult { Success = true, Message = "JobManager initialized" });
            }
            catch (System.Exception ex)
            {
                return UniTask.FromResult(new BootStepResult { Success = false, Message = ex.Message });
            }
        }
    }
}
