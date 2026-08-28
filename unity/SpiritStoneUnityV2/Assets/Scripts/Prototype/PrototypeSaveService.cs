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
        private const string GoldKey = "Prototype.Gold";
        private const string UpgradeKey = "Prototype.Upgrade";
        private const string ProtagonistLevelKey = "Prototype.ProtagonistLevel";
        private const string ProtagonistExperienceKey = "Prototype.ProtagonistExperience";
        private const string ArcaLevelKey = "Prototype.ArcaLevel";
        private const string ArcaExperienceKey = "Prototype.ArcaExperience";
        private const string LastActiveUtcTicksKey = "Prototype.LastActiveUtcTicks";
        private const string SpiritKeyPrefix = "Prototype.Spirit.";
        private const string FormationSlotKeyPrefix = "Prototype.Formation.Slot.";
        private static readonly string[] DefaultFormation = { "arca", "ignis", "elysia" };

        public static PrototypeSaveData Load()
        {
            int savedStage = Mathf.Max(1, PlayerPrefs.GetInt(StageKey, 1));
            int migratedHighestClearedStage = Mathf.Max(0, savedStage - 1);
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
                ProtagonistLevel = Mathf.Max(1, PlayerPrefs.GetInt(ProtagonistLevelKey, 1)),
                ProtagonistExperience = Mathf.Max(0, PlayerPrefs.GetInt(ProtagonistExperienceKey, 0)),
                ArcaLevel = Mathf.Max(1, PlayerPrefs.GetInt(ArcaLevelKey, 1)),
                ArcaExperience = Mathf.Max(0, PlayerPrefs.GetInt(ArcaExperienceKey, 0))
            };
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
            }
            for (int index = 0; index < DefaultFormation.Length; index++)
                data.FormationSpiritIds.Add(PlayerPrefs.GetString(GetFormationSlotKey(index), DefaultFormation[index]));
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
    }
}
