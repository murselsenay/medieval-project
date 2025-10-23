using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Modules.EventSystem.Managers;
using Components.Towers.Models;
using Modules.TowerSystem.Managers;
using TMPro;
using UnityEngine.UI;
using Modules.Economy.Managers;
using Modules.Economy.Enums;
using Components.Tiles.Controllers;
using Components.Tiles.Enums;
using Components.Constants;
using Modules.ObjectPoolSystem;

namespace Components.Towers.Controllers.Build
{
    public class TowerBuildingController : MonoBehaviour
    {
        [SerializeField] private Transform _towerBuildItemHolder;
        [SerializeField] private GameObject _towerBuildUI;

        private HexagonController _selectedHex;

        private void Awake()
        {
            if (_towerBuildUI != null)
                _towerBuildUI.SetActive(false);
        }

        private void OnEnable()
        {
            EventManager.OnHexagonSelected += OnHexagonSelected;
            EventManager.OnHexagonDeselected += OnHexagonDeselected;
        }

        private void OnDisable()
        {
            EventManager.OnHexagonSelected -= OnHexagonSelected;
            EventManager.OnHexagonDeselected -= OnHexagonDeselected;
        }

        private void OnHexagonSelected(HexagonController hex)
        {
            _selectedHex = hex;

            if (_selectedHex == null)
            {
                HideUI();
                return;
            }

            // only show build UI for selected hex
            if (_selectedHex.State != EHexagonState.Selected)
            {
                HideUI();
                return;
            }

            ShowUI();
        }

        private void OnHexagonDeselected(HexagonController hex)
        {
            // if there's no currently selected hex, hide the UI
            if (HexagonController.CurrentlySelected == null)
            {
                HideUI();
            }
            else
            {
                // update _selectedHex to current selection
                _selectedHex = HexagonController.CurrentlySelected;
            }
        }

        private void ShowUI()
        {
            if (_towerBuildUI != null)
                _towerBuildUI.SetActive(true);

            PopulateBuildItems();
        }

        private void HideUI()
        {
            if (_towerBuildUI != null)
                _towerBuildUI.SetActive(false);

            ClearHolder();
            _selectedHex = null;
        }

        private void ClearHolder()
        {
            if (_towerBuildItemHolder == null) return;
            for (int i = _towerBuildItemHolder.childCount - 1; i >= 0; i--)
            {
                var child = _towerBuildItemHolder.GetChild(i).GetComponent<BaseObject>();
                if (child != null)
                {
                    ObjectPool.ReturnToPool(child);
                }
                else
                {
                    Destroy(_towerBuildItemHolder.GetChild(i).gameObject);
                }
            }
        }

        private async void PopulateBuildItems()
        {
            ClearHolder();
            if (_towerBuildItemHolder == null) return;

            var buildables = TowerManager.GetBuildableTowers();
            foreach (var t in buildables)
            {
                // obtain pooled TowerBuildItem
                var item = await ObjectPool.GetObjectAsync<TowerBuildItem>(_towerBuildItemHolder);
                if (item == null) continue;

                item.transform.SetParent(_towerBuildItemHolder);
                item.Initialize(t, OnBuyRequested);
            }
        }

        private async void OnBuyRequested(TowerData data)
        {
            if (_selectedHex == null) return;

            int cost = data.BuildingCost;
            //if (cost > 0)
            //{
            //    if (!CurrencyManager.TryConsume(ECurrencyType.Gold, cost))
            //    {
            //        // not enough gold
            //        return;
            //    }
            //}

            // determine pool key from tower type
            string poolKey = data.TowerType == Components.Towers.Enums.ETowerType.Area ? TowerKeys.AreaTower : TowerKeys.MainTower;

            var tower = await TowerManager.CreateTowerAt(_selectedHex.transform, _selectedHex.transform, poolKey, data);
            if (tower != null)
            {
                // If this hex is currently selected, deselect it first so the static CurrentlySelected is cleared
                if (HexagonController.CurrentlySelected == _selectedHex)
                {
                    _selectedHex.Deselect();
                }

                // mark hex as occupied/disabled
                if (_selectedHex != null)
                    _selectedHex.State = EHexagonState.Disabled;

                HideUI();
            }
        }
    }
}
