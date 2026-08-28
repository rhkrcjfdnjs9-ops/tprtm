using System;
using UnityEngine;

namespace SpiritStone.Prototype
{
    [Serializable]
    public sealed class PrototypeAbilityDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, Min(0f)] private float cooldown;
        [SerializeField, Min(0f)] private float powerMultiplier = 1f;
        [SerializeField, Min(0f)] private float duration;
        [SerializeField, Min(0f)] private float energyGain;
        [SerializeField] private SpiritAbilityEffect effect;
        [SerializeField, Min(1)] private int maximumTargets = 1;

        public PrototypeSpiritAbilityData CreateRuntimeData()
        {
            return new PrototypeSpiritAbilityData(id, displayName, cooldown, powerMultiplier, duration, energyGain, effect, maximumTargets);
        }

        public void Configure(PrototypeSpiritAbilityData source)
        {
            id = source.Id;
            displayName = source.DisplayName;
            cooldown = source.Cooldown;
            powerMultiplier = source.PowerMultiplier;
            duration = source.Duration;
            energyGain = source.EnergyGain;
            effect = source.Effect;
            maximumTargets = source.MaximumTargets;
        }
    }
}
