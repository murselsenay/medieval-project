using Modules.BootSystem.Models;
using Modules.TimerSystem.Managers;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Modules.BootSystem.Scriptables
{
    [CreateAssetMenu(menuName = "Boot/Steps/InitTimerManager")]
    public class InitTimerManagerStep : BootStep
    {
        public override UniTask<BootStepResult> ExecuteAsync()
        {
            try
            {
                TimerManager.Init();
                return UniTask.FromResult(new BootStepResult { Success = true });
            }
            catch (System.Exception ex)
            {
                return UniTask.FromResult(new BootStepResult { Success = false, Message = ex.Message });
            }
        }
    }
}