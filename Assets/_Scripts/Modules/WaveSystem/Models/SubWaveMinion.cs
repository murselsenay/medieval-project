using Components.Minions.Models;

namespace Modules.WaveSystem.Models
{
    public struct SubWaveMinion
    {
        public string Key;
        public MinionData Data;
        public int Count;

        public SubWaveMinion(string key, MinionData data, int count)
        {
            Key = key ?? string.Empty;
            Data = data;
            Count = count;
        }
    }
}