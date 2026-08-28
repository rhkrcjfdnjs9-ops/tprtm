using UnityEngine;

namespace SpiritStone.Prototype
{
    public static class PrototypeGrowthCalculator
    {
        public static int GetRequiredExperience(int level)
        {
            return 45 + (Mathf.Max(1, level) - 1) * 25;
        }

        public static int ApplyLevelUps(ref int level, ref int experience)
        {
            level = Mathf.Max(1, level);
            experience = Mathf.Max(0, experience);
            int levelsGained = 0;
            while (experience >= GetRequiredExperience(level))
            {
                experience -= GetRequiredExperience(level);
                level++;
                levelsGained++;
            }
            return levelsGained;
        }
    }
}
