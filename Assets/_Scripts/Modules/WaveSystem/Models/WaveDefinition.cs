using System.Collections.Generic;

namespace Modules.WaveSystem.Models
{
    public class WaveDefinition
    {
        public string Name;
        public List<SubWave> SubWaves = new List<SubWave>();
        public int PrepareTimeMs;

        public int TotalCount
        {
            get
            {
                int s = 0;
                for (int i = 0; i < SubWaves.Count; i++)
                {
                    var sub = SubWaves[i];
                    if (sub.Minions == null) continue;
                    for (int j = 0; j < sub.Minions.Count; j++) s += sub.Minions[j].Count;
                }
                return s;
            }
        }

        public WaveDefinition(string name = null, int prepareTimeMs = 0)
        {
            Name = name ?? string.Empty;
            PrepareTimeMs = prepareTimeMs;
            SubWaves = new List<SubWave>();
        }
    }
}
