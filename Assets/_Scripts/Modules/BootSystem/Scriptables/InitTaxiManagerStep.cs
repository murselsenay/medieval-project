using Cysharp.Threading.Tasks;
using Modules.BootSystem.Models;
using Modules.TaxiSystem.Managers;
using UnityEngine;

namespace Modules.BootSystem.Scriptables
{
    [CreateAssetMenu(menuName = "Boot/Steps/InitTaxiManager")]
    public class InitTaxiManagerStep : BootStep
    {
        public override UniTask<BootStepResult> ExecuteAsync()
        {
            try
            {
                TaxiManager.Init();
                return UniTask.FromResult(new BootStepResult { Success = true, Message = "TaxiManager initialized" });
            }
            catch (System.Exception ex)
            {
                return UniTask.FromResult(new BootStepResult { Success = false, Message = ex.Message });
            }
        }
    }
}