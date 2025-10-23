using AYellowpaper.SerializedCollections;
using Components.Projectiles.Enums;
using Components.Towers.Enums;
using Modules.Economy.Enums;
using Scriptables.Constants;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
namespace Scriptables.Singletons
{
    [CreateAssetMenu(menuName = ScriptablePaths.SCRIPTABLES + nameof(ResourceWarehouse), fileName = nameof(ResourceWarehouse))]
    public class ResourceWarehouse : ScriptableSingleton<ResourceWarehouse>
    {
        [BHeader("UI Materials")]
        public Material GrayscaleUIMaterial;
        [BHeader("Materials")]
        public Material HexagonGrayMaterial;
        public Material HexagonGreenMaterial;
        public Material HexagonBrownMaterial;
        public Material HexagonRedMaterial;
        public Material HexagonLightGreenMaterial;
        public Material HexagonSelectedGreenMaterial;
        [BHeader("Currencies")]
        [SerializeField] SerializedDictionary<ECurrencyType, Sprite> _currencySprites;
        [BHeader("Towers")]
        [SerializeField] SerializedDictionary<ETowerType, Sprite> _towerSprites;
        [BHeader("Projectile")]
        [SerializeField] SerializedDictionary<EProjectileType, Sprite> _projectileIconSprites;

        public Sprite GetCurrencySprite(ECurrencyType currencyType) => _currencySprites[currencyType];
        public Sprite GetTowerSprite(ETowerType towerType) => _towerSprites[towerType];
        public Sprite GetProjectileIconSprite(EProjectileType projectileType) => _projectileIconSprites[projectileType];

    }
}
