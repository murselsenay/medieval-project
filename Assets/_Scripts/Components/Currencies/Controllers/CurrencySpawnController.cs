using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Modules.ObjectPoolSystem;
using Components.Constants;
using Modules.Economy.Enums;
using Modules.Economy.Managers;
using Modules.EventSystem.Managers;
using NaughtyAttributes;
using Modules.PopupSystem.Managers;

namespace Components.Currencies.Controllers
{
    public class CurrencySpawnController : MonoBehaviour
    {
        [Header("Spawn Settings")]
        private ECurrencyType _currencyType = ECurrencyType.Gold;
        private Transform _spawnPoint;
        [SerializeField] private int _spawnDelayMs = 40;

        [Button]
        public void ShowPopup()
        {
            PopupManager.ShowPopup(Modules.PopupSystem.Enums.EPopup.Jobs);
        }
        private void OnEnable()
        {
            EventManager.OnSpawnCurrencyRequest += OnSpawnRequest;
        }

        private void OnDisable()
        {
            EventManager.OnSpawnCurrencyRequest -= OnSpawnRequest;
        }

        private void OnSpawnRequest(Transform spawnPoint, ECurrencyType type, int amount)
        {
            if (amount <= 0) return;

            _currencyType = type;
            _spawnPoint = spawnPoint;

            CurrencyManager.Add(_currencyType, amount);

            SpawnFromPoolAsync(amount).Forget();
        }

        private async UniTaskVoid SpawnFromPoolAsync(int amount)
        {
            if (_spawnPoint == null) return;

            string key = CurrencyKeys.GetKeyForCurrency(_currencyType);

            for (int i = 0; i < amount; i++)
            {
                CurrencyObjectController obj = null;
                try
                {
                    obj = await ObjectPool.GetObjectAsync<CurrencyObjectController>(null, key);
                    obj.Initialize(amount);
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
}

