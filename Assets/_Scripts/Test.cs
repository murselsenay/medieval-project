using UnityEngine;
using Cysharp.Threading.Tasks;
using Modules.ObjectPoolSystem;
using Modules.Economy.Managers;
using Modules.Economy.Enums;
using NaughtyAttributes;
using Components.Currencies.Controllers;
using Components.Constants;

public class Test : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Amounts")]
    [SerializeField] private int _goldAmount = 5;
    [SerializeField] private int _gemAmount = 1;

    [Header("Spawn Timing (ms)")]
    [SerializeField] private int _spawnDelayMs = 40;

    [Button]
    public void SpawnGold()
    {
        CurrencyManager.Add(ECurrencyType.Gold, _goldAmount);
        SpawnGoldFromPoolAsync().Forget();
    }

    [Button]
    public void SpawnGem()
    {
        CurrencyManager.Add(ECurrencyType.Gem, _gemAmount);
        SpawnGemFromPoolAsync().Forget();
    }

    private async UniTaskVoid SpawnGoldFromPoolAsync()
    {
        if (_spawnPoint == null) return;

        for (int i = 0; i < _goldAmount; i++)
        {
            CurrencyObjectController obj = null;
            try
            {
                obj = await ObjectPool.GetObjectAsync<CurrencyObjectController>(null, CurrencyKeys.Gold);
            }
            catch { }

            if (obj != null)
            {
                obj.transform.position = _spawnPoint.position + Vector3.up * 0.5f;
                try { obj.Activate(); } catch { }
            }

            await UniTask.Delay(_spawnDelayMs);
        }
    }

    private async UniTaskVoid SpawnGemFromPoolAsync()
    {
        if (_spawnPoint == null) return;

        for (int i = 0; i < _gemAmount; i++)
        {
            CurrencyObjectController obj = null;
            try
            {
                obj = await ObjectPool.GetObjectAsync<CurrencyObjectController>(null, CurrencyKeys.Gem);
            }
            catch { }

            if (obj != null)
            {
                obj.transform.position = _spawnPoint.position + Vector3.up * 0.5f;
                try { obj.Activate(); } catch { }
            }

            await UniTask.Delay(_spawnDelayMs);
        }
    }
}