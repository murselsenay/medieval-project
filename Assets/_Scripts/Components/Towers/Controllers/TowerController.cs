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
            TowerData data = new TowerData(Enums.ETowerType.Main, 1000, Projectiles.Enums.EProjectileType.Arrow, 10, 2.5f, 1, 15);

            // Use TowerManager to create and register the tower. Create at the provided hexagon if assigned, otherwise at a fallback position.
            if (_hexagonGridController != null)
            {
                var tower = await TowerManager.CreateTowerAt(_towerHolder, _hexagonGridController.Hexagons[0].transform, TowerKeys.MainTower, data);
                // additional per-tower setup if needed
            }
            else
            {
                var tower = await TowerManager.CreateTowerAt(_towerHolder, new Vector3(0, 1, 0), TowerKeys.MainTower, data);
            }
        }

        private void OnEnable()
        {
            InitializeTowers();
        }
    }
}
