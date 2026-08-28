using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeSpiritAbilityData
    {
        public PrototypeSpiritAbilityData(string id, string displayName, float cooldown, float powerMultiplier, float duration, float energyGain, SpiritAbilityEffect effect = SpiritAbilityEffect.Attack, int maximumTargets = 1)
        {
            Id = id;
            DisplayName = displayName;
            Cooldown = Mathf.Max(0f, cooldown);
            PowerMultiplier = Mathf.Max(0f, powerMultiplier);
            Duration = Mathf.Max(0f, duration);
            EnergyGain = Mathf.Max(0f, energyGain);
            Effect = effect;
            MaximumTargets = Mathf.Max(1, maximumTargets);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public float Cooldown { get; }
        public float PowerMultiplier { get; }
        public float Duration { get; }
        public float EnergyGain { get; }
        public SpiritAbilityEffect Effect { get; }
        public int MaximumTargets { get; }
    }
}
