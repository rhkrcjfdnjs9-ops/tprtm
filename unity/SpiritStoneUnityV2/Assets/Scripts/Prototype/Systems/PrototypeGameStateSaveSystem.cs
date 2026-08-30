using System;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeGameStateSaveSystem
    {
        private readonly PrototypeStageProgression stageProgression;
        private readonly PrototypeSpiritGrowthSystem spiritGrowth;
        private readonly PrototypeFormationSystem formationSystem;
        private readonly PrototypeSummonSystem summonSystem;
        private readonly PrototypeSpiritTrainingSystem spiritTrainingSystem;
        private readonly PrototypeSpiritSpecialGrowthSystem spiritSpecialGrowthSystem;

        public PrototypeGameStateSaveSystem(
            PrototypeStageProgression stageProgression,
            PrototypeSpiritGrowthSystem spiritGrowth,
            PrototypeFormationSystem formationSystem,
            PrototypeSummonSystem summonSystem,
            PrototypeSpiritTrainingSystem spiritTrainingSystem,
            PrototypeSpiritSpecialGrowthSystem spiritSpecialGrowthSystem)
        {
            this.stageProgression = stageProgression ?? throw new ArgumentNullException(nameof(stageProgression));
            this.spiritGrowth = spiritGrowth ?? throw new ArgumentNullException(nameof(spiritGrowth));
            this.formationSystem = formationSystem ?? throw new ArgumentNullException(nameof(formationSystem));
            this.summonSystem = summonSystem ?? throw new ArgumentNullException(nameof(summonSystem));
            this.spiritTrainingSystem = spiritTrainingSystem ?? throw new ArgumentNullException(nameof(spiritTrainingSystem));
            this.spiritSpecialGrowthSystem = spiritSpecialGrowthSystem ?? throw new ArgumentNullException(nameof(spiritSpecialGrowthSystem));
        }

        public PrototypeSaveData CreateSaveData(int gold, int upgradeLevel, int battleCommandLevel, int spiritHasteLevel,
            int protagonistLevel, int protagonistExperience)
        {
            PrototypeSpiritProgress arcaProgress = spiritGrowth.Get("arca");
            PrototypeSaveData saveData = new()
            {
                Gold = gold,
                UpgradeLevel = upgradeLevel,
                ProtagonistBattleCommandLevel = battleCommandLevel,
                ProtagonistSpiritHasteLevel = spiritHasteLevel,
                ProtagonistLevel = protagonistLevel,
                ProtagonistExperience = protagonistExperience,
                ArcaLevel = arcaProgress?.Level ?? 1,
                ArcaExperience = arcaProgress?.Experience ?? 0
            };
            stageProgression.WriteTo(saveData);
            spiritGrowth.WriteTo(saveData);
            formationSystem.WriteTo(saveData);
            summonSystem.WriteTo(saveData);
            spiritTrainingSystem.WriteTo(saveData);
            spiritSpecialGrowthSystem.WriteTo(saveData);
            return saveData;
        }

        public void Save(int gold, int upgradeLevel, int battleCommandLevel, int spiritHasteLevel,
            int protagonistLevel, int protagonistExperience, DateTime utcNow)
        {
            PrototypeSaveService.Save(
                CreateSaveData(gold, upgradeLevel, battleCommandLevel, spiritHasteLevel, protagonistLevel, protagonistExperience),
                utcNow);
        }
    }
}
