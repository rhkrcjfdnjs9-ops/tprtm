using System;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeStageData
    {
        private readonly PrototypeEnemyData[] encounters;

        public PrototypeStageData(int stageNumber, PrototypeEnemyData[] stageEncounters, int clearGoldReward, int clearExperienceReward)
        {
            if (stageEncounters == null || stageEncounters.Length != 10)
                throw new ArgumentException("A stage requires exactly ten encounters.", nameof(stageEncounters));
            StageNumber = Math.Max(1, stageNumber);
            encounters = stageEncounters;
            ClearGoldReward = Math.Max(0, clearGoldReward);
            ClearExperienceReward = Math.Max(0, clearExperienceReward);
        }

        public int StageNumber { get; }
        public int ClearGoldReward { get; }
        public int ClearExperienceReward { get; }

        public PrototypeEnemyData GetEncounter(int waveNumber)
        {
            if (waveNumber < 1 || waveNumber > encounters.Length)
                throw new ArgumentOutOfRangeException(nameof(waveNumber));
            return encounters[waveNumber - 1];
        }
    }
}
