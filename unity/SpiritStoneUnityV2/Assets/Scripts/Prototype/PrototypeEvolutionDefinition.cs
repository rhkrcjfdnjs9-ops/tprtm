using System;
using UnityEngine;

namespace SpiritStone.Prototype
{
    [Serializable]
    public sealed class PrototypeEvolutionDefinition
    {
        [SerializeField, Min(1)] private int requiredLevel = 1;
        [SerializeField] private SpiritEvolutionStage stage;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, Min(0f)] private float attackMultiplier = 1f;
        [SerializeField] private Color displayColor = Color.white;

        public PrototypeSpiritEvolutionMilestone CreateRuntimeData()
        {
            return new PrototypeSpiritEvolutionMilestone(requiredLevel, stage, displayName, attackMultiplier, displayColor);
        }

        public void Configure(PrototypeSpiritEvolutionMilestone source)
        {
            requiredLevel = source.RequiredLevel;
            stage = source.Data.Stage;
            displayName = source.Data.DisplayName;
            attackMultiplier = source.Data.AttackMultiplier;
            displayColor = source.Data.DisplayColor;
        }
    }
}
