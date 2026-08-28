using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeBattleSystem
    {
        public float EnemyHealth { get; private set; }
        public float EnemyMaximumHealth { get; private set; }
        public float EnemyAttackDamage { get; private set; }
        public float EnemyAttackInterval { get; private set; }
        public float ProtagonistCooldown { get; private set; }
        public float EnemyCooldown { get; private set; }
        public bool IsEnemyDefeated => EnemyHealth <= 0f;

        public void BeginEncounter(PrototypeEnemyData enemy)
        {
            EnemyMaximumHealth = enemy.MaximumHealth;
            EnemyHealth = EnemyMaximumHealth;
            EnemyAttackDamage = enemy.AttackDamage;
            EnemyAttackInterval = enemy.AttackInterval;
            ProtagonistCooldown = 0.2f;
            EnemyCooldown = 1f;
        }

        public void Tick(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            ProtagonistCooldown -= safeDeltaTime;
            EnemyCooldown -= safeDeltaTime;
        }

        public bool TryBeginProtagonistAttack(float interval)
        {
            if (ProtagonistCooldown > 0f || IsEnemyDefeated) return false;
            ProtagonistCooldown = Mathf.Max(0.01f, interval);
            return true;
        }

        public bool TryBeginEnemyAttack()
        {
            if (EnemyCooldown > 0f || IsEnemyDefeated) return false;
            EnemyCooldown = Mathf.Max(0.01f, EnemyAttackInterval);
            return true;
        }

        public float ApplyDamage(float damage)
        {
            float appliedDamage = Mathf.Max(0f, damage);
            EnemyHealth = Mathf.Max(0f, EnemyHealth - appliedDamage);
            return appliedDamage;
        }

        public static float ApplyElement(float rawDamage, SpiritElement attacker, SpiritElement defender) =>
            rawDamage * PrototypeElementChart.GetDamageMultiplier(attacker, defender);

        public static float CalculateProtagonistDamage(float baseAttack, int level, int upgradeLevel)
        {
            float levelMultiplier = 1f + (Mathf.Max(1, level) - 1) * 0.06f;
            return baseAttack * (1f + Mathf.Max(0, upgradeLevel) * 0.12f) * levelMultiplier;
        }

        public static float CalculateSpiritDamage(PrototypeSpiritSlot slot, int level, int upgradeLevel, int breakthrough,
            bool battleCommandActive, float battleCommandBonus)
        {
            float levelMultiplier = 1f + (Mathf.Max(1, level) - 1) * 0.08f;
            float commandMultiplier = battleCommandActive ? 1f + battleCommandBonus : 1f;
            float evolutionMultiplier = slot.Spirit.GetEvolutionForLevel(level).AttackMultiplier;
            float skillMultiplier = slot.SkillTwoRemaining > 0f && slot.Spirit.SkillTwo.Effect == SpiritAbilityEffect.AttackPowerBuff
                ? slot.Spirit.SkillTwo.PowerMultiplier : 1f;
            float breakthroughMultiplier = 1f + Mathf.Max(0, breakthrough) * 0.08f;
            return slot.Spirit.BaseAttack * (1f + Mathf.Max(0, upgradeLevel) * 0.12f) * levelMultiplier
                * commandMultiplier * evolutionMultiplier * skillMultiplier * breakthroughMultiplier;
        }

        public static float GetEnemyAttackMultiplier(PrototypeSpiritSlot[] slots)
        {
            float multiplier = 1f;
            if (slots == null) return multiplier;
            foreach (PrototypeSpiritSlot slot in slots)
                if (slot.IsAssigned && slot.SkillOneRemaining > 0f && slot.Spirit.SkillOne.Effect == SpiritAbilityEffect.EnemyAttackReduction)
                    multiplier = Mathf.Min(multiplier, slot.Spirit.SkillOne.PowerMultiplier);
            return multiplier;
        }

        public static float GetIncomingDamageMultiplier(PrototypeSpiritSlot[] slots)
        {
            float multiplier = 1f;
            if (slots == null) return multiplier;
            foreach (PrototypeSpiritSlot slot in slots)
                if (slot.IsAssigned && slot.UltimateRemaining > 0f && slot.Spirit.Ultimate.Effect == SpiritAbilityEffect.DamageReduction)
                    multiplier = Mathf.Min(multiplier, slot.Spirit.Ultimate.PowerMultiplier);
            return multiplier;
        }

        public static float GetSpiritAttackInterval(PrototypeSpiritSlot slot, bool hasteActive, float hasteMultiplier)
        {
            float interval = slot.Spirit.AttackInterval;
            if (slot.SkillTwoRemaining > 0f && slot.Spirit.SkillTwo.Effect == SpiritAbilityEffect.AttackSpeedBuff)
                interval *= slot.Spirit.SkillTwo.PowerMultiplier;
            if (hasteActive) interval *= hasteMultiplier;
            return interval;
        }
    }
}
