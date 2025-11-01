using System.Threading.Tasks;
using UnityEngine;
using Modules.BootSystem.Models;
using Modules.JobSystem.Managers;

namespace Modules.BootSystem.Scriptables
{
    [CreateAssetMenu(menuName = "Boot/Steps/InitJobManager")]
    public class InitJobManagerStep : BootStep
    {
        public override Task<BootStepResult> ExecuteAsync()
        {
            try
            {
                JobManager.Init();
                return Task.FromResult(new BootStepResult { Success = true, Message = "JobManager initialized" });
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(new BootStepResult { Success = false, Message = ex.Message });
            }
        }
    }
}
