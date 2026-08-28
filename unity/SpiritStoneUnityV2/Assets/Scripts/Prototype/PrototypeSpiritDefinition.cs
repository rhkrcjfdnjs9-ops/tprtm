using System.Collections.Generic;
using UnityEngine;

namespace SpiritStone.Prototype
{
    [CreateAssetMenu(fileName = "SpiritDefinition", menuName = "Spirit Stone/Spirit Definition")]
    public sealed class PrototypeSpiritDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private PrototypeSpiritRarity rarity;
        [SerializeField] private SpiritElement element;
        [SerializeField] private SpiritCombatRole combatRole;
        [SerializeField, Min(1f)] private float baseAttack = 1f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1f;
        [SerializeField] private PrototypeAbilityDefinition basicAttack = new();
        [SerializeField] private PrototypeAbilityDefinition skillOne = new();
        [SerializeField] private PrototypeAbilityDefinition skillTwo = new();
        [SerializeField] private PrototypeAbilityDefinition ultimate = new();
        [SerializeField, Min(1f)] private float ultimateEnergyMaximum = 100f;
        [SerializeField] private List<PrototypeEvolutionDefinition> evolutionMilestones = new();

        public string Id => id;

        public PrototypeSpiritData CreateRuntimeData()
        {
            PrototypeSpiritEvolutionMilestone[] milestones = new PrototypeSpiritEvolutionMilestone[evolutionMilestones.Count];
            for (int index = 0; index < milestones.Length; index++) milestones[index] = evolutionMilestones[index].CreateRuntimeData();
            return new PrototypeSpiritData(id, displayName, rarity, element, combatRole, baseAttack, attackInterval,
                basicAttack.CreateRuntimeData(), skillOne.CreateRuntimeData(), skillTwo.CreateRuntimeData(), ultimate.CreateRuntimeData(),
                ultimateEnergyMaximum, milestones);
        }

        public void Configure(PrototypeSpiritData source)
        {
            id = source.Id;
            displayName = source.DisplayName;
            rarity = source.Rarity;
            element = source.Element;
            combatRole = source.CombatRole;
            baseAttack = source.BaseAttack;
            attackInterval = source.AttackInterval;
            basicAttack.Configure(source.BasicAttack);
            skillOne.Configure(source.SkillOne);
            skillTwo.Configure(source.SkillTwo);
            ultimate.Configure(source.Ultimate);
            ultimateEnergyMaximum = source.UltimateEnergyMaximum;
            evolutionMilestones.Clear();
            for (int index = 0; index < source.EvolutionMilestones.Count; index++)
            {
                PrototypeEvolutionDefinition definition = new();
                definition.Configure(source.EvolutionMilestones[index]);
                evolutionMilestones.Add(definition);
            }
        }
    }
}
