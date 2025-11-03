using AYellowpaper.SerializedCollections;
using Modules.Economy.Enums;
using Modules.VehicleSystem.Enums;
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
        [BHeader("Currencies")]
        [SerializeField] private SerializedDictionary<ECurrencyType, Sprite> _currencySprites;
        [BHeader("Drivers")]
        [SerializeField] private SerializedDictionary<string, Sprite> _driverPortraits;
        [BHeader("Taxis")]
        [SerializeField] private SerializedDictionary<EVehicleType, Sprite> _vehicleSprites;

        public Sprite GetCurrencySprite(ECurrencyType currencyType) => _currencySprites[currencyType];

        public Sprite GetDriverPortrait(string driverName) => _driverPortraits[driverName];
        public Sprite GetVehicleSprite(EVehicleType vehicleType) => _vehicleSprites[vehicleType];

    }
}
