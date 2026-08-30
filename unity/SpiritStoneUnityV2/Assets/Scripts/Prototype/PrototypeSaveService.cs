using System;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public static class PrototypeSaveService
    {
        private const string StageKey = "Prototype.Stage";
        private const string HighestClearedStageKey = "Prototype.HighestClearedStage";
        private const string AutoChallengeKey = "Prototype.AutoChallenge";
        private const string SpiritStonesKey = "Prototype.SpiritStones";
        private const string SsrCommonShardsKey = "Prototype.SsrCommonShards";
        private const string OwnershipInitializedKey = "Prototype.Ownership.Initialized";
        private const string OwnedSpiritKeyPrefix = "Prototype.Ownership.Spirit.";
        private const string SpiritShardKeyPrefix = "Prototype.SpiritShard.";
        private const string SpiritBreakthroughKeyPrefix = "Prototype.SpiritBreakthrough.";
        private const string SpiritSpecialGrowthKeyPrefix = "Prototype.SpiritSpecialGrowth.";
        private const string GoldKey = "Prototype.Gold";
        private const string UpgradeKey = "Prototype.Upgrade";
        private const string ProtagonistBattleCommandKey = "Prototype.Protagonist.BattleCommand";
        private const string ProtagonistSpiritHasteKey = "Prototype.Protagonist.SpiritHaste";
        private const string SpiritUpgradeStonesKey = "Prototype.SpiritTraining.Stones";
        private const string EnhancementStonesKey = "Prototype.SpiritTraining.CommonStones";
        private const string SpiritAttackTrainingKey = "Prototype.SpiritTraining.Attack";
        private const string SpiritDefenseTrainingKey = "Prototype.SpiritTraining.Defense";
        private const string SpiritAttackSpeedTrainingKey = "Prototype.SpiritTraining.AttackSpeed";
        private const string SpiritMaximumHealthTrainingKey = "Prototype.SpiritTraining.MaximumHealth";
        private const string SpiritCriticalChanceTrainingKey = "Prototype.SpiritTraining.CriticalChance";
        private const string SpiritCriticalDamageTrainingKey = "Prototype.SpiritTraining.CriticalDamage";
        private const string ProtagonistLevelKey = "Prototype.ProtagonistLevel";
        private const string ProtagonistExperienceKey = "Prototype.ProtagonistExperience";
        private const string ArcaLevelKey = "Prototype.ArcaLevel";
        private const string ArcaExperienceKey = "Prototype.ArcaExperience";
        private const string LastActiveUtcTicksKey = "Prototype.LastActiveUtcTicks";
        private const string SpiritKeyPrefix = "Prototype.Spirit.";
        private const string FormationSlotKeyPrefix = "Prototype.Formation.Slot.";
        private const string SummonHistoryCountKey = "Prototype.SummonHistory.Count";
        private const string SummonHistoryKeyPrefix = "Prototype.SummonHistory.";
        private static readonly string[] DefaultFormation = { "arca", "ignis", "elysia" };

        public static PrototypeSaveData Load()
        {
            int savedStage = Mathf.Max(1, PlayerPrefs.GetInt(StageKey, 1));
            int migratedHighestClearedStage = Mathf.Max(0, savedStage - 1);
            int savedSpiritHasteLevel = Mathf.Max(0, PlayerPrefs.GetInt(ProtagonistSpiritHasteKey, 0));
            PrototypeSaveData data = new PrototypeSaveData
            {
                Stage = savedStage,
                HighestClearedStage = Mathf.Max(0, PlayerPrefs.GetInt(HighestClearedStageKey, migratedHighestClearedStage)),
                IsAutoChallengeEnabled = PlayerPrefs.GetInt(AutoChallengeKey, 1) == 1,
                SpiritStones = Mathf.Max(0, PlayerPrefs.GetInt(SpiritStonesKey, 300)),
                SsrCommonShards = Mathf.Max(0, PlayerPrefs.GetInt(SsrCommonShardsKey, 0)),
                IsOwnershipInitialized = PlayerPrefs.GetInt(OwnershipInitializedKey, 0) == 1,
                Gold = Mathf.Max(0, PlayerPrefs.GetInt(GoldKey, 0)),
                UpgradeLevel = Mathf.Max(0, PlayerPrefs.GetInt(UpgradeKey, 0)),
                ProtagonistBattleCommandLevel = Mathf.Max(0, PlayerPrefs.GetInt(ProtagonistBattleCommandKey, 0)),
                ProtagonistSpiritHasteLevel = Mathf.Clamp(savedSpiritHasteLevel, 0, IdleBattlePrototype.MaximumSpiritHasteLevel),
                SpiritUpgradeStones = Mathf.Max(0, PlayerPrefs.GetInt(SpiritUpgradeStonesKey, 0)),
                EnhancementStones = Mathf.Max(0, PlayerPrefs.GetInt(EnhancementStonesKey, 0)),
                SpiritAttackTrainingLevel = Mathf.Max(0, PlayerPrefs.GetInt(SpiritAttackTrainingKey, 0)),
                SpiritDefenseTrainingLevel = Mathf.Max(0, PlayerPrefs.GetInt(SpiritDefenseTrainingKey, 0)),
                SpiritAttackSpeedTrainingLevel = Mathf.Max(0, PlayerPrefs.GetInt(SpiritAttackSpeedTrainingKey, 0)),
                SpiritMaximumHealthTrainingLevel = Mathf.Max(0, PlayerPrefs.GetInt(SpiritMaximumHealthTrainingKey, 0)),
                SpiritCriticalChanceTrainingLevel = Mathf.Max(0, PlayerPrefs.GetInt(SpiritCriticalChanceTrainingKey, 0)),
                SpiritCriticalDamageTrainingLevel = Mathf.Max(0, PlayerPrefs.GetInt(SpiritCriticalDamageTrainingKey, 0)),
                ProtagonistLevel = Mathf.Max(1, PlayerPrefs.GetInt(ProtagonistLevelKey, 1)),
                ProtagonistExperience = Mathf.Max(0, PlayerPrefs.GetInt(ProtagonistExperienceKey, 0)),
                ArcaLevel = Mathf.Max(1, PlayerPrefs.GetInt(ArcaLevelKey, 1)),
                ArcaExperience = Mathf.Max(0, PlayerPrefs.GetInt(ArcaExperienceKey, 0))
            };
            if (savedSpiritHasteLevel > IdleBattlePrototype.MaximumSpiritHasteLevel)
            {
                long refund = 0;
                for (int level = IdleBattlePrototype.MaximumSpiritHasteLevel; level < savedSpiritHasteLevel; level++)
                    refund += 100L + level * 75L;
                data.Gold = (int)Math.Min(int.MaxValue, data.Gold + refund);
            }
            foreach (PrototypeSpiritData spirit in PrototypeSpiritCatalog.GetAll())
            {
                bool isLegacyArca = spirit.Id == "arca" && !PlayerPrefs.HasKey(GetSpiritLevelKey(spirit.Id));
                int level = isLegacyArca ? data.ArcaLevel : PlayerPrefs.GetInt(GetSpiritLevelKey(spirit.Id), 1);
                int experience = isLegacyArca ? data.ArcaExperience : PlayerPrefs.GetInt(GetSpiritExperienceKey(spirit.Id), 0);
                data.SpiritProgress.Add(new PrototypeSpiritProgressData
                {
                    SpiritId = spirit.Id,
                    Level = Mathf.Max(1, level),
                    Experience = Mathf.Max(0, experience)
                });
                bool isOwned = data.IsOwnershipInitialized
                    ? PlayerPrefs.GetInt(GetOwnedSpiritKey(spirit.Id), 0) == 1
                    : spirit.Id == "arca" || PlayerPrefs.HasKey(StageKey);
                if (isOwned) data.OwnedSpiritIds.Add(spirit.Id);
                data.SpiritShards.Add(new PrototypeSpiritShardData
                {
                    SpiritId = spirit.Id,
                    Amount = Mathf.Max(0, PlayerPrefs.GetInt(GetSpiritShardKey(spirit.Id), 0))
                });
                data.SpiritBreakthroughs.Add(new PrototypeSpiritBreakthroughData
                {
                    SpiritId = spirit.Id,
                    Level = Mathf.Clamp(PlayerPrefs.GetInt(GetSpiritBreakthroughKey(spirit.Id), 0), 0, 6)
                });
                data.SpiritSpecialGrowth.Add(new PrototypeSpiritSpecialGrowthData
                {
                    SpiritId = spirit.Id,
                    SkillPowerLevel = Mathf.Clamp(PlayerPrefs.GetInt(GetSpiritSkillPowerKey(spirit.Id), 0), 0, PrototypeSpiritSpecialGrowthSystem.MaximumLevel),
                    CooldownReductionLevel = Mathf.Clamp(PlayerPrefs.GetInt(GetSpiritCooldownReductionKey(spirit.Id), 0), 0, PrototypeSpiritSpecialGrowthSystem.MaximumLevel)
                });
            }
            for (int index = 0; index < DefaultFormation.Length; index++)
                data.FormationSpiritIds.Add(PlayerPrefs.GetString(GetFormationSlotKey(index), DefaultFormation[index]));
            int historyCount = Mathf.Clamp(PlayerPrefs.GetInt(SummonHistoryCountKey, 0), 0, PrototypeSummonSystem.MaximumHistoryCount);
            for (int index = 0; index < historyCount; index++)
            {
                string spiritId = PlayerPrefs.GetString(GetSummonHistoryKey(index), string.Empty);
                if (!string.IsNullOrWhiteSpace(spiritId)) data.SummonHistoryIds.Add(spiritId);
            }
            return data;
        }

        public static DateTime? LoadLastActiveUtc()
        {
            string savedTicks = PlayerPrefs.GetString(LastActiveUtcTicksKey, string.Empty);
            if (!long.TryParse(savedTicks, out long ticks)) return null;
            try
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        public static void Save(PrototypeSaveData data, DateTime lastActiveUtc)
        {
            if (data == null)
            {
                Debug.LogErrorFormat("[PrototypeSaveService] Save data must not be null.");
                return;
            }

            PlayerPrefs.SetInt(StageKey, Mathf.Max(1, data.Stage));
            PlayerPrefs.SetInt(HighestClearedStageKey, Mathf.Max(0, data.HighestClearedStage));
            PlayerPrefs.SetInt(AutoChallengeKey, data.IsAutoChallengeEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SpiritStonesKey, Mathf.Max(0, data.SpiritStones));
            PlayerPrefs.SetInt(SsrCommonShardsKey, Mathf.Max(0, data.SsrCommonShards));
            PlayerPrefs.SetInt(OwnershipInitializedKey, 1);
            PlayerPrefs.SetInt(GoldKey, Mathf.Max(0, data.Gold));
            PlayerPrefs.SetInt(UpgradeKey, Mathf.Max(0, data.UpgradeLevel));
            PlayerPrefs.SetInt(ProtagonistBattleCommandKey, Mathf.Max(0, data.ProtagonistBattleCommandLevel));
            PlayerPrefs.SetInt(ProtagonistSpiritHasteKey, Mathf.Clamp(data.ProtagonistSpiritHasteLevel, 0, IdleBattlePrototype.MaximumSpiritHasteLevel));
            PlayerPrefs.SetInt(SpiritUpgradeStonesKey, Mathf.Max(0, data.SpiritUpgradeStones));
            PlayerPrefs.SetInt(EnhancementStonesKey, Mathf.Max(0, data.EnhancementStones));
            PlayerPrefs.SetInt(SpiritAttackTrainingKey, Mathf.Max(0, data.SpiritAttackTrainingLevel));
            PlayerPrefs.SetInt(SpiritDefenseTrainingKey, Mathf.Max(0, data.SpiritDefenseTrainingLevel));
            PlayerPrefs.SetInt(SpiritAttackSpeedTrainingKey, Mathf.Max(0, data.SpiritAttackSpeedTrainingLevel));
            PlayerPrefs.SetInt(SpiritMaximumHealthTrainingKey, Mathf.Max(0, data.SpiritMaximumHealthTrainingLevel));
            PlayerPrefs.SetInt(SpiritCriticalChanceTrainingKey, Mathf.Max(0, data.SpiritCriticalChanceTrainingLevel));
            PlayerPrefs.SetInt(SpiritCriticalDamageTrainingKey, Mathf.Max(0, data.SpiritCriticalDamageTrainingLevel));
            PlayerPrefs.SetInt(ProtagonistLevelKey, Mathf.Max(1, data.ProtagonistLevel));
            PlayerPrefs.SetInt(ProtagonistExperienceKey, Mathf.Max(0, data.ProtagonistExperience));
            PlayerPrefs.SetInt(ArcaLevelKey, Mathf.Max(1, data.ArcaLevel));
            PlayerPrefs.SetInt(ArcaExperienceKey, Mathf.Max(0, data.ArcaExperience));
            for (int index = 0; index < data.SpiritProgress.Count; index++)
            {
                PrototypeSpiritProgressData progress = data.SpiritProgress[index];
                if (progress == null || string.IsNullOrWhiteSpace(progress.SpiritId)) continue;
                PlayerPrefs.SetInt(GetSpiritLevelKey(progress.SpiritId), Mathf.Max(1, progress.Level));
                PlayerPrefs.SetInt(GetSpiritExperienceKey(progress.SpiritId), Mathf.Max(0, progress.Experience));
            }
            for (int index = 0; index < data.FormationSpiritIds.Count; index++)
                PlayerPrefs.SetString(GetFormationSlotKey(index), data.FormationSpiritIds[index] ?? string.Empty);
            foreach (PrototypeSpiritData spirit in PrototypeSpiritCatalog.GetAll())
                PlayerPrefs.SetInt(GetOwnedSpiritKey(spirit.Id), data.OwnedSpiritIds.Contains(spirit.Id) ? 1 : 0);
            for (int index = 0; index < data.SpiritShards.Count; index++)
            {
                PrototypeSpiritShardData shard = data.SpiritShards[index];
                if (shard == null || string.IsNullOrWhiteSpace(shard.SpiritId)) continue;
                PlayerPrefs.SetInt(GetSpiritShardKey(shard.SpiritId), Mathf.Max(0, shard.Amount));
            }
            for (int index = 0; index < data.SpiritBreakthroughs.Count; index++)
            {
                PrototypeSpiritBreakthroughData breakthrough = data.SpiritBreakthroughs[index];
                if (breakthrough == null || string.IsNullOrWhiteSpace(breakthrough.SpiritId)) continue;
                PlayerPrefs.SetInt(GetSpiritBreakthroughKey(breakthrough.SpiritId), Mathf.Clamp(breakthrough.Level, 0, 6));
            }
            for (int index = 0; index < data.SpiritSpecialGrowth.Count; index++)
            {
                PrototypeSpiritSpecialGrowthData growth = data.SpiritSpecialGrowth[index];
                if (growth == null || string.IsNullOrWhiteSpace(growth.SpiritId)) continue;
                PlayerPrefs.SetInt(GetSpiritSkillPowerKey(growth.SpiritId), Mathf.Clamp(growth.SkillPowerLevel, 0, PrototypeSpiritSpecialGrowthSystem.MaximumLevel));
                PlayerPrefs.SetInt(GetSpiritCooldownReductionKey(growth.SpiritId), Mathf.Clamp(growth.CooldownReductionLevel, 0, PrototypeSpiritSpecialGrowthSystem.MaximumLevel));
            }
            int previousHistoryCount = Mathf.Clamp(PlayerPrefs.GetInt(SummonHistoryCountKey, 0), 0, PrototypeSummonSystem.MaximumHistoryCount);
            int historyCount = Mathf.Min(data.SummonHistoryIds.Count, PrototypeSummonSystem.MaximumHistoryCount);
            PlayerPrefs.SetInt(SummonHistoryCountKey, historyCount);
            for (int index = 0; index < historyCount; index++)
                PlayerPrefs.SetString(GetSummonHistoryKey(index), data.SummonHistoryIds[index] ?? string.Empty);
            for (int index = historyCount; index < previousHistoryCount; index++)
                PlayerPrefs.DeleteKey(GetSummonHistoryKey(index));
            SaveLastActiveUtc(lastActiveUtc, false);
            PlayerPrefs.Save();
        }

        public static void SaveLastActiveUtc(DateTime utcTime, bool flush = true)
        {
            PlayerPrefs.SetString(LastActiveUtcTicksKey, utcTime.ToUniversalTime().Ticks.ToString());
            if (flush) PlayerPrefs.Save();
        }

        private static string GetSpiritLevelKey(string spiritId) => $"{SpiritKeyPrefix}{spiritId}.Level";

        private static string GetSpiritExperienceKey(string spiritId) => $"{SpiritKeyPrefix}{spiritId}.Experience";

        private static string GetFormationSlotKey(int slotIndex) => $"{FormationSlotKeyPrefix}{slotIndex}";

        private static string GetOwnedSpiritKey(string spiritId) => $"{OwnedSpiritKeyPrefix}{spiritId}";

        private static string GetSpiritShardKey(string spiritId) => $"{SpiritShardKeyPrefix}{spiritId}";

        private static string GetSpiritBreakthroughKey(string spiritId) => $"{SpiritBreakthroughKeyPrefix}{spiritId}";

        private static string GetSpiritSkillPowerKey(string spiritId) => $"{SpiritSpecialGrowthKeyPrefix}{spiritId}.SkillPower";

        private static string GetSpiritCooldownReductionKey(string spiritId) => $"{SpiritSpecialGrowthKeyPrefix}{spiritId}.CooldownReduction";

        private static string GetSummonHistoryKey(int index) => $"{SummonHistoryKeyPrefix}{index}";
    }
}
