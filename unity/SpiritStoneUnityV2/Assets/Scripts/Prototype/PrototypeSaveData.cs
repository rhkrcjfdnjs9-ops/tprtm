namespace SpiritStone.Prototype
{
    public sealed class PrototypeSaveData
    {
        public int Stage { get; set; } = 1;
        public int HighestClearedStage { get; set; }
        public bool IsAutoChallengeEnabled { get; set; } = true;
        public int SpiritStones { get; set; } = 300;
        public int SsrCommonShards { get; set; }
        public bool IsOwnershipInitialized { get; set; }
        public int Gold { get; set; }
        public int UpgradeLevel { get; set; }
        public int ProtagonistBattleCommandLevel { get; set; }
        public int ProtagonistSpiritHasteLevel { get; set; }
        public int SpiritUpgradeStones { get; set; }
        public int EnhancementStones { get; set; }
        public int SpiritAttackTrainingLevel { get; set; }
        public int SpiritDefenseTrainingLevel { get; set; }
        public int SpiritAttackSpeedTrainingLevel { get; set; }
        public int SpiritMaximumHealthTrainingLevel { get; set; }
        public int SpiritCriticalChanceTrainingLevel { get; set; }
        public int SpiritCriticalDamageTrainingLevel { get; set; }
        public int ProtagonistLevel { get; set; } = 1;
        public int ProtagonistExperience { get; set; }
        public int ArcaLevel { get; set; } = 1;
        public int ArcaExperience { get; set; }
        public System.Collections.Generic.List<PrototypeSpiritProgressData> SpiritProgress { get; } = new();
        public System.Collections.Generic.List<string> FormationSpiritIds { get; } = new();
        public System.Collections.Generic.List<string> OwnedSpiritIds { get; } = new();
        public System.Collections.Generic.List<PrototypeSpiritShardData> SpiritShards { get; } = new();
        public System.Collections.Generic.List<PrototypeSpiritBreakthroughData> SpiritBreakthroughs { get; } = new();
        public System.Collections.Generic.List<PrototypeSpiritSpecialGrowthData> SpiritSpecialGrowth { get; } = new();
        public System.Collections.Generic.List<string> SummonHistoryIds { get; } = new();
    }
}
