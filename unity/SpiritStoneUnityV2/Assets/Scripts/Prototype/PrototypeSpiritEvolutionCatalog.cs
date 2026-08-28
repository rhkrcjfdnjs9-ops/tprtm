using UnityEngine;

namespace SpiritStone.Prototype
{
    public static class PrototypeSpiritEvolutionCatalog
    {
        public static PrototypeSpiritEvolutionData GetForLevel(int level)
        {
            int safeLevel = Mathf.Max(1, level);
            if (safeLevel >= 50)
                return new PrototypeSpiritEvolutionData(SpiritEvolutionStage.Awakened, "정령 각성", "해방된 힘을 완전히 깨운 상태", 2.3f, new Color(0.92f, 0.72f, 1f));
            if (safeLevel >= 30)
                return new PrototypeSpiritEvolutionData(SpiritEvolutionStage.Liberated, "정령 해방", "인간형 정령으로 해방된 상태", 1.7f, new Color(0.72f, 0.3f, 1f));
            if (safeLevel >= 20)
                return new PrototypeSpiritEvolutionData(SpiritEvolutionStage.SpiritStoneThree, "정령돌 3단계", "일상 대화가 가능하고 오라가 생성된 상태", 1.35f, new Color(0.65f, 0.32f, 0.9f));
            if (safeLevel >= 10)
                return new PrototypeSpiritEvolutionData(SpiritEvolutionStage.SpiritStoneTwo, "정령돌 2단계", "가끔 말하며 선명한 빛이 나는 상태", 1.15f, new Color(0.48f, 0.3f, 0.7f));
            return new PrototypeSpiritEvolutionData(SpiritEvolutionStage.SpiritStoneOne, "정령돌 1단계", "말하지 않고 은은한 빛이 나는 상태", 1f, new Color(0.32f, 0.28f, 0.42f));
        }
    }
}
