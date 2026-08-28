using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeEnemyData
    {
        public PrototypeEnemyData(
            string displayName,
            float maximumHealth,
            float attackDamage,
            float attackInterval,
            int goldReward,
            int experienceReward,
            SpiritElement element,
            Color displayColor,
            bool isBoss)
        {
            DisplayName = displayName;
            MaximumHealth = Mathf.Max(1f, maximumHealth);
            AttackDamage = Mathf.Max(1f, attackDamage);
            AttackInterval = Mathf.Max(0.1f, attackInterval);
            GoldReward = Mathf.Max(0, goldReward);
            ExperienceReward = Mathf.Max(0, experienceReward);
            Element = element;
            DisplayColor = displayColor;
            IsBoss = isBoss;
        }

        public string DisplayName { get; }
        public float MaximumHealth { get; }
        public float AttackDamage { get; }
        public float AttackInterval { get; }
        public int GoldReward { get; }
        public int ExperienceReward { get; }
        public SpiritElement Element { get; }
        public Color DisplayColor { get; }
        public bool IsBoss { get; }
    }
}
