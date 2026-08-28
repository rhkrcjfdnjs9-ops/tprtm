using UnityEngine;

namespace SpiritStone.Prototype
{
    [CreateAssetMenu(fileName = "StageBalance", menuName = "Spirit Stone/Stage Balance")]
    public sealed class PrototypeStageBalanceDefinition : ScriptableObject
    {
        [SerializeField] private float baseHealth = 45f;
        [SerializeField] private float healthPerStage = 16f;
        [SerializeField] private float healthPerWave = 9f;
        [SerializeField] private float baseDamage = 5f;
        [SerializeField] private float damagePerStage = 1.5f;
        [SerializeField] private float damagePerWave = 0.4f;
        [SerializeField] private int baseGold = 8;
        [SerializeField] private int goldPerStage = 3;
        [SerializeField] private int goldPerWave = 2;
        [SerializeField] private int baseExperience = 7;
        [SerializeField] private int experiencePerStage = 2;
        [SerializeField] private int experiencePerWave = 1;

        public float GetHealth(int stage, int wave) => baseHealth + stage * healthPerStage + wave * healthPerWave;
        public float GetDamage(int stage, int wave) => baseDamage + stage * damagePerStage + wave * damagePerWave;
        public int GetGold(int stage, int wave) => baseGold + stage * goldPerStage + wave * goldPerWave;
        public int GetExperience(int stage, int wave) => baseExperience + stage * experiencePerStage + wave * experiencePerWave;
        public int GetClearGold(int stage) => 40 + stage * 15;
        public int GetClearExperience(int stage) => 25 + stage * 5;
    }
}
