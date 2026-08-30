using System;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class PrototypeGrowthBattleValidation
    {
        [MenuItem("Tools/Prototype/Validate Growth And Battle")]
        public static void Run()
        {
            PrototypeSaveData save = new() { EnhancementStones = 100, SpiritUpgradeStones = 20 };
            PrototypeSpiritTrainingSystem training = new();
            training.Initialize(save);
            int firstCost = training.GetCost(PrototypeSpiritTrainingStat.Attack);
            Require(training.TryUpgrade(PrototypeSpiritTrainingStat.Attack), "Attack training must succeed with enough stones.");
            Require(training.AttackLevel == 1 && training.EnhancementStones == 100 - firstCost,
                "Attack training must increase one level and spend the exact cost.");
            int stonesBeforeInvalidUpgrade = training.EnhancementStones;
            Require(!training.TryUpgrade((PrototypeSpiritTrainingStat)999), "An unknown training stat must be rejected.");
            Require(training.EnhancementStones == stonesBeforeInvalidUpgrade,
                "An invalid training request must not consume enhancement stones.");

            PrototypeSpiritSpecialGrowthSystem specialGrowth = new();
            specialGrowth.Initialize(save);
            for (int level = 0; level < PrototypeSpiritSpecialGrowthSystem.MaximumLevel; level++)
                Require(specialGrowth.Upgrade("arca", PrototypeSpiritSpecialGrowthType.SkillPower),
                    "Skill power must advance until its maximum level.");
            Require(!specialGrowth.Upgrade("arca", PrototypeSpiritSpecialGrowthType.SkillPower),
                "Skill power must reject upgrades after maximum level.");
            Require(Mathf.Approximately(specialGrowth.GetSkillPowerMultiplier("arca"), 1.8f),
                "Maximum skill growth must produce the configured 80 percent bonus.");

            int levelValue = 1;
            int experience = PrototypeGrowthCalculator.GetRequiredExperience(1) + PrototypeGrowthCalculator.GetRequiredExperience(2);
            Require(PrototypeGrowthCalculator.ApplyLevelUps(ref levelValue, ref experience) == 2,
                "Accumulated experience must support multiple level-ups.");
            Require(levelValue == 3 && experience == 0, "Level-up calculation must preserve the correct remainder.");

            Require(Mathf.Approximately(PrototypeBattleSystem.ApplyElement(100f, SpiritElement.Water, SpiritElement.Fire), 125f),
                "Water must deal increased damage to Fire.");
            Require(Mathf.Approximately(PrototypeBattleSystem.ApplyElement(100f, SpiritElement.Fire, SpiritElement.Water), 80f),
                "Fire must deal reduced damage to Water.");
            Require(PrototypeBattleSystem.CalculateProtagonistDamage(8f, 2, 1) > PrototypeBattleSystem.CalculateProtagonistDamage(8f, 1, 1),
                "Protagonist damage must increase with level.");

            PrototypeSpiritAbilityData attackSkillTwo = new("test_attack", "Test Attack", 3f, 2f, 0f, 10f,
                SpiritAbilityEffect.Attack, 2);
            PrototypeAbilityExecution attackExecution = PrototypeSpiritAbilitySystem.Resolve(
                attackSkillTwo, SpiritAbilitySlot.SkillTwo, 10f, 1.5f);
            Require(Mathf.Approximately(attackExecution.Damage, 30f) && attackExecution.DealsDamage,
                "An attack effect must deal damage regardless of its ability slot.");
            PrototypeAbilityExecution shieldExecution = PrototypeSpiritAbilitySystem.Resolve(
                new PrototypeSpiritAbilityData("test_shield", "Test Shield", 3f, 2f, 4f, 0f, SpiritAbilityEffect.Shield),
                SpiritAbilitySlot.SkillOne, 10f, 1.5f);
            Require(Mathf.Approximately(shieldExecution.Shield, 30f) && shieldExecution.GrantsShield,
                "Shield strength must include the common skill multiplier.");
            PrototypeAbilityExecution healExecution = PrototypeSpiritAbilitySystem.Resolve(
                new PrototypeSpiritAbilityData("test_heal", "Test Heal", 3f, 0.2f, 0f, 0f, SpiritAbilityEffect.HealAll),
                SpiritAbilitySlot.Ultimate, 10f, 1.5f);
            Require(Mathf.Approximately(healExecution.HealingRatio, 0.3f) && healExecution.HealsParty,
                "Party healing must include the common skill multiplier.");

            Debug.LogFormat("[PrototypeGrowthBattleValidation] PASS: growth costs, caps, experience, and battle calculations are valid.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"[PrototypeGrowthBattleValidation] {message}");
        }
    }
}
