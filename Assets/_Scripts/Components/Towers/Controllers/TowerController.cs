using Components.Constants;
using Components.Towers.Models;
using Modules.ObjectPoolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Modules.TowerSystem.Managers;
using NaughtyAttributes;
using Components.Tiles.Controllers;

namespace Components.Towers.Controllers
{
    public class TowerController : MonoBehaviour
    {
        [SerializeField] private Transform _towerHolder;
        [SerializeField] private HexagonGridController _hexagonGridController;
        [SerializeField] private TowerInformationController _towerInformationController;
        private List<MainTower> _towers;

        [Button]
        public async void InitializeTowers()
        {
            // Try to get the default "Main" tower data from TowerManager buildables.
            // Fall back to a hardcoded TowerData if not available.
            TowerData data;
            if (!TowerManager.TryGetDefaultTowerData(Enums.ETowerType.Main, out data))
            {
                data = new TowerData(Enums.ETowerType.Main, 1000, Projectiles.Enums.EProjectileType.Arrow, 10, 2.5f, 1, 15, 10);
            }

            // determine pool key for main tower
            string poolKey = TowerKeys.MainTower;

            // Use TowerManager to create and register the tower. Create at the provided hexagon if assigned, otherwise at a fallback position.
            if (_hexagonGridController != null && _hexagonGridController.Hexagons != null && _hexagonGridController.Hexagons.Count > 0)
            {
                var tower = await TowerManager.CreateTowerAt(_towerHolder, _hexagonGridController.Hexagons[0].transform, poolKey, data);
                // additional per-tower setup if needed
            }
            else
            {
                var tower = await TowerManager.CreateTowerAt(_towerHolder, new Vector3(0, 1, 0), poolKey, data);
            }
        }

        private void OnEnable()
        {
            InitializeTowers();
        }
    }
}
