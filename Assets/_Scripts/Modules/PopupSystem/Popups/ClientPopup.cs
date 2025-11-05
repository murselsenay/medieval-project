using Modules.PopupSystem.Components;
using UnityEngine;
using System.Collections.Generic;
using Modules.ClientSystem.Managers;
using Modules.ClientSystem.Components;
using Cysharp.Threading.Tasks;
using Modules.AdressableSystem;
using Components.Constants;
using Modules.ObjectPoolSystem;
using Modules.Logger;
using Modules.EventSystem.Managers;

namespace Modules.PopupSystem.Popups
{
    public class ClientPopup : BasePopup
    {
        [SerializeField] private Transform _clientItemHolder;

        private readonly List<ClientItem> _spawnedItems = new List<ClientItem>();
        private readonly Dictionary<string, ClientItem> _itemsByClientId = new Dictionary<string, ClientItem>();

        public override void Init()
        {
            base.Init();

            if (_clientItemHolder != null)
            {
                var ap = _clientItemHolder.transform.localPosition;
                ap.y =0f;
                _clientItemHolder.transform.localPosition = ap;
            }

            PopulateClients().Forget();
        }

        public override void Activate()
        {
            base.Activate();
            EventManager.OnClientJobCreated += OnClientJobCreated;
            EventManager.OnClientJobAccepted += OnClientJobAccepted;
            EventManager.OnClientJobRejected += OnClientJobRejected;
            EventManager.OnJobCancelled += OnJobCancelled;
        }

        public override void Deactivate()
        {
            EventManager.OnClientJobCreated -= OnClientJobCreated;
            EventManager.OnClientJobAccepted -= OnClientJobAccepted;
            EventManager.OnClientJobRejected -= OnClientJobRejected;
            EventManager.OnJobCancelled -= OnJobCancelled;
            base.Deactivate();
        }

        private void OnClientJobCreated(Modules.ClientSystem.Models.Client client, Modules.JobSystem.Models.Job job)
        {
            // add or update item for this client
            AddOrUpdateClientItem(client).Forget();
        }

        private void OnClientJobAccepted(Modules.ClientSystem.Models.Client client, Modules.JobSystem.Models.Job job)
        {
            // update existing item visuals for accepted state
            UpdateClientItem(client);
        }

        private void OnClientJobRejected(Modules.ClientSystem.Models.Client client)
        {
            // remove the client's item
            RemoveClientItem(client?.Id);
        }

        private void OnJobCancelled(Modules.JobSystem.Models.Job job)
        {
            if (job == null) return;
            // find owner client and remove its item
            var owner = ClientManager.AllClients?.Find(c => c != null && c.TripJob != null && c.TripJob.Id == job.Id);
            if (owner != null) RemoveClientItem(owner.Id);
        }

        public async UniTask PopulateClients()
        {
            if (_clientItemHolder != null)
            {
                var ap0 = _clientItemHolder.transform.localPosition;
                ap0.y =0f;
                _clientItemHolder.transform.localPosition = ap0;
            }

            ClearClients();

            var addressToUse = AddressableKeys.ClientItem;
            if (string.IsNullOrEmpty(addressToUse))
            {
                DebugLogger.LogError("ClientPopup: ClientItem prefab or address is not assigned.");
                return;
            }

            // Use ActiveClients to populate
            var clients = ClientManager.ActiveClients;
            if (clients == null || clients.Count ==0) return;

            foreach (var client in clients)
            {
                if (client == null) continue;

                // create and register item
                await AddOrUpdateClientItem(client);
            }

            if (_clientItemHolder != null)
            {
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_clientItemHolder as RectTransform);
            }
        }

        private async UniTask AddOrUpdateClientItem(Modules.ClientSystem.Models.Client client)
        {
            if (client == null) return;

            if (_itemsByClientId.TryGetValue(client.Id, out var existing) && existing != null)
            {
                // ensure parent and update visuals
                if (existing.transform.parent != _clientItemHolder)
                    existing.transform.SetParent(_clientItemHolder, false);
                existing.Init(client);
                return;
            }

            var addressToUse = AddressableKeys.ClientItem;
            ClientItem item = null;
            try
            {
                item = await ObjectPool.GetObjectAsync<ClientItem>(_clientItemHolder, addressToUse);
            }
            catch
            {
                item = null;
            }

            if (item == null) return;

            // Force parent under holder in case pool returned it under PoolHolder
            if (_clientItemHolder != null && item.transform.parent != _clientItemHolder)
                item.transform.SetParent(_clientItemHolder, false);

            item.Init(client);
            _spawnedItems.Add(item);
            _itemsByClientId[client.Id] = item;

            if (_clientItemHolder != null)
            {
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_clientItemHolder as RectTransform);
            }
        }

        private void UpdateClientItem(Modules.ClientSystem.Models.Client client)
        {
            if (client == null) return;
            if (_itemsByClientId.TryGetValue(client.Id, out var existing) && existing != null)
            {
                if (existing.transform.parent != _clientItemHolder)
                    existing.transform.SetParent(_clientItemHolder, false);
                existing.Init(client);
            }
        }

        private void RemoveClientItem(string clientId)
        {
            if (string.IsNullOrEmpty(clientId)) return;
            if (_itemsByClientId.TryGetValue(clientId, out var existing) && existing != null)
            {
                try { ObjectPool.ReturnToPool(existing); } catch { if (existing.gameObject != null) UnityEngine.Object.Destroy(existing.gameObject); }
                _spawnedItems.Remove(existing);
                _itemsByClientId.Remove(clientId);

                if (_clientItemHolder != null)
                {
                    Canvas.ForceUpdateCanvases();
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_clientItemHolder as RectTransform);
                }
            }
        }

        public void ClearClients()
        {
            foreach (var it in _spawnedItems)
            {
                if (it == null) continue;
                try { ObjectPool.ReturnToPool(it); } catch { if (it.gameObject != null) UnityEngine.Object.Destroy(it.gameObject); }
            }

            _spawnedItems.Clear();
            _itemsByClientId.Clear();
        }

        protected override void OnAfterClose()
        {
            ClearClients();
        }

        private void OnDestroy()
        {
            ClearClients();
        }
    }
}

