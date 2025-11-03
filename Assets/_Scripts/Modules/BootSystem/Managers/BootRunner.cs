using System.Threading.Tasks;
using UnityEngine;
using Modules.Logger;
using System.Linq;
using Cysharp.Threading.Tasks;

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
            // Try the expected path first
            var manager = Resources.Load<Scriptables.BootManager>("Scriptables/Boot/BootManager");

            // Fallback: if not found at expected path, try to find any BootManager in Resources
            if (manager == null)
            {
                var all = Resources.LoadAll<Scriptables.BootManager>("");
                if (all != null && all.Length >0)
                {
                    manager = all[0];
                }
            }

            if (manager == null)
            {
                return;
            }

            if (manager.Steps == null || manager.Steps.Count ==0)
            {
            }
            else
            {
            }

            await manager.RunBootAsync();
        }
    }
}
