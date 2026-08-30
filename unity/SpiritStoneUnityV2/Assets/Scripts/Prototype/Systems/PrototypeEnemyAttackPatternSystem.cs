namespace SpiritStone.Prototype
{
    public readonly struct PrototypeEnemyAttackPattern
    {
        public PrototypeEnemyAttackPattern(string displayName, int hitCount, float damageMultiplier, bool targetsAll)
        {
            DisplayName = displayName;
            HitCount = hitCount;
            DamageMultiplier = damageMultiplier;
            TargetsAll = targetsAll;
        }

        public string DisplayName { get; }
        public int HitCount { get; }
        public float DamageMultiplier { get; }
        public bool TargetsAll { get; }
    }

    public static class PrototypeEnemyAttackPatternSystem
    {
        public static PrototypeEnemyAttackPattern GetBasicPattern(PrototypeEnemyArchetype archetype)
        {
            return archetype switch
            {
                PrototypeEnemyArchetype.Fast => new PrototypeEnemyAttackPattern("고속 연타", 2, 0.58f, false),
                PrototypeEnemyArchetype.Heavy => new PrototypeEnemyAttackPattern("중장 강타", 1, 1.35f, false),
                PrototypeEnemyArchetype.Boss => new PrototypeEnemyAttackPattern("보스 공격", 1, 1f, false),
                _ => new PrototypeEnemyAttackPattern("균형 공격", 1, 1f, false)
            };
        }

        public static PrototypeEnemyAttackPattern GetBossSpecialPattern(SpiritElement element)
        {
            return element switch
            {
                SpiritElement.Water => new PrototypeEnemyAttackPattern("심해의 해일", 1, 0.65f, true),
                SpiritElement.Fire => new PrototypeEnemyAttackPattern("화염 폭발", 1, 0.85f, true),
                SpiritElement.Wind => new PrototypeEnemyAttackPattern("폭풍 난무", 1, 0.6f, true),
                SpiritElement.Lightning => new PrototypeEnemyAttackPattern("연쇄 낙뢰", 1, 0.7f, true),
                SpiritElement.Light => new PrototypeEnemyAttackPattern("광휘의 심판", 1, 0.75f, true),
                SpiritElement.Dark => new PrototypeEnemyAttackPattern("심연 붕괴", 1, 0.7f, true),
                _ => new PrototypeEnemyAttackPattern("지면 붕괴", 1, 0.7f, true)
            };
        }
    }
}
