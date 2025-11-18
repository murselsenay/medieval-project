using AYellowpaper.SerializedCollections;
using Modules.Economy.Enums;
using Scriptables.Constants;
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

        public Sprite GetCurrencySprite(ECurrencyType currencyType) => _currencySprites[currencyType];
    }
}
