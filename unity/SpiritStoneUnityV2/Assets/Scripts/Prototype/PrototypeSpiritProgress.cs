using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeSpiritProgress
    {
        public PrototypeSpiritProgress(string spiritId, int level, int experience)
        {
            SpiritId = spiritId;
            Level = Mathf.Max(1, level);
            Experience = Mathf.Max(0, experience);
            Normalize();
        }

        public string SpiritId { get; }
        public int Level { get; private set; }
        public int Experience { get; private set; }

        public int AddExperience(int amount)
        {
            Experience += Mathf.Max(0, amount);
            return Normalize();
        }

        public PrototypeSpiritProgressData CreateSaveData()
        {
            return new PrototypeSpiritProgressData { SpiritId = SpiritId, Level = Level, Experience = Experience };
        }

        private int Normalize()
        {
            int level = Level;
            int experience = Experience;
            int levelsGained = PrototypeGrowthCalculator.ApplyLevelUps(ref level, ref experience);
            Level = level;
            Experience = experience;
            return levelsGained;
        }
    }
}
