using System.Collections.Generic;
using System.Linq;
using Modules.DriverSystem.Models;

namespace Modules.DriverSystem.Managers
{
    public static class DriverManager
    {
        private static List<Driver> _drivers;
        public static IReadOnlyList<Driver> Drivers => _drivers?.AsReadOnly();

        public static void Init()
        {
            // Initialize a constant set of6 drivers:2 Safe,2 Bonus,2 Speedy
            _drivers = new List<Driver>
            {
                new SafeDriver("Safe Driver1"),
                new SafeDriver("Safe Driver2"),
                new BonusDriver("Bonus Driver1"),
                new BonusDriver("Bonus Driver2"),
                new SpeedyDriver("Speedy Driver1"),
                new SpeedyDriver("Speedy Driver2")
            };
        }

        public static IEnumerable<Driver> GetAvailableDrivers()
        {
            if (_drivers == null) yield break;
            foreach (var d in _drivers.Where(x => !x.IsOnJob)) yield return d;
        }

        public static Driver GetDriverByName(string name)
        {
            if (_drivers == null) return null;
            return _drivers.FirstOrDefault(d => d.DriverName == name);
        }

        public static Driver GetFreeDriver()
        {
            if (_drivers == null) return null;
            return _drivers.FirstOrDefault(d => !d.IsOnJob);
        }
    }
}