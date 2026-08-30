using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public static class PrototypeSpiritCatalog
    {
        private static readonly Dictionary<string, PrototypeSpiritData> FallbackSpirits = new(StringComparer.OrdinalIgnoreCase)
        {
            ["arca"] = new PrototypeSpiritData(
                "arca", "아르카", PrototypeSpiritRarity.SSR, SpiritElement.Lightning, SpiritCombatRole.RangedAttack, 18f, 0.8f,
                new PrototypeSpiritAbilityData("rotating_core", "회전 코어", 0f, 1f, 0f, 14f),
                new PrototypeSpiritAbilityData("chain_lightning", "연쇄 번개", 4.5f, 2.25f, 0f, 24f, SpiritAbilityEffect.Attack, 3),
                new PrototypeSpiritAbilityData("overcharge", "과충전", 9f, 0.55f, 3f, 12f, SpiritAbilityEffect.AttackSpeedBuff),
                new PrototypeSpiritAbilityData("lightning_judgment", "뇌광 심판", 0f, 5f, 0f, 0f), 100f,
                new PrototypeSpiritEvolutionMilestone(1, SpiritEvolutionStage.SpiritStoneOne, "정령돌 1단계", 1f, new Color(0.32f, 0.28f, 0.42f)),
                new PrototypeSpiritEvolutionMilestone(10, SpiritEvolutionStage.SpiritStoneTwo, "정령돌 2단계", 1.15f, new Color(0.48f, 0.3f, 0.7f)),
                new PrototypeSpiritEvolutionMilestone(20, SpiritEvolutionStage.SpiritStoneThree, "정령돌 3단계", 1.35f, new Color(0.65f, 0.32f, 0.9f)),
                new PrototypeSpiritEvolutionMilestone(30, SpiritEvolutionStage.Liberated, "정령 해방", 1.7f, new Color(0.72f, 0.3f, 1f)),
                new PrototypeSpiritEvolutionMilestone(50, SpiritEvolutionStage.Awakened, "정령 각성", 2.3f, new Color(0.92f, 0.72f, 1f))),
            ["ignis"] = new PrototypeSpiritData(
                "ignis", "호노카", PrototypeSpiritRarity.SSR, SpiritElement.Fire, SpiritCombatRole.MeleeAttack, 22f, 1.05f,
                new PrototypeSpiritAbilityData("flame_strike", "화염 타격", 0f, 1f, 0f, 12f),
                new PrototypeSpiritAbilityData("blazing_charge", "작열 돌진", 5.2f, 2.6f, 0f, 22f),
                new PrototypeSpiritAbilityData("burning_will", "타오르는 의지", 10f, 1.45f, 4f, 14f, SpiritAbilityEffect.AttackPowerBuff),
                new PrototypeSpiritAbilityData("inferno_burst", "업화 폭발", 0f, 5.4f, 0f, 0f), 100f,
                new PrototypeSpiritEvolutionMilestone(1, SpiritEvolutionStage.SpiritStoneOne, "정령돌 1단계", 1f, new Color(0.48f, 0.2f, 0.12f)),
                new PrototypeSpiritEvolutionMilestone(10, SpiritEvolutionStage.SpiritStoneTwo, "정령돌 2단계", 1.15f, new Color(0.68f, 0.22f, 0.1f)),
                new PrototypeSpiritEvolutionMilestone(20, SpiritEvolutionStage.SpiritStoneThree, "정령돌 3단계", 1.35f, new Color(0.88f, 0.28f, 0.08f)),
                new PrototypeSpiritEvolutionMilestone(30, SpiritEvolutionStage.Liberated, "정령 해방", 1.7f, new Color(1f, 0.38f, 0.1f)),
                new PrototypeSpiritEvolutionMilestone(50, SpiritEvolutionStage.Awakened, "정령 각성", 2.3f, new Color(1f, 0.68f, 0.18f))),
            ["elysia"] = new PrototypeSpiritData(
                "elysia", "엘리시아", PrototypeSpiritRarity.SSR, SpiritElement.Water, SpiritCombatRole.Defense, 14f, 1.1f,
                new PrototypeSpiritAbilityData("water_impact", "물방울 충격", 0f, 1f, 0f, 12f),
                new PrototypeSpiritAbilityData("weakening_wave", "약화의 파동", 6f, 0.75f, 4f, 20f, SpiritAbilityEffect.EnemyAttackReduction),
                new PrototypeSpiritAbilityData("aqua_barrier", "아쿠아 배리어", 11f, 2.2f, 5f, 15f, SpiritAbilityEffect.Shield),
                new PrototypeSpiritAbilityData("deep_sea_sanctuary", "심해의 성역", 0f, 0.45f, 5f, 0f, SpiritAbilityEffect.DamageReduction), 100f,
                new PrototypeSpiritEvolutionMilestone(1, SpiritEvolutionStage.SpiritStoneOne, "정령돌 1단계", 1f, new Color(0.16f, 0.4f, 0.58f)),
                new PrototypeSpiritEvolutionMilestone(10, SpiritEvolutionStage.SpiritStoneTwo, "정령돌 2단계", 1.15f, new Color(0.12f, 0.55f, 0.72f)),
                new PrototypeSpiritEvolutionMilestone(20, SpiritEvolutionStage.SpiritStoneThree, "정령돌 3단계", 1.35f, new Color(0.1f, 0.7f, 0.85f)),
                new PrototypeSpiritEvolutionMilestone(30, SpiritEvolutionStage.Liberated, "정령 해방", 1.7f, new Color(0.22f, 0.82f, 1f)),
                new PrototypeSpiritEvolutionMilestone(50, SpiritEvolutionStage.Awakened, "정령 각성", 2.3f, new Color(0.62f, 0.94f, 1f))),
            ["windy"] = new PrototypeSpiritData(
                "windy", "윈디", PrototypeSpiritRarity.SSR, SpiritElement.Wind, SpiritCombatRole.Support, 12f, 1.15f,
                new PrototypeSpiritAbilityData("breeze_orb", "산들바람", 0f, 1f, 0f, 14f),
                new PrototypeSpiritAbilityData("wind_whisper", "바람의 속삭임", 7f, 0.2f, 0f, 20f, SpiritAbilityEffect.HealAll),
                new PrototypeSpiritAbilityData("tailwind_blessing", "순풍의 가호", 11f, 0.78f, 5f, 16f, SpiritAbilityEffect.AttackSpeedBuff),
                new PrototypeSpiritAbilityData("storm_sanctuary", "폭풍의 성역", 0f, 1.35f, 6f, 0f, SpiritAbilityEffect.TeamAttackPowerBuff), 100f,
                new PrototypeSpiritEvolutionMilestone(1, SpiritEvolutionStage.SpiritStoneOne, "정령돌 1단계", 1f, new Color(0.2f, 0.48f, 0.32f)),
                new PrototypeSpiritEvolutionMilestone(10, SpiritEvolutionStage.SpiritStoneTwo, "정령돌 2단계", 1.15f, new Color(0.28f, 0.68f, 0.42f)),
                new PrototypeSpiritEvolutionMilestone(20, SpiritEvolutionStage.SpiritStoneThree, "정령돌 3단계", 1.35f, new Color(0.35f, 0.85f, 0.55f)),
                new PrototypeSpiritEvolutionMilestone(30, SpiritEvolutionStage.Liberated, "정령 해방", 1.7f, new Color(0.55f, 1f, 0.72f)),
                new PrototypeSpiritEvolutionMilestone(50, SpiritEvolutionStage.Awakened, "정령 각성", 2.3f, new Color(0.82f, 1f, 0.9f)))
        };
        private static Dictionary<string, PrototypeSpiritData> loadedSpirits;

        private static Dictionary<string, PrototypeSpiritData> Spirits => loadedSpirits ??= LoadSpirits();

        private static Dictionary<string, PrototypeSpiritData> LoadSpirits()
        {
            PrototypeSpiritDefinition[] definitions = Resources.LoadAll<PrototypeSpiritDefinition>("Prototype/Spirits");
            if (definitions.Length == 0)
                throw new InvalidOperationException("No spirit definition assets were found in Resources/Prototype/Spirits.");
            Dictionary<string, PrototypeSpiritData> result = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < definitions.Length; index++)
            {
                PrototypeSpiritDefinition definition = definitions[index];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                    throw new InvalidOperationException("Spirit definition contains an empty id.");
                if (!result.TryAdd(definition.Id, definition.CreateRuntimeData()))
                    throw new InvalidOperationException($"Duplicate spirit definition id: {definition.Id}");
            }
            return result;
        }

        public static PrototypeSpiritData GetRequired(string spiritId)
        {
            if (string.IsNullOrWhiteSpace(spiritId) || !Spirits.TryGetValue(spiritId, out PrototypeSpiritData spirit))
                throw new ArgumentException($"Unknown spirit id: {spiritId}", nameof(spiritId));
            return spirit;
        }

        public static IEnumerable<PrototypeSpiritData> GetAll()
        {
            return Spirits.Values.OrderBy(spirit => spirit.Id, StringComparer.OrdinalIgnoreCase);
        }

        public static void Reload()
        {
            loadedSpirits = null;
            _ = Spirits.Count;
        }

        public static IEnumerable<PrototypeSpiritData> GetBuiltInDefaults()
        {
            return FallbackSpirits.Values;
        }
    }
}
