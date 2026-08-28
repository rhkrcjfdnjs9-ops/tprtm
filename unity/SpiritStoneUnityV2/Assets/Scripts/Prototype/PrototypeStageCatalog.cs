using UnityEngine;

namespace SpiritStone.Prototype
{
    public static class PrototypeStageCatalog
    {
        private static PrototypeStageBalanceDefinition balance;
        private static PrototypeStageBalanceDefinition Balance => balance ??= Resources.Load<PrototypeStageBalanceDefinition>("Prototype/StageBalance");

        public static PrototypeStageData Create(int stageNumber)
        {
            int safeStage = Mathf.Max(1, stageNumber);
            PrototypeEnemyData[] encounters = new PrototypeEnemyData[10];
            for (int wave = 1; wave <= 9; wave++)
                encounters[wave - 1] = CreateNormalEnemy(safeStage, wave);
            encounters[9] = CreateBoss(safeStage);
            return new PrototypeStageData(
                safeStage,
                encounters,
                Balance != null ? Balance.GetClearGold(safeStage) : 40 + safeStage * 15,
                Balance != null ? Balance.GetClearExperience(safeStage) : 25 + safeStage * 5);
        }

        private static PrototypeEnemyData CreateNormalEnemy(int stage, int wave)
        {
            float baseHealth = Balance != null ? Balance.GetHealth(stage, wave) : 45f + stage * 16f + wave * 9f;
            float baseDamage = Balance != null ? Balance.GetDamage(stage, wave) : 5f + stage * 1.5f + wave * 0.4f;
            int baseGold = Balance != null ? Balance.GetGold(stage, wave) : 8 + stage * 3 + wave * 2;
            int baseExperience = Balance != null ? Balance.GetExperience(stage, wave) : 7 + stage * 2 + wave;
            int archetype = (wave - 1) % 3;
            SpiritElement element = GetElement(stage + wave - 2);

            return archetype switch
            {
                0 => new PrototypeEnemyData("일반 적 A · 균형형", baseHealth, baseDamage, 1.5f, baseGold, baseExperience, element, new Color(0.92f, 0.35f, 0.2f), false),
                1 => new PrototypeEnemyData("일반 적 B · 중장형", baseHealth * 1.45f, baseDamage * 0.9f, 1.9f, Mathf.RoundToInt(baseGold * 1.2f), Mathf.RoundToInt(baseExperience * 1.2f), element, new Color(0.62f, 0.3f, 0.18f), false),
                _ => new PrototypeEnemyData("일반 적 C · 고속형", baseHealth * 0.75f, baseDamage * 1.15f, 1f, baseGold, baseExperience, element, new Color(0.95f, 0.58f, 0.15f), false)
            };
        }

        private static PrototypeEnemyData CreateBoss(int stage)
        {
            float baseHealth = Balance != null ? Balance.GetHealth(stage, 10) : 45f + stage * 16f + 90f;
            float baseDamage = Balance != null ? Balance.GetDamage(stage, 10) : 5f + stage * 1.5f + 4f;
            int baseGold = Balance != null ? Balance.GetGold(stage, 10) : 8 + stage * 3 + 20;
            int baseExperience = Balance != null ? Balance.GetExperience(stage, 10) : 7 + stage * 2 + 10;
            return new PrototypeEnemyData(
                "스테이지 보스",
                baseHealth * 3.2f,
                baseDamage * 1.35f,
                1.35f,
                baseGold * 2,
                baseExperience * 2,
                GetElement(stage - 1),
                new Color(0.72f, 0.08f, 0.18f),
                true);
        }

        private static SpiritElement GetElement(int index)
        {
            SpiritElement[] elements =
            {
                SpiritElement.Water,
                SpiritElement.Fire,
                SpiritElement.Wind,
                SpiritElement.Lightning,
                SpiritElement.Light,
                SpiritElement.Dark
            };
            return elements[Mathf.Abs(index) % elements.Length];
        }
    }
}
