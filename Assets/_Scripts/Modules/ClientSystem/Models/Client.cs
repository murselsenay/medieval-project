using Modules.JobSystem.Models;
using Modules.ClientSystem.Enums;

namespace Modules.ClientSystem.Models
{
    public class Client
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public EClientProfession Profession { get; set; }
        public EClientGender Gender { get; set; }

        // The trip job this client currently offers (can be null)
        public Job TripJob { get; set; }

        // If the client was rejected recently, they won't offer jobs until this unix timestamp
        public long CooldownUntilUnix { get; set; }

        public Client()
        {
            Id = System.Guid.NewGuid().ToString();
            Name = string.Empty;
            Age = 0;
            Profession = EClientProfession.Tourist;
            Gender = EClientGender.Unknown;
            TripJob = null;
            CooldownUntilUnix = 0;
        }

        public bool CanGenerateJob(long currentUnix)
        {
            return TripJob == null && currentUnix >= CooldownUntilUnix;
        }
    }
}