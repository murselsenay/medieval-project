using Modules.WaveSystem.Managers;
using System.Collections.Generic;

namespace Modules.WaveSystem.Models
{
    public struct SubWave
    {
        public List<SubWaveMinion> Minions;
        public int WaitMsAfter;

        public SubWave(List<SubWaveMinion> minions, int waitMsAfter = 0)
        {
            Minions = minions ?? new List<SubWaveMinion>();
            WaitMsAfter = waitMsAfter;
        }
    }
}