using UnityEngine;

namespace SpiritStone.Prototype
{
    public enum PrototypeSpiritTrainingStat
    {
        Attack,
        Defense,
        AttackSpeed,
        MaximumHealth,
        CriticalChance,
        CriticalDamage
    }

    public sealed class PrototypeSpiritTrainingSystem
    {
        public int UpgradeStones { get; private set; }
        public int EnhancementStones { get; private set; }
        public int AttackLevel { get; private set; }
        public int DefenseLevel { get; private set; }
        public int AttackSpeedLevel { get; private set; }
        public int MaximumHealthLevel { get; private set; }
        public int CriticalChanceLevel { get; private set; }
        public int CriticalDamageLevel { get; private set; }

        public float AttackMultiplier => 1f + AttackLevel * 0.04f;
        public float DefenseMultiplier => 1f + DefenseLevel * 0.04f;
        public float AttackIntervalMultiplier => Mathf.Max(0.6f, 1f - AttackSpeedLevel * 0.015f);
        public float MaximumHealthMultiplier => 1f + MaximumHealthLevel * 0.05f;
        public float CriticalChance => Mathf.Min(0.5f, 0.05f + CriticalChanceLevel * 0.005f);
        public float CriticalDamageMultiplier => Mathf.Min(3f, 1.5f + CriticalDamageLevel * 0.02f);

        public void Initialize(PrototypeSaveData data)
        {
            UpgradeStones = Mathf.Max(0, data.SpiritUpgradeStones);
            EnhancementStones = Mathf.Max(0, data.EnhancementStones);
            AttackLevel = Mathf.Max(0, data.SpiritAttackTrainingLevel);
            DefenseLevel = Mathf.Max(0, data.SpiritDefenseTrainingLevel);
            AttackSpeedLevel = Mathf.Max(0, data.SpiritAttackSpeedTrainingLevel);
            MaximumHealthLevel = Mathf.Max(0, data.SpiritMaximumHealthTrainingLevel);
            CriticalChanceLevel = Mathf.Max(0, data.SpiritCriticalChanceTrainingLevel);
            CriticalDamageLevel = Mathf.Max(0, data.SpiritCriticalDamageTrainingLevel);
        }

        public void AddUpgradeStones(int amount) => UpgradeStones += Mathf.Max(0, amount);

        public void AddEnhancementStones(int amount) => EnhancementStones += Mathf.Max(0, amount);

        public bool TrySpendSpiritUpgradeStone()
        {
            if (UpgradeStones <= 0) return false;
            UpgradeStones--;
            return true;
        }

        public int GetLevel(PrototypeSpiritTrainingStat stat) => stat switch
        {
            PrototypeSpiritTrainingStat.Attack => AttackLevel,
            PrototypeSpiritTrainingStat.Defense => DefenseLevel,
            PrototypeSpiritTrainingStat.AttackSpeed => AttackSpeedLevel,
            PrototypeSpiritTrainingStat.MaximumHealth => MaximumHealthLevel,
            PrototypeSpiritTrainingStat.CriticalChance => CriticalChanceLevel,
            PrototypeSpiritTrainingStat.CriticalDamage => CriticalDamageLevel,
            _ => 0
        };

        public int GetCost(PrototypeSpiritTrainingStat stat) => 5 + GetLevel(stat) * 2;

        public float GetEffectValue(PrototypeSpiritTrainingStat stat, int levelOffset = 0)
        {
            int level = Mathf.Max(0, GetLevel(stat) + levelOffset);
            return stat switch
            {
                PrototypeSpiritTrainingStat.Attack => level * 4f,
                PrototypeSpiritTrainingStat.Defense => level * 4f,
                PrototypeSpiritTrainingStat.AttackSpeed => Mathf.Min(40f, level * 1.5f),
                PrototypeSpiritTrainingStat.MaximumHealth => level * 5f,
                PrototypeSpiritTrainingStat.CriticalChance => Mathf.Min(50f, 5f + level * 0.5f),
                PrototypeSpiritTrainingStat.CriticalDamage => Mathf.Min(300f, 150f + level * 2f),
                _ => 0f
            };
        }

        public bool TryUpgrade(PrototypeSpiritTrainingStat stat)
        {
            if (!System.Enum.IsDefined(typeof(PrototypeSpiritTrainingStat), stat)) return false;
            int cost = GetCost(stat);
            if (EnhancementStones < cost) return false;
            EnhancementStones -= cost;
            switch (stat)
            {
                case PrototypeSpiritTrainingStat.Attack: AttackLevel++; break;
                case PrototypeSpiritTrainingStat.Defense: DefenseLevel++; break;
                case PrototypeSpiritTrainingStat.AttackSpeed: AttackSpeedLevel++; break;
                case PrototypeSpiritTrainingStat.MaximumHealth: MaximumHealthLevel++; break;
                case PrototypeSpiritTrainingStat.CriticalChance: CriticalChanceLevel++; break;
                case PrototypeSpiritTrainingStat.CriticalDamage: CriticalDamageLevel++; break;
            }
            return true;
        }

        public void WriteTo(PrototypeSaveData data)
        {
            data.SpiritUpgradeStones = UpgradeStones;
            data.EnhancementStones = EnhancementStones;
            data.SpiritAttackTrainingLevel = AttackLevel;
            data.SpiritDefenseTrainingLevel = DefenseLevel;
            data.SpiritAttackSpeedTrainingLevel = AttackSpeedLevel;
            data.SpiritMaximumHealthTrainingLevel = MaximumHealthLevel;
            data.SpiritCriticalChanceTrainingLevel = CriticalChanceLevel;
            data.SpiritCriticalDamageTrainingLevel = CriticalDamageLevel;
        }
    }
}
