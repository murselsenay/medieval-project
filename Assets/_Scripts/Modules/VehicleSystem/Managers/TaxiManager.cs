using System.Collections.Generic;
using Modules.TaxiSystem.Models;

namespace Modules.TaxiSystem.Managers
{
    public static class TaxiManager
    {
        public static List<Taxi> AllTaxis { get; } = new List<Taxi>();

        public static List<Taxi> OwnedTaxis { get; } = new List<Taxi>();

        public static void Init()
        {
            if (AllTaxis.Count >0)
                return;

            AllTaxis.Clear();
            OwnedTaxis.Clear();

            var mobile = new MobiletTaxi { Id = "mobilet-001", IsLocked = false };
            var classic = new ClassicTaxi { Id = "classic-001", IsLocked = true };
            var metropolitan = new MetropolitanTaxi { Id = "metropolitan-001", IsLocked = true };
            var vip = new VipTaxi { Id = "vip-001", IsLocked = true };
            var london = new LondonTaxi { Id = "london-001", IsLocked = true };

            AllTaxis.AddRange(new Taxi[] { mobile, classic, metropolitan, vip, london });

            OwnedTaxis.Add(mobile);
        }
    }
}