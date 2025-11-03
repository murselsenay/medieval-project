using Modules.BootSystem.Models;
using Modules.DriverSystem.Managers;
using System.Threading.Tasks;
using UnityEngine;
using Modules.Logger;
using Cysharp.Threading.Tasks;

namespace Modules.BootSystem.Scriptables
{
    [CreateAssetMenu(menuName = "Boot/Steps/InitDriverManager")]
    public class InitDriverManagerStep : BootStep
    {
        public override UniTask<BootStepResult> ExecuteAsync()
        {
            try
            {
                DriverManager.Init();
                return UniTask.FromResult(new BootStepResult { Success = true, Message = "DriverManager initialized" });
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException(ex);
                return UniTask.FromResult(new BootStepResult { Success = false, Message = ex.Message });
            }
        }
    }
}
