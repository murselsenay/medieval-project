using Components.Constants;
using Cysharp.Threading.Tasks;
using Modules.AdressableSystem;
using Modules.ClientSystem.Components;
using Modules.ClientSystem.Managers;
using Modules.EventSystem.Managers;
using Modules.Logger;
using Modules.ObjectPoolSystem;
using Modules.PopupSystem.Components;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Modules.ClientSystem.Models;
using TMPro;

namespace Modules.PopupSystem.Popups
{
    public class ClientPopup : BasePopup
    {
        [SerializeField] private Transform _clientItemHolder;
        [SerializeField] private ScrollRect _scrollRect;
        [BHeader("Taxis")]
        [SerializeField] private GameObject _taxiHolder;
        [SerializeField] private TMP_Text _taxiHeaderText;

        private readonly List<ClientItem> _spawnedItems = new List<ClientItem>();
        private readonly Dictionary<string, ClientItem> _itemsByClientId = new Dictionary<string, ClientItem>();

        // store client for which we opened the taxi selector
        private Client _selectedClientForTaxi;

        public override void Init()
        {
            if (_clientItemHolder != null)
            {
                var ap = _clientItemHolder.transform.localPosition;
                ap.y = 0f;
                _clientItemHolder.transform.localPosition = ap;
            }

            PopulateClients().Forget();
        }

        public override void Activate()
        {
            if (_scrollRect != null)
            {
                _scrollRect.StopMovement();
                _scrollRect.velocity = Vector2.zero;
                _scrollRect.enabled = false;
            }

            base.Activate();

            EventManager.OnShowTaxisRequested += OnShowTaxisRequested;
            EventManager.OnClientJobCreated += OnClientJobCreated;
            EventManager.OnClientJobAccepted += OnClientJobAccepted;
            EventManager.OnClientJobRejected += OnClientJobRejected;
            EventManager.OnJobCancelled += OnJobCancelled;
        }

        public override void Deactivate()
        {
            EventManager.OnShowTaxisRequested -= OnShowTaxisRequested;
            EventManager.OnClientJobCreated -= OnClientJobCreated;
            EventManager.OnClientJobAccepted -= OnClientJobAccepted;
            EventManager.OnClientJobRejected -= OnClientJobRejected;
            EventManager.OnJobCancelled -= OnJobCancelled;

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                _scrollRect.velocity = Vector2.zero;
                _scrollRect.enabled = true;
            }

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

            if (_selectedClientForTaxi != null && client.Id == _selectedClientForTaxi.Id)
                OnHideTaxisRequested();
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
                ap0.y = 0f;
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
            if (clients == null || clients.Count == 0) return;

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

        private void ClearClients()
        {
            foreach (var it in _spawnedItems)
            {
                if (it == null) continue;
                try { ObjectPool.ReturnToPool(it); } catch { if (it.gameObject != null) UnityEngine.Object.Destroy(it.gameObject); }
            }

            _spawnedItems.Clear();
            _itemsByClientId.Clear();
        }

        private void OnShowTaxisRequested(Client client)
        {
            _selectedClientForTaxi = client;
            if (_taxiHolder != null)
                _taxiHolder.SetActive(true);

            if (_taxiHeaderText != null)
                _taxiHeaderText.text = $"Select a taxi for {client.Name}";
        }

        private void OnHideTaxisRequested()
        {
            _selectedClientForTaxi = null;

            if (_taxiHolder != null)
                _taxiHolder.SetActive(false);
        }

        protected override void OnAfterShow()
        {
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                _scrollRect.velocity = Vector2.zero;
                _scrollRect.enabled = true;
            }
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

