using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeSpiritData
    {
        private readonly PrototypeSpiritEvolutionMilestone[] evolutionMilestones;

        public PrototypeSpiritData(string id, string displayName, PrototypeSpiritRarity rarity, SpiritElement element, SpiritCombatRole combatRole,
            float baseAttack, float attackInterval, PrototypeSpiritAbilityData basicAttack,
            PrototypeSpiritAbilityData skillOne, PrototypeSpiritAbilityData skillTwo,
            PrototypeSpiritAbilityData ultimate, float ultimateEnergyMaximum,
            params PrototypeSpiritEvolutionMilestone[] evolutionMilestones)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Spirit id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            Id = id;
            DisplayName = displayName;
            Rarity = rarity;
            Element = element;
            CombatRole = combatRole;
            BaseAttack = Mathf.Max(1f, baseAttack);
            AttackInterval = Mathf.Max(0.1f, attackInterval);
            BasicAttack = basicAttack ?? throw new ArgumentNullException(nameof(basicAttack));
            SkillOne = skillOne ?? throw new ArgumentNullException(nameof(skillOne));
            SkillTwo = skillTwo ?? throw new ArgumentNullException(nameof(skillTwo));
            Ultimate = ultimate ?? throw new ArgumentNullException(nameof(ultimate));
            UltimateEnergyMaximum = Mathf.Max(1f, ultimateEnergyMaximum);
            this.evolutionMilestones = evolutionMilestones ?? throw new ArgumentNullException(nameof(evolutionMilestones));
            if (this.evolutionMilestones.Length == 0) throw new ArgumentException("At least one evolution milestone is required.", nameof(evolutionMilestones));
            Array.Sort(this.evolutionMilestones, (left, right) => left.RequiredLevel.CompareTo(right.RequiredLevel));
        }

        public string Id { get; }
        public string DisplayName { get; }
        public PrototypeSpiritRarity Rarity { get; }
        public SpiritElement Element { get; }
        public SpiritCombatRole CombatRole { get; }
        public float BaseAttack { get; }
        public float AttackInterval { get; }
        public PrototypeSpiritAbilityData BasicAttack { get; }
        public PrototypeSpiritAbilityData SkillOne { get; }
        public PrototypeSpiritAbilityData SkillTwo { get; }
        public PrototypeSpiritAbilityData Ultimate { get; }
        public float UltimateEnergyMaximum { get; }
        public IReadOnlyList<PrototypeSpiritEvolutionMilestone> EvolutionMilestones => evolutionMilestones;

        public PrototypeSpiritEvolutionData GetEvolutionForLevel(int level)
        {
            PrototypeSpiritEvolutionData result = evolutionMilestones[0].Data;
            for (int index = 1; index < evolutionMilestones.Length; index++)
            {
                if (level < evolutionMilestones[index].RequiredLevel) break;
                result = evolutionMilestones[index].Data;
            }
            return result;
        }
    }
}
