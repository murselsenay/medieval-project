using Cysharp.Threading.Tasks;
using Modules.AdressableSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Modules.ObjectPoolSystem
{
    public static class ObjectPool
    {
        // Pools per type+key for O(1) reuse
        private static readonly Dictionary<string, Queue<BaseObject>> _pools = new Dictionary<string, Queue<BaseObject>>();

        private static List<BaseObject> _prefabs = new List<BaseObject>();

        // Track addressable-instantiated GameObjects so we can ReleaseInstance when destroying
        private static HashSet<GameObject> _addressableInstances = new HashSet<GameObject>();

        // Map instance -> poolKey so manual returns always enqueue to correct pool
        private static readonly Dictionary<BaseObject, string> _instancePoolKeys = new Dictionary<BaseObject, string>();

        // Store explicit handlers so we can unsubscribe to avoid re-entrancy
        private static readonly Dictionary<BaseObject, Action<BaseObject>> _deactivatedHandlers = new Dictionary<BaseObject, Action<BaseObject>>();
        private static readonly Dictionary<BaseObject, Action<BaseObject>> _destroyedHandlers = new Dictionary<BaseObject, Action<BaseObject>>();

        // Optional PoolHolder to keep pooled objects organized in hierarchy
        private static GameObject _poolHolder;

        public static async UniTask InitAsync()
        {
            EnsurePoolHolder();
            await LoadPrefabs();
        }

        public static void Reset()
        {
            ResetPool();
        }

        private static void EnsurePoolHolder()
        {
            if (_poolHolder == null)
            {
                _poolHolder = GameObject.Find("PoolHolder");
                if (_poolHolder == null)
                {
                    _poolHolder = new GameObject("PoolHolder");
                    UnityEngine.Object.DontDestroyOnLoad(_poolHolder);
                }
            }

            // Ensure PoolHolder is at root and has a safe transform to avoid inheriting scale from other parents
            if (_poolHolder.transform.parent != null)
                _poolHolder.transform.SetParent(null);

            _poolHolder.transform.localScale = Vector3.one;
            _poolHolder.transform.localRotation = Quaternion.identity;
            _poolHolder.transform.localPosition = Vector3.zero;
        }

        private static async UniTask LoadPrefabs()
        {
            List<GameObject> loaded = await AddressableManager.LoadAllAsync<GameObject>("prefabs");
            if (loaded == null || loaded.Count == 0)
                return;

            var baseObjects = loaded
                .Where(go => go != null)
                .Select(go => go.GetComponent<BaseObject>())
                .Where(bo => bo != null)
                .ToList();

            if (baseObjects.Count == 0)
                return;

            _prefabs.Clear();
            _prefabs.AddRange(baseObjects);
        }

        private static string GetPoolKey<T>(string key) where T : BaseObject
        {
            return typeof(T).FullName + "_" + (key ?? string.Empty);
        }

        private static string GetPoolKey(BaseObject obj)
        {
            return obj.GetType().FullName + "_" + (obj.Key ?? string.Empty);
        }
        private static void AttachHandlers(BaseObject obj, string poolKey)
        {
            if (obj == null) return;
            Action<BaseObject> onDeactivated = (b) => ReturnToPool(b, poolKey);
            Action<BaseObject> onDestroyed = (b) => OnDestroyed(b);

            obj.DeActivated += onDeactivated;
            obj.Destroyed += onDestroyed;

            _deactivatedHandlers[obj] = onDeactivated;
            _destroyedHandlers[obj] = onDestroyed;
        }

        private static void DetachHandlers(BaseObject obj)
        {
            if (obj == null) return;
            if (_deactivatedHandlers.TryGetValue(obj, out var onDeactivated))
            {
                obj.DeActivated -= onDeactivated;
                _deactivatedHandlers.Remove(obj);
            }
            if (_destroyedHandlers.TryGetValue(obj, out var onDestroyed))
            {
                obj.Destroyed -= onDestroyed;
                _destroyedHandlers.Remove(obj);
            }
        }

        public static async UniTask PrewarmAsync<T>(int count, Transform parent = null, string key = "") where T : BaseObject
        {
            if (count <= 0) return;

            EnsurePoolHolder();
            string poolKey = GetPoolKey<T>(key);
            if (!_pools.TryGetValue(poolKey, out var queue))
            {
                queue = new Queue<BaseObject>();
                _pools[poolKey] = queue;
            }

            T prefab = (T)_prefabs.Find(p => p is T && key.Equals(p.Key));
            for (int i = 0; i < count; i++)
            {
                T obj = null;
                if (prefab)
                {
                    obj = UnityEngine.Object.Instantiate(prefab, parent) as T;
                }
                else
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        T comp = await AddressableManager.InstantiateAsync<T>(key, parent);
                        if (comp != null)
                        {
                            obj = comp;
                            _addressableInstances.Add(comp.gameObject);
                        }
                    }
                }

                if (obj == null) continue;

                _instancePoolKeys[obj] = poolKey;

                // ensure stored under pool holder with safe transform
                obj.transform.SetParent(_poolHolder.transform, false);

                DetachHandlers(obj);
                obj.Deactivate();

                AttachHandlers(obj, poolKey);

                if (!queue.Contains(obj))
                    queue.Enqueue(obj);
            }
        }

        public static T GetObject<T>(Transform parent = null, string key = "") where T : BaseObject
        {
            EnsurePoolHolder();
            string poolKey = GetPoolKey<T>(key);

            if (_pools.TryGetValue(poolKey, out var queue) && queue.Count > 0)
            {
                var item = queue.Dequeue() as T;
                if (item != null)
                {
                    // set parent preserving local transform (avoid inheriting unexpected world transform)
                    item.transform.SetParent(parent ?? _poolHolder.transform, false);
                    item.Activate();
                    return item;
                }
            }

            T prefab = (T)_prefabs.Find(p => p is T && key.Equals(p.Key));
            if (prefab)
            {
                T obj = UnityEngine.Object.Instantiate(prefab, parent) as T;
                _instancePoolKeys[obj] = poolKey;
                AttachHandlers(obj, poolKey);
                obj.Activate();
                return obj;
            }

            Debug.LogError($"Prefab not found for type {typeof(T).FullName} with key '{key}'. Use GetObjectAsync to instantiate addressable assets.");
            return null;
        }

        public static async UniTask<T> GetObjectAsync<T>(Transform parent = null, string key = "") where T : BaseObject
        {
            EnsurePoolHolder();
            string poolKey = GetPoolKey<T>(key);

            if (_pools.TryGetValue(poolKey, out var queue) && queue.Count > 0)
            {
                var item = queue.Dequeue() as T;
                if (item != null)
                {
                    item.transform.SetParent(parent ?? _poolHolder.transform, false);
                    item.Activate();
                    return item;
                }
            }

            T prefab = (T)_prefabs.Find(p => p is T && key.Equals(p.Key));
            if (prefab)
            {
                T obj = UnityEngine.Object.Instantiate(prefab, parent) as T;
                _instancePoolKeys[obj] = poolKey;
                AttachHandlers(obj, poolKey);
                obj.Activate();
                return obj;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"Prefab/address not found for {typeof(T).FullName}. Provide address or ensure prefab is preloaded.");
                return null;
            }

            // instantiate addressable under pool holder to avoid transient root placement
            Transform targetParent = parent ?? _poolHolder.transform;
            T instantiated = await AddressableManager.InstantiateAsync<T>(key, targetParent);
            if (instantiated == null)
            {
                Debug.LogError($"Addressable instantiate failed or component missing for address: {key}");
                return null;
            }

            // Ensure inactive, parent set, then activate to avoid visible root ghost
            try { instantiated.gameObject.SetActive(false); } catch { }

            _addressableInstances.Add(instantiated.gameObject);
            _instancePoolKeys[instantiated] = poolKey;

            AttachHandlers(instantiated, poolKey);

            // Force parent (AddressableManager may not respect parent param)
            instantiated.transform.SetParent(targetParent, false);

            instantiated.Activate();
            return instantiated;
        }

        public static void ReturnToPool(BaseObject obj)
        {
            if (obj == null) return;

            string poolKey;
            if (!_instancePoolKeys.TryGetValue(obj, out poolKey))
            {
                poolKey = GetPoolKey(obj);
            }

            ReturnToPool(obj, poolKey);
        }

        public static void ReturnToPool(BaseObject obj, string poolKey)
        {
            if (obj == null) return;

            EnsurePoolHolder();
            if (!_pools.TryGetValue(poolKey, out var queue))
            {
                queue = new Queue<BaseObject>();
                _pools[poolKey] = queue;
            }

            DetachHandlers(obj);

            // parent under pool holder without changing local transform
            obj.transform.SetParent(_poolHolder.transform, false);
            obj.Deactivate();

            _instancePoolKeys[obj] = poolKey;

            AttachHandlers(obj, poolKey);

            if (!queue.Contains(obj))
                queue.Enqueue(obj);
        }

        public static void ReturnAllActiveOfType<T>() where T : BaseObject
        {
            var active = _instancePoolKeys.Keys.OfType<T>().Where(o => o != null && o.gameObject != null && o.gameObject.activeInHierarchy).ToList();
            foreach (var obj in active)
            {
                try
                {
                    ReturnToPool(obj);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ObjectPool: failed returning instance to pool: {ex.Message}");
                }
            }
        }
        public static void ReturnAllActive()
        {
            var active = _instancePoolKeys.Keys.Where(o => o != null && o.gameObject != null && o.gameObject.activeInHierarchy).ToList();
            foreach (var obj in active)
            {
                try
                {
                    ReturnToPool(obj);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ObjectPool: failed returning instance to pool: {ex.Message}");
                }
            }
        }

        private static void OnDestroyed(BaseObject b)
        {
            if (b == null) return;

            string poolKey = GetPoolKey(b);
            if (_pools.TryGetValue(poolKey, out var queue))
            {
                if (queue.Contains(b))
                {
                    var list = queue.ToList();
                    list.Remove(b);
                    _pools[poolKey] = new Queue<BaseObject>(list);
                }
            }

            if (_instancePoolKeys.ContainsKey(b))
                _instancePoolKeys.Remove(b);

            DetachHandlers(b);

            if (b.gameObject != null && _addressableInstances.Contains(b.gameObject))
            {
                AddressableManager.ReleaseInstance(b.gameObject);
                _addressableInstances.Remove(b.gameObject);
            }
        }

        public static void ResetPool()
        {
            foreach (var kvp in _pools)
            {
                foreach (var obj in kvp.Value)
                {
                    if (obj == null) continue;
                    DetachHandlers(obj);
                    if (obj.gameObject != null && _addressableInstances.Contains(obj.gameObject))
                    {
                        AddressableManager.ReleaseInstance(obj.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(obj.gameObject);
                    }
                }
            }

            _pools.Clear();
            _prefabs.Clear();
            _addressableInstances.Clear();
            _instancePoolKeys.Clear();
            _deactivatedHandlers.Clear();
            _destroyedHandlers.Clear();

            Resources.UnloadUnusedAssets();
        }

        public static void ReturnAllToPool<T>() where T : BaseObject
        {
            string poolKey = GetPoolKey<T>(null);
            if (_pools.TryGetValue(poolKey, out var queue))
            {
                var toDeactivate = queue.Where(obj => obj != null).ToList();
                foreach (var obj in toDeactivate)
                {
                    ReturnToPool(obj, poolKey);
                }
            }
        }
        public static void ReturnAllToPool()
        {
            foreach (var kvp in _pools)
            {
                var queue = kvp.Value;
                var toDeactivate = queue.Where(obj => obj != null).ToList();
                foreach (var obj in toDeactivate)
                {
                    ReturnToPool(obj, kvp.Key);
                }
            }
        }
    }
}
