using System;
using UnityEngine;
using Components.Towers.Models;
using TMPro;
using UnityEngine.UI;
using Modules.ObjectPoolSystem;
using Scriptables.Singletons;

namespace Components.Towers.Controllers.Build
{
    public class TowerBuildItem : BaseObject
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Image _towerImage;
        [SerializeField] private Image _projectileTypeImage;

        private TowerData _data;
        private Action<TowerData> _onBuy;

        public void Initialize(TowerData data, Action<TowerData> onBuy)
        {
            _data = data;
            _onBuy = onBuy;

            if (_titleText != null)
                _titleText.text = data.TowerType.ToString();

            if (_costText != null)
                _costText.text = $"Buy <sprite=0> {data.BuildingCost}";

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveAllListeners();
                _buyButton.onClick.AddListener(() => { _onBuy?.Invoke(_data); });
            }

            _towerImage.sprite = data.Sprite;
            _projectileTypeImage.sprite = ResourceWarehouse.Instance.GetProjectileIconSprite(data.ProjectileType);
        }
    }
}
