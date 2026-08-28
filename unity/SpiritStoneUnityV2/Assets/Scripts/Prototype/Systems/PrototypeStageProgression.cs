using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeStageProgression
    {
        public int Stage { get; private set; }
        public int HighestClearedStage { get; private set; }
        public bool IsAutoChallengeEnabled { get; private set; }
        public bool CanSelectPrevious => Stage > 1;
        public bool CanSelectNext => Stage < HighestClearedStage + 1;

        public void Initialize(PrototypeSaveData saveData)
        {
            HighestClearedStage = Mathf.Max(0, saveData.HighestClearedStage);
            IsAutoChallengeEnabled = saveData.IsAutoChallengeEnabled;
            Stage = Mathf.Clamp(saveData.Stage, 1, HighestClearedStage + 1);
        }

        public void ToggleAutoChallenge() => IsAutoChallengeEnabled = !IsAutoChallengeEnabled;

        public bool TrySelect(int requestedStage)
        {
            int selected = Mathf.Clamp(requestedStage, 1, HighestClearedStage + 1);
            if (selected == Stage) return false;
            Stage = selected;
            return true;
        }

        public void CompleteStage()
        {
            HighestClearedStage = Mathf.Max(HighestClearedStage, Stage);
            Stage = IsAutoChallengeEnabled ? HighestClearedStage + 1 : HighestClearedStage;
        }

        public bool ReturnFromFailedBossChallenge()
        {
            if (Stage <= Mathf.Max(1, HighestClearedStage)) return false;
            Stage = Mathf.Max(1, HighestClearedStage);
            return true;
        }

        public void WriteTo(PrototypeSaveData saveData)
        {
            saveData.Stage = Stage;
            saveData.HighestClearedStage = HighestClearedStage;
            saveData.IsAutoChallengeEnabled = IsAutoChallengeEnabled;
        }
    }
}
