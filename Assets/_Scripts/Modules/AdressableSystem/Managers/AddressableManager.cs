using Cysharp.Threading.Tasks;
using Modules.Logger;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Modules.AdressableSystem
{
    public static class AddressableManager
    {
        public static async UniTask Initialize()
        {
                
        }

        public static async UniTask<T> LoadAsync<T>(string address) where T : Object
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;

            DebugLogger.LogError($"Addressable load failed: {address}");
            return null;
        }

        public static async UniTask<List<T>> LoadAllAsync<T>(string address) where T : Object
        {
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(address, null);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                return handle.Result.ToList();

            DebugLogger.LogError($"Addressables load failed: {address}");
            return new List<T>();
        }

        public static async UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;

            DebugLogger.LogError($"Addressable instantiate failed: {address}");
            return null;
        }

        public static async UniTask<TComponent> InstantiateAsync<TComponent>(string address, Transform parent = null) where TComponent : Component
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject go = handle.Result;
                if (go == null)
                {
                    DebugLogger.LogError($"Addressable instantiate returned null GameObject: {address}");
                    return null;
                }

                TComponent comp = go.GetComponent<TComponent>();
                if (comp != null)
                    return comp;

                DebugLogger.LogError($"Instantiated addressable does not contain component of type {typeof(TComponent).FullName}: {address}");
                return null;
            }

            DebugLogger.LogError($"Addressable instantiate failed: {address}");
            return null;
        }

        public static void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return;
            Addressables.ReleaseInstance(instance);
        }

        public static void Release<T>(T obj)
        {
            Addressables.Release(obj);
        }
    }
}
