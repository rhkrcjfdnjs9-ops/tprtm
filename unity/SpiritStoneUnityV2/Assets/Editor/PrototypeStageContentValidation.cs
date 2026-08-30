using System;
using System.Collections.Generic;
using SpiritStone.Prototype;
using UnityEditor;

namespace SpiritStone.Editor
{
    public static class PrototypeStageContentValidation
    {
        [MenuItem("Tools/Spirit Stone/Validate Stage Content")]
        public static void Run()
        {
            HashSet<string> bossNames = new(StringComparer.Ordinal);
            for (int stageNumber = 1; stageNumber <= 10; stageNumber++)
            {
                PrototypeStageContentData content = PrototypeStageCatalog.CreateContent(stageNumber);
                PrototypeStageData stage = PrototypeStageCatalog.Create(stageNumber);
                Require(content.NormalElements.Length == 9, $"Stage {stageNumber} requires nine normal elements.");
                Require(content.NormalArchetypes.Length == 9, $"Stage {stageNumber} requires nine normal archetypes.");
                Require(bossNames.Add(content.BossName), $"Stage {stageNumber} boss name must be unique.");
                Require(stage.GetEncounter(10).IsBoss, $"Stage {stageNumber} final encounter must be a boss.");
                Require(stage.GetEncounter(10).DisplayName == content.BossName, $"Stage {stageNumber} boss name mismatch.");
                Require(stage.FirstClearSpiritStoneReward > stage.RepeatClearSpiritStoneReward,
                    $"Stage {stageNumber} first-clear reward must exceed repeat reward.");
                Require(stage.NormalEnhancementStoneDropChance > 0f && stage.NormalEnhancementStoneDropChance <= 1f,
                    $"Stage {stageNumber} normal drop chance is invalid.");
                Require(stage.BossSpiritUpgradeStoneDropChance > 0f && stage.BossSpiritUpgradeStoneDropChance <= 1f,
                    $"Stage {stageNumber} boss drop chance is invalid.");
            }

            PrototypeStageProgression repeatProgression = new();
            repeatProgression.Initialize(new PrototypeSaveData
            {
                Stage = 1,
                HighestClearedStage = 10,
                IsAutoChallengeEnabled = true
            });
            repeatProgression.CompleteStage();
            Require(repeatProgression.Stage == 2, "Auto challenge must advance after clearing a previously cleared stage.");

            PrototypeStageProgression newProgression = new();
            newProgression.Initialize(new PrototypeSaveData
            {
                Stage = 11,
                HighestClearedStage = 10,
                IsAutoChallengeEnabled = true
            });
            newProgression.CompleteStage();
            Require(newProgression.Stage == 12, "A newly cleared highest stage must advance.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
