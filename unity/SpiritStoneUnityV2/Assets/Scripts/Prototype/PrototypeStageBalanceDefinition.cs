using UnityEngine;

namespace SpiritStone.Prototype
{
    [CreateAssetMenu(fileName = "StageBalance", menuName = "Spirit Stone/Stage Balance")]
    public sealed class PrototypeStageBalanceDefinition : ScriptableObject
    {
        [SerializeField] private float baseHealth = 45f;
        [SerializeField, Range(0f, 0.25f)] private float healthGrowthPerStage = 0.023f;
        [SerializeField, Range(0f, 0.25f)] private float healthGrowthPerWave = 0.03f;
        [SerializeField] private float baseDamage = 5f;
        [SerializeField, Range(0f, 0.25f)] private float damageGrowthPerStage = 0.012f;
        [SerializeField, Range(0f, 0.25f)] private float damageGrowthPerWave = 0.035f;
        [SerializeField] private int baseGold = 8;
        [SerializeField, Range(0f, 0.1f)] private float goldGrowthPerStage = 0.025f;
        [SerializeField, Range(0f, 0.25f)] private float goldGrowthPerWave = 0.05f;
        [SerializeField] private int baseExperience = 7;
        [SerializeField, Range(0f, 0.1f)] private float experienceGrowthPerStage = 0.02f;
        [SerializeField, Range(0f, 0.25f)] private float experienceGrowthPerWave = 0.04f;
        [SerializeField, Min(0)] private int clearGoldBase = 40;
        [SerializeField, Min(0)] private int clearGoldPerStage = 15;
        [SerializeField, Min(0)] private int clearExperienceBase = 25;
        [SerializeField, Min(0)] private int clearExperiencePerStage = 5;

        public float GetHealth(int stage, int wave) => GetScaledFloat(baseHealth, healthGrowthPerStage, stage - 1, healthGrowthPerWave, wave - 1, 1e30f);
        public float GetDamage(int stage, int wave) => GetScaledFloat(baseDamage, damageGrowthPerStage, stage - 1, damageGrowthPerWave, wave - 1, 1e20f);
        public int GetGold(int stage, int wave) => GetScaledInt(baseGold, goldGrowthPerStage, stage - 1, goldGrowthPerWave, wave - 1);
        public int GetExperience(int stage, int wave) => GetScaledInt(baseExperience, experienceGrowthPerStage, stage - 1, experienceGrowthPerWave, wave - 1);
        public int GetClearGold(int stage) => GetSafeLinearReward(clearGoldBase, clearGoldPerStage, stage);
        public int GetClearExperience(int stage) => GetSafeLinearReward(clearExperienceBase, clearExperiencePerStage, stage);

        private static float GetScaledFloat(float baseValue, float stageGrowth, int stageExponent, float waveGrowth, int waveExponent, float maximum)
        {
            double value = baseValue * System.Math.Pow(1d + stageGrowth, Mathf.Max(0, stageExponent))
                * System.Math.Pow(1d + waveGrowth, Mathf.Max(0, waveExponent));
            return (float)System.Math.Min(maximum, value);
        }

        private static int GetScaledInt(int baseValue, float stageGrowth, int stageExponent, float waveGrowth, int waveExponent)
        {
            double value = baseValue * System.Math.Pow(1d + stageGrowth, Mathf.Max(0, stageExponent))
                * System.Math.Pow(1d + waveGrowth, Mathf.Max(0, waveExponent));
            return (int)System.Math.Min(1_000_000_000d, System.Math.Round(value));
        }

        private static int GetSafeLinearReward(int baseValue, int perStage, int stage)
        {
            long value = (long)baseValue + (long)Mathf.Max(1, stage) * perStage;
            return (int)System.Math.Min(1_000_000_000L, value);
        }
    }
}
