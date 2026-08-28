using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeSpiritEvolutionData
    {
        public PrototypeSpiritEvolutionData(SpiritEvolutionStage stage, string displayName, string description, float attackMultiplier, Color displayColor)
        {
            Stage = stage;
            DisplayName = displayName;
            Description = description;
            AttackMultiplier = Mathf.Max(1f, attackMultiplier);
            DisplayColor = displayColor;
        }

        public SpiritEvolutionStage Stage { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public float AttackMultiplier { get; }
        public Color DisplayColor { get; }
    }
}
