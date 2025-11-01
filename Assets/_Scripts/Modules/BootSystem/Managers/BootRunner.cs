using System.Threading.Tasks;
using UnityEngine;
using Modules.Logger;

namespace Modules.BootSystem.Managers
{
    public class BootRunner : MonoBehaviour
    {
        private static bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameStart()
        {
            if (_started) return;
            _started = true;
            var go = new GameObject("BootRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<BootRunner>();
        }

        private async void Start()
        {
            // Load BootManager from Resources/BootManager.asset (you must place it there)
            var manager = Resources.Load<Scriptables.BootManager>("Scriptables/Boot/BootManager");
            if (manager == null)
            {
                DebugLogger.LogWarning("BootManager not found in Resources. Skipping boot.");
                return;
            }

            await manager.RunBootAsync();
            DebugLogger.Log("Boot sequence completed.");
        }
    }
}
