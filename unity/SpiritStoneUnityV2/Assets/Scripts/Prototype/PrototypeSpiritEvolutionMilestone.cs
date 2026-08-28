using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeSpiritEvolutionMilestone
    {
        public PrototypeSpiritEvolutionMilestone(int requiredLevel, SpiritEvolutionStage stage, string displayName, float attackMultiplier, Color displayColor)
        {
            RequiredLevel = Mathf.Max(1, requiredLevel);
            Data = new PrototypeSpiritEvolutionData(stage, displayName, string.Empty, attackMultiplier, displayColor);
        }

        public int RequiredLevel { get; }
        public PrototypeSpiritEvolutionData Data { get; }
    }
}
