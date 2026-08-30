using System;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class PrototypeSaveOfflineValidation
    {
        [MenuItem("Tools/Prototype/Validate Save And Offline Rewards")]
        public static void Run()
        {
            DateTime now = new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
            PrototypeOfflineReward shortAbsence = PrototypeOfflineRewardCalculator.Calculate(
                now, now.AddSeconds(-59), 1, 8f, 0.6f);
            Require(!shortAbsence.HasReward, "Less than one completed minute must not grant a reward.");

            PrototypeOfflineReward oneMinute = PrototypeOfflineRewardCalculator.Calculate(
                now, now.AddMinutes(-1), 1, 8f, 0.6f);
            Require(oneMinute.CompletedMinutes == 1 && oneMinute.Gold == 3 && oneMinute.Experience == 2,
                "One minute at stage one must match the configured reward formula.");

            PrototypeOfflineReward capped = PrototypeOfflineRewardCalculator.Calculate(
                now, now.AddHours(-24), 10, 8f, 0.6f);
            Require(capped.CompletedMinutes == 480, "Offline progress must be capped at eight hours.");
            PrototypeOfflineReward futureTimestamp = PrototypeOfflineRewardCalculator.Calculate(
                now, now.AddHours(1), 10, 8f, 0.6f);
            Require(!futureTimestamp.HasReward, "A future timestamp must never grant a reward.");

            PrototypeSaveData source = new()
            {
                Stage = 12,
                HighestClearedStage = 11,
                SpiritStones = 777,
                SsrCommonShards = 4,
                Gold = 12345,
                EnhancementStones = 19,
                SpiritUpgradeStones = 7,
                ProtagonistLevel = 8,
                ProtagonistExperience = 17,
                IsOwnershipInitialized = true
            };
            source.OwnedSpiritIds.Add("arca");
            source.OwnedSpiritIds.Add("windy");
            source.FormationSpiritIds.Add("windy");
            source.FormationSpiritIds.Add("arca");
            source.FormationSpiritIds.Add(string.Empty);

            PrototypeSummonSystem summons = new();
            PrototypeFormationSystem formation = new();
            PrototypeStageProgression stages = new();
            PrototypeSpiritGrowthSystem growth = new();
            PrototypeSpiritTrainingSystem training = new();
            PrototypeSpiritSpecialGrowthSystem specialGrowth = new();
            summons.Initialize(source);
            formation.Initialize(source, summons);
            stages.Initialize(source);
            growth.Initialize(source);
            training.Initialize(source);
            specialGrowth.Initialize(source);
            PrototypeGameStateSaveSystem saveSystem = new(stages, growth, formation, summons, training, specialGrowth);
            PrototypeSaveData restored = saveSystem.CreateSaveData(
                source.Gold, source.UpgradeLevel, source.ProtagonistBattleCommandLevel,
                source.ProtagonistSpiritHasteLevel, source.ProtagonistLevel, source.ProtagonistExperience);

            Require(restored.Stage == 12 && restored.HighestClearedStage == 11,
                "Stage progress must survive save-data reconstruction.");
            Require(restored.Gold == 12345 && restored.SpiritStones == 777,
                "Currencies must survive save-data reconstruction.");
            Require(restored.EnhancementStones == 19 && restored.SpiritUpgradeStones == 7,
                "Growth materials must survive save-data reconstruction.");
            Require(restored.FormationSpiritIds.Count == 3 && restored.FormationSpiritIds[0] == "windy",
                "Formation order must survive save-data reconstruction.");

            Debug.LogFormat("[PrototypeSaveOfflineValidation] PASS: offline limits and save-data reconstruction are valid.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"[PrototypeSaveOfflineValidation] {message}");
        }
    }
}
