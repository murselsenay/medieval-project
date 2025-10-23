using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Components.Minions.Controllers;
using Modules.EventSystem.Managers;
using Components.Minions.Models;
using Components.Minions.Enums;
using Modules.RewardSystem.Models;
using Modules.WaveSystem.Models;
using Components.Constants;

namespace Modules.WaveSystem.Managers
{
    public static class WaveManager
    {
        private static WaveController _controller;
        private static List<WaveDefinition> _waves;
        private static int _currentWaveIndex = -1;
        private static UniTaskCompletionSource<bool> _startTcs;
        private static bool _running;

        public static void Initialize(WaveController controller)
        {
            _controller = controller;
            _waves = CreateDefaultWaves();
            _currentWaveIndex = -1;
            _running = false;
        }

        public static List<WaveDefinition> CreateDefaultWaves()
        {
            var list = new List<WaveDefinition>();

            // Temel Minyon Tanýmlarý
            var baseMinions = new Dictionary<string, MinionData>
    {
        { MinionKeys.Barbarian, new MinionData(EMinionType.Barbarian, 60f, 1, 10, new GoldReward(3)) },
        { MinionKeys.Archer, new MinionData(EMinionType.Archer, 40f, 1, 8, new GoldReward(3)) },
        { MinionKeys.Knight, new MinionData(EMinionType.Knight, 90f, 1, 10, new GoldReward(5)) },
        { MinionKeys.Mage, new MinionData(EMinionType.Mage, 35f, 1, 12, new GoldReward(4)) }
    };

            const int totalWaves = 30;

            for (int i = 1; i <= totalWaves; i++)
            {
                // 1. Minyon Ýstatistiklerini Güncelleme:
                int levelIncrease = (i - 1) / 5;
                float healthMultiplier = 1f + (levelIncrease * 0.2f); // Her 5 dalgada %20 HP artýþý
                int minionLevel = 1 + levelIncrease;

                // 2. Zorluk Eðrisi (Minyon Sayýsý ve Alt Dalga Beklemesi):

                int totalMinionsForWave = 4 + (i - 1) / 2;

                // ORÝJÝNAL HESAPLAMA: Dalga ilerledikçe azalýr (min. 300ms)
                int originalWaitMs = Math.Max(300, 1500 - (i * 50));

                // YENÝ BEKLEME SÜRESÝ: Orijinal süreyi 3 katýna çýkaralým.
                // Maksimum süreyi 4500ms (4.5 saniye) ile sýnýrlayalým.
                int subWaveWaitMs = Math.Min(4500, originalWaitMs * 3);


                // 3. Minyon Tanýtým Aþamalarý:
                List<string> activeMinionKeys = new List<string> { MinionKeys.Barbarian };
                if (i >= 6) activeMinionKeys.Add(MinionKeys.Archer);
                if (i >= 11) activeMinionKeys.Add(MinionKeys.Knight);
                if (i >= 16) activeMinionKeys.Add(MinionKeys.Mage);

                // 4. Dalga Tanýmýný Oluþturma:
                var wave = new WaveDefinition($"Wave {i}", prepareTimeMs: 0); // Kullanýcý baþlatacak

                // Minyon verilerini güncellenmiþ HP ve Level ile alalým
                MinionData getUpdatedData(string key)
                {
                    var baseData = baseMinions[key];
                    return new MinionData(
                        baseData.MinionType,
                        (float)Math.Round(baseData.Health * healthMultiplier),
                        minionLevel,
                        baseData.Damage + levelIncrease,
                        baseData.Reward
                    );
                }

                // --- Alt Dalgalarý Oluþturma Mantýðý ---

                int subWaveCount = 2; // Baþlangýç 2 alt dalga
                if (i >= 6) subWaveCount = 3;
                if (i >= 16) subWaveCount = 4;

                int minionCountPerSubWave = totalMinionsForWave / subWaveCount;
                int remainingMinions = totalMinionsForWave % subWaveCount;

                for (int si = 0; si < subWaveCount; si++)
                {
                    int currentSubWaveTotalCount = minionCountPerSubWave + (si < remainingMinions ? 1 : 0);
                    if (currentSubWaveTotalCount == 0) continue;

                    var subWaveMinions = new List<SubWaveMinion>();
                    int minionTypeCount = activeMinionKeys.Count;
                    int countPerType = currentSubWaveTotalCount / minionTypeCount;
                    int remainingInSubWave = currentSubWaveTotalCount % minionTypeCount;

                    for (int ki = 0; ki < minionTypeCount; ki++)
                    {
                        string key = activeMinionKeys[ki];
                        int count = countPerType + (ki < remainingInSubWave ? 1 : 0);
                        if (count > 0)
                        {
                            subWaveMinions.Add(new SubWaveMinion(key, getUpdatedData(key), count));
                        }
                    }

                    // Son alt dalga sonrasý bekleme 0 olsun
                    int waitMs = (si == subWaveCount - 1) ? 0 : subWaveWaitMs;

                    wave.SubWaves.Add(new SubWave(subWaveMinions, waitMs));
                }

                list.Add(wave);
            }

            return list;
        }

        public static async UniTask StartAllWavesAsync(bool autoStartFirst = false)
        {
            if (_controller == null || _waves == null || _waves.Count == 0) return;
            if (_running) return;

            _running = true;

            for (int i = 0; i < _waves.Count; i++)
            {
                _currentWaveIndex = i;
                var wave = _waves[i];

                EventManager.DelegatePreparePhaseStarted(i, wave);

                if (wave.PrepareTimeMs > 0 || (autoStartFirst && i == 0))
                {
                    int waitMs = wave.PrepareTimeMs;
                    if (autoStartFirst && i == 0 && waitMs == 0)
                        waitMs = 0;

                    if (waitMs > 0)
                        await UniTask.Delay(waitMs);
                }
                else
                {
                    _startTcs = new UniTaskCompletionSource<bool>();
                    await _startTcs.Task;
                }

                EventManager.DelegatePreparePhaseEnded(i, wave);

                EventManager.DelegateWaveStarted(i, wave);
                await RunWaveAsync(wave);
                EventManager.DelegateWaveCompleted(i, wave);

                await UniTask.Delay(500);
            }

            _running = false;
            EventManager.DelegateAllWavesCompleted();
        }

        private static async UniTask RunWaveAsync(WaveDefinition wave)
        {
            if (_controller == null || wave == null) return;

            // Track active minions spawned during this wave so we can wait until all die
            var activeMinions = new HashSet<MinionController>();

            // Handlers to modify the set
            EventManager.MinionEvent onSpawn = (MinionController m) =>
            {
                if (m != null)
                    activeMinions.Add(m);
            };

            EventManager.MinionEvent onDied = (MinionController m) =>
            {
                if (m != null)
                    activeMinions.Remove(m);
            };

            EventManager.OnMinionSpawned += onSpawn;
            EventManager.OnMinionDied += onDied;

            try
            {
                for (int si = 0; si < wave.SubWaves.Count; si++)
                {
                    var sub = wave.SubWaves[si];
                    if (sub.Minions == null) continue;

                    // total minions in this subwave
                    int total = 0;
                    for (int mi = 0; mi < sub.Minions.Count; mi++) total += sub.Minions[mi].Count;
                    if (total <= 0) continue;

                    // pass full subwave to controller so it can spawn based on provided MinionData
                    await _controller.SpawnWaveAsync(sub);

                    if (sub.WaitMsAfter > 0)
                        await UniTask.Delay(sub.WaitMsAfter);
                }

                // Wait until all minions spawned in this wave have died
                while (activeMinions.Count > 0)
                {
                    await UniTask.Yield();
                }
            }
            finally
            {
                EventManager.OnMinionSpawned -= onSpawn;
                EventManager.OnMinionDied -= onDied;
            }
        }
        public static void RequestStartWave()
        {
            if (_startTcs != null)
            {
                _startTcs.TrySetResult(true);
                _startTcs = null;
            }
        }

        public static int CurrentWaveIndex => _currentWaveIndex;
        public static bool IsRunning => _running;
    }
}