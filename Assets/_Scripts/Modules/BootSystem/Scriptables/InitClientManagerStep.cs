using System.Threading.Tasks;
using UnityEngine;
using Modules.BootSystem.Models;
using Modules.ClientSystem.Managers;
using Cysharp.Threading.Tasks;

namespace Modules.BootSystem.Scriptables
{
    [CreateAssetMenu(menuName = "Boot/Steps/InitClientManager")]
    public class InitClientManagerStep : BootStep
    {
        public override UniTask<BootStepResult> ExecuteAsync()
        {
            try
            {
                ClientManager.Init();
                return UniTask.FromResult(new BootStepResult { Success = true, Message = "ClientManager initialized" });
            }
            catch (System.Exception ex)
            {
                return UniTask.FromResult(new BootStepResult { Success = false, Message = ex.Message });
            }
        }
    }
}
