using System;
using System.Collections.Generic;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class PrototypeEnemyPatternValidation
    {
        [MenuItem("Tools/Prototype/Validate Enemy Patterns")]
        public static void Run()
        {
            PrototypeEnemyAttackPattern balanced = PrototypeEnemyAttackPatternSystem.GetBasicPattern(PrototypeEnemyArchetype.Balanced);
            PrototypeEnemyAttackPattern heavy = PrototypeEnemyAttackPatternSystem.GetBasicPattern(PrototypeEnemyArchetype.Heavy);
            PrototypeEnemyAttackPattern fast = PrototypeEnemyAttackPatternSystem.GetBasicPattern(PrototypeEnemyArchetype.Fast);
            Require(balanced.HitCount == 1 && Mathf.Approximately(balanced.DamageMultiplier, 1f),
                "Balanced enemies must perform one standard hit.");
            Require(heavy.HitCount == 1 && heavy.DamageMultiplier > balanced.DamageMultiplier,
                "Heavy enemies must perform a stronger single hit.");
            Require(fast.HitCount == 2 && fast.DamageMultiplier < balanced.DamageMultiplier,
                "Fast enemies must perform two reduced-damage hits.");

            HashSet<string> bossPatternNames = new(StringComparer.Ordinal);
            foreach (SpiritElement element in Enum.GetValues(typeof(SpiritElement)))
            {
                PrototypeEnemyAttackPattern pattern = PrototypeEnemyAttackPatternSystem.GetBossSpecialPattern(element);
                Require(pattern.TargetsAll && pattern.DamageMultiplier > 0f && !string.IsNullOrWhiteSpace(pattern.DisplayName),
                    $"{element} boss special must be a valid party-wide attack.");
                Require(bossPatternNames.Add(pattern.DisplayName), $"{element} boss special requires a unique display name.");
            }
            Require(Mathf.Approximately(PrototypeElementChart.GetDamageMultiplier(SpiritElement.Fire, SpiritElement.Wind), 1.25f),
                "Enemy elemental advantage must use the configured 1.25 multiplier.");
            Require(Mathf.Approximately(PrototypeElementChart.GetDamageMultiplier(SpiritElement.Wind, SpiritElement.Fire), 0.8f),
                "Enemy elemental disadvantage must use the configured 0.8 multiplier.");

            Debug.LogFormat("[PrototypeEnemyPatternValidation] PASS: archetype attacks and six elemental boss specials are valid.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"[PrototypeEnemyPatternValidation] {message}");
        }
    }
}
