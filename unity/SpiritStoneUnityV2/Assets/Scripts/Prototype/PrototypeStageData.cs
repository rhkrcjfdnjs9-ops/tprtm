using System;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeStageData
    {
        private readonly PrototypeEnemyData[] encounters;

        public PrototypeStageData(int stageNumber, PrototypeEnemyData[] stageEncounters, int clearGoldReward,
            int clearExperienceReward, int firstClearSpiritStoneReward = 20, int repeatClearSpiritStoneReward = 5,
            float normalEnhancementStoneDropChance = 0.2f, float bossSpiritUpgradeStoneDropChance = 0.3f)
        {
            if (stageEncounters == null || stageEncounters.Length != 10)
                throw new ArgumentException("A stage requires exactly ten encounters.", nameof(stageEncounters));
            StageNumber = Math.Max(1, stageNumber);
            encounters = stageEncounters;
            ClearGoldReward = Math.Max(0, clearGoldReward);
            ClearExperienceReward = Math.Max(0, clearExperienceReward);
            FirstClearSpiritStoneReward = Math.Max(0, firstClearSpiritStoneReward);
            RepeatClearSpiritStoneReward = Math.Max(0, repeatClearSpiritStoneReward);
            NormalEnhancementStoneDropChance = UnityEngine.Mathf.Clamp01(normalEnhancementStoneDropChance);
            BossSpiritUpgradeStoneDropChance = UnityEngine.Mathf.Clamp01(bossSpiritUpgradeStoneDropChance);
        }

        public int StageNumber { get; }
        public int ClearGoldReward { get; }
        public int ClearExperienceReward { get; }
        public int FirstClearSpiritStoneReward { get; }
        public int RepeatClearSpiritStoneReward { get; }
        public float NormalEnhancementStoneDropChance { get; }
        public float BossSpiritUpgradeStoneDropChance { get; }

        public PrototypeEnemyData GetEncounter(int waveNumber)
        {
            if (waveNumber < 1 || waveNumber > encounters.Length)
                throw new ArgumentOutOfRangeException(nameof(waveNumber));
            return encounters[waveNumber - 1];
        }
    }
}
