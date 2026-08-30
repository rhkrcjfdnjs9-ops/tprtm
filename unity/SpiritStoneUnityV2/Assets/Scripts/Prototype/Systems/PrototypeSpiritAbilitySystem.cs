using System;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public static class PrototypeSpiritAbilitySystem
    {
        public static PrototypeAbilityExecution Resolve(
            PrototypeSpiritAbilityData ability,
            SpiritAbilitySlot slot,
            float spiritAttackPower,
            float finalDamageMultiplier = 1f)
        {
            if (ability == null) throw new ArgumentNullException(nameof(ability));
            float safeAttackPower = Mathf.Max(0f, spiritAttackPower);
            float safeFinalMultiplier = Mathf.Max(0f, finalDamageMultiplier);
            bool dealsDamage = ability.Effect == SpiritAbilityEffect.Attack;
            float damage = dealsDamage ? safeAttackPower * ability.PowerMultiplier * safeFinalMultiplier : 0f;
            float shield = ability.Effect == SpiritAbilityEffect.Shield
                ? safeAttackPower * ability.PowerMultiplier * safeFinalMultiplier : 0f;
            float healingRatio = ability.Effect == SpiritAbilityEffect.HealAll
                ? ability.PowerMultiplier * safeFinalMultiplier : 0f;
            return new PrototypeAbilityExecution(ability, slot, damage, shield, healingRatio);
        }
    }
}
