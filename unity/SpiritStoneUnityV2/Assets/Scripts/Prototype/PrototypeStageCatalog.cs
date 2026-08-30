using UnityEngine;

namespace SpiritStone.Prototype
{
    public static class PrototypeStageCatalog
    {
        private static readonly string[] BossNames =
        {
            "심해의 수호자", "잿불의 포식자", "폭풍의 감시자", "뇌광의 집행자", "광휘의 심판자",
            "심연의 파수꾼", "빙류의 거신", "화산의 군주", "천공의 지배자", "원소 융합체"
        };
        private static readonly SpiritElement[] ZoneElements =
        {
            SpiritElement.Water, SpiritElement.Fire, SpiritElement.Wind, SpiritElement.Lightning,
            SpiritElement.Light, SpiritElement.Dark, SpiritElement.Water, SpiritElement.Fire,
            SpiritElement.Wind, SpiritElement.Lightning
        };
        private static readonly PrototypeEnemyArchetype[] EncounterPattern =
        {
            PrototypeEnemyArchetype.Balanced, PrototypeEnemyArchetype.Fast, PrototypeEnemyArchetype.Balanced,
            PrototypeEnemyArchetype.Heavy, PrototypeEnemyArchetype.Fast, PrototypeEnemyArchetype.Balanced,
            PrototypeEnemyArchetype.Heavy, PrototypeEnemyArchetype.Fast, PrototypeEnemyArchetype.Heavy
        };
        private static PrototypeStageBalanceDefinition balance;
        private static PrototypeStageBalanceDefinition Balance => balance ??= Resources.Load<PrototypeStageBalanceDefinition>("Prototype/StageBalance");

        public static PrototypeStageData Create(int stageNumber)
        {
            int safeStage = Mathf.Max(1, stageNumber);
            PrototypeStageContentData content = CreateContent(safeStage);
            PrototypeEnemyData[] encounters = new PrototypeEnemyData[10];
            for (int wave = 1; wave <= 9; wave++)
                encounters[wave - 1] = CreateNormalEnemy(safeStage, wave, content);
            encounters[9] = CreateBoss(safeStage, content);
            return new PrototypeStageData(
                safeStage,
                encounters,
                Balance != null ? Balance.GetClearGold(safeStage) : 40 + safeStage * 15,
                Balance != null ? Balance.GetClearExperience(safeStage) : 25 + safeStage * 5,
                content.FirstClearSpiritStones, content.RepeatClearSpiritStones,
                content.NormalEnhancementStoneDropChance, content.BossSpiritUpgradeStoneDropChance);
        }

        public static PrototypeStageContentData CreateContent(int stageNumber)
        {
            int stage = Mathf.Max(1, stageNumber);
            int index = (stage - 1) % 10;
            SpiritElement primary = ZoneElements[index];
            SpiritElement secondary = GetElement((int)primary + 1);
            SpiritElement[] elements = new SpiritElement[9];
            PrototypeEnemyArchetype[] archetypes = new PrototypeEnemyArchetype[9];
            for (int wave = 0; wave < 9; wave++)
            {
                elements[wave] = wave == 5 || wave == 8 ? secondary : primary;
                archetypes[wave] = EncounterPattern[(wave + index) % EncounterPattern.Length];
            }
            return new PrototypeStageContentData(stage, elements, archetypes, BossNames[index], primary,
                30 + index * 5, 5 + index / 3, 0.15f + index * 0.01f, 0.25f + index * 0.02f);
        }

        private static PrototypeEnemyData CreateNormalEnemy(int stage, int wave, PrototypeStageContentData content)
        {
            float baseHealth = Balance != null ? Balance.GetHealth(stage, wave) : 45f + stage * 16f + wave * 9f;
            float baseDamage = Balance != null ? Balance.GetDamage(stage, wave) : 5f + stage * 1.5f + wave * 0.4f;
            int baseGold = Balance != null ? Balance.GetGold(stage, wave) : 8 + stage * 3 + wave * 2;
            int baseExperience = Balance != null ? Balance.GetExperience(stage, wave) : 7 + stage * 2 + wave;
            SpiritElement element = content.NormalElements[wave - 1];
            return PrototypeEnemyCatalog.GetNormal(content.NormalArchetypes[wave - 1])
                .CreateRuntimeData(baseHealth, baseDamage, baseGold, baseExperience, element);
        }

        private static PrototypeEnemyData CreateBoss(int stage, PrototypeStageContentData content)
        {
            float baseHealth = Balance != null ? Balance.GetHealth(stage, 10) : 45f + stage * 16f + 90f;
            float baseDamage = Balance != null ? Balance.GetDamage(stage, 10) : 5f + stage * 1.5f + 4f;
            int baseGold = Balance != null ? Balance.GetGold(stage, 10) : 8 + stage * 3 + 20;
            int baseExperience = Balance != null ? Balance.GetExperience(stage, 10) : 7 + stage * 2 + 10;
            return PrototypeEnemyCatalog.GetBoss().CreateRuntimeData(
                baseHealth, baseDamage, baseGold, baseExperience, content.BossElement, content.BossName);
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
