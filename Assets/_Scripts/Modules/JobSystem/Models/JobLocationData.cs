
namespace Modules.JobSystem.Models
{
    public class JobLocationData
    {
        public string LocationName { get; private set; }
        public float BaseDistance { get; private set; }

        public JobLocationData(string name, float baseDistance)
        {
            LocationName = name;
            BaseDistance = baseDistance;
        }
    }
}