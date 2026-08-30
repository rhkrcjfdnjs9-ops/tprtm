namespace SpiritStone.Prototype
{
    public sealed class PrototypeStageContentData
    {
        public PrototypeStageContentData(int stageNumber, SpiritElement[] normalElements,
            PrototypeEnemyArchetype[] normalArchetypes, string bossName, SpiritElement bossElement,
            int firstClearSpiritStones, int repeatClearSpiritStones,
            float normalEnhancementStoneDropChance, float bossSpiritUpgradeStoneDropChance)
        {
            StageNumber = stageNumber;
            NormalElements = normalElements;
            NormalArchetypes = normalArchetypes;
            BossName = bossName;
            BossElement = bossElement;
            FirstClearSpiritStones = firstClearSpiritStones;
            RepeatClearSpiritStones = repeatClearSpiritStones;
            NormalEnhancementStoneDropChance = normalEnhancementStoneDropChance;
            BossSpiritUpgradeStoneDropChance = bossSpiritUpgradeStoneDropChance;
        }

        public int StageNumber { get; }
        public SpiritElement[] NormalElements { get; }
        public PrototypeEnemyArchetype[] NormalArchetypes { get; }
        public string BossName { get; }
        public SpiritElement BossElement { get; }
        public int FirstClearSpiritStones { get; }
        public int RepeatClearSpiritStones { get; }
        public float NormalEnhancementStoneDropChance { get; }
        public float BossSpiritUpgradeStoneDropChance { get; }
    }
}
