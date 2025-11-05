using AYellowpaper.SerializedCollections;
using Modules.Economy.Enums;
using Modules.VehicleSystem.Enums;
using Scriptables.Constants;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using System.Linq;

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

        [BHeader("Clients")]
        [SerializeField] private SerializedDictionary<string, Sprite> _clientPortraits = new SerializedDictionary<string, Sprite>();
        [SerializeField] private List<string> _femaleClientNames = new List<string> { "Ayse", "Fatma", "Ece", "Selin", "Lara" };
        [SerializeField] private List<string> _maleClientNames = new List<string> { "Ahmet", "Mehmet", "Can", "Ozan", "Deniz", "Umut", "Mert" };

        // Destinations (name -> distance in km)
        [BHeader("Destinations")]
        [SerializeField]
        private SerializedDictionary<string, int> _destinationsDistances = new SerializedDictionary<string, int>
        {
            { "Tavern",3 },
            { "Market",2 },
            { "Harbor",8 },
            { "Castle",12 },
            { "Fields",6 },
            { "Mill",4 },
            { "TownSquare",1 },
            { "Dock",7 },
            { "Temple",5 },
            { "Forest",15 },
            { "Mine",18 },
            { "Barracks",9 },
            { "Gate",2 },
            { "Bridge",3 },
            { "Stables",4 },
            { "Gardens",5 },
            { "Library",6 },
            { "GuildHall",7 },
            { "Slums",3 },
            { "NobleQuarter",10 },
            { "EastGate",8 },
            { "WestGate",8 },
            { "SouthGate",9 },
            { "NorthGate",11 },
            { "StoneBridge",3 },
            { "RiverBank",6 },
            { "Lighthouse",14 },
            { "Windmill",5 },
            { "Orchard",7 },
            { "FishMarket",2 },
            { "HarborMarket",8 }
        };

        public Sprite GetCurrencySprite(ECurrencyType currencyType) => _currencySprites[currencyType];

        public Sprite GetDriverPortrait(string driverName) => _driverPortraits[driverName];
        public Sprite GetVehicleSprite(EVehicleType vehicleType) => _vehicleSprites[vehicleType];

        // Clients
        public IEnumerable<string> FemaleClientNames => _femaleClientNames;
        public IEnumerable<string> MaleClientNames => _maleClientNames;
        public IEnumerable<string> AllClientNames => _femaleClientNames.Concat(_maleClientNames);

        public Sprite GetClientPortrait(string clientName)
        {
            if (_clientPortraits == null) return null;
            if (_clientPortraits.TryGetValue(clientName, out var sprite)) return sprite;
            return null;
        }

        public string GetRandomClientName(bool female)
        {
            if (female)
            {
                if (_femaleClientNames == null || _femaleClientNames.Count ==0) return string.Empty;
                return _femaleClientNames[Random.Range(0, _femaleClientNames.Count)];
            }
            else
            {
                if (_maleClientNames == null || _maleClientNames.Count ==0) return string.Empty;
                return _maleClientNames[Random.Range(0, _maleClientNames.Count)];
            }
        }

        // Destination accessors
        public IReadOnlyList<string> GetAllDestinations()
        {
            if (_destinationsDistances == null) return new List<string>();
            return _destinationsDistances.Keys.ToList();
        }

        public bool TryGetDestinationDistance(string destination, out int distance)
        {
            distance =0;
            if (_destinationsDistances == null) return false;
            return _destinationsDistances.TryGetValue(destination, out distance);
        }
    }
}
