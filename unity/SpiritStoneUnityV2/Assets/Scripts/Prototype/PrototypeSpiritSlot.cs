using System;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeSpiritSlot
    {
        public PrototypeSpiritSlot(int slotIndex)
        {
            if (slotIndex < 0) throw new ArgumentOutOfRangeException(nameof(slotIndex));
            SlotIndex = slotIndex;
        }

        public int SlotIndex { get; }
        public PrototypeSpiritData Spirit { get; private set; }
        public string SpiritId => Spirit?.Id ?? string.Empty;
        public string DisplayName { get; private set; } = "비어 있음";
        public bool IsAssigned { get; private set; }
        public float AttackCooldownRemaining { get; private set; }
        public float SkillOneCooldownRemaining { get; private set; }
        public float SkillOneRemaining { get; private set; }
        public float SkillTwoCooldownRemaining { get; private set; }
        public float SkillTwoRemaining { get; private set; }
        public float UltimateEnergy { get; private set; }
        public float UltimateRemaining { get; private set; }
        public bool IsActing { get; private set; }

        public void Assign(PrototypeSpiritData spirit, float initialCooldown)
        {
            Spirit = spirit ?? throw new ArgumentNullException(nameof(spirit));
            DisplayName = spirit.DisplayName;
            IsAssigned = true;
            AttackCooldownRemaining = Math.Max(0f, initialCooldown);
            SkillOneCooldownRemaining = 1.5f;
            SkillTwoCooldownRemaining = 3f;
        }

        public void Clear()
        {
            Spirit = null;
            DisplayName = "비어 있음";
            IsAssigned = false;
            AttackCooldownRemaining = 0f;
            SkillOneCooldownRemaining = 0f;
            SkillOneRemaining = 0f;
            SkillTwoCooldownRemaining = 0f;
            SkillTwoRemaining = 0f;
            UltimateEnergy = 0f;
            UltimateRemaining = 0f;
            IsActing = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsAssigned) return;
            float safeDeltaTime = Math.Max(0f, deltaTime);
            AttackCooldownRemaining = Math.Max(0f, AttackCooldownRemaining - safeDeltaTime);
            SkillOneCooldownRemaining = Math.Max(0f, SkillOneCooldownRemaining - safeDeltaTime);
            SkillOneRemaining = Math.Max(0f, SkillOneRemaining - safeDeltaTime);
            SkillTwoCooldownRemaining = Math.Max(0f, SkillTwoCooldownRemaining - safeDeltaTime);
            SkillTwoRemaining = Math.Max(0f, SkillTwoRemaining - safeDeltaTime);
            UltimateRemaining = Math.Max(0f, UltimateRemaining - safeDeltaTime);
        }

        public bool IsAttackReady => IsAssigned && AttackCooldownRemaining <= 0f;
        public bool IsSkillOneReady => IsAssigned && SkillOneCooldownRemaining <= 0f;
        public bool IsSkillTwoReady => IsAssigned && SkillTwoCooldownRemaining <= 0f;
        public bool IsUltimateReady => IsAssigned && UltimateEnergy >= Spirit.UltimateEnergyMaximum;

        public void BeginAttackCooldown(float duration)
        {
            AttackCooldownRemaining = Math.Max(0.01f, duration);
        }

        public void BeginSkillOne(float cooldownMultiplier = 1f)
        {
            SkillOneCooldownRemaining = Math.Max(0.01f, Spirit.SkillOne.Cooldown * cooldownMultiplier);
            SkillOneRemaining = Math.Max(0f, Spirit.SkillOne.Duration);
        }

        public void BeginSkillTwo(float cooldownMultiplier = 1f)
        {
            SkillTwoCooldownRemaining = Math.Max(0.01f, Spirit.SkillTwo.Cooldown * cooldownMultiplier);
            SkillTwoRemaining = Math.Max(0f, Spirit.SkillTwo.Duration);
        }

        public void GainUltimateEnergy(float amount)
        {
            UltimateEnergy = Math.Min(Spirit.UltimateEnergyMaximum, UltimateEnergy + Math.Max(0f, amount));
        }

        public void SpendUltimateEnergy()
        {
            UltimateEnergy = 0f;
            UltimateRemaining = Math.Max(0f, Spirit.Ultimate.Duration);
        }

        public void SetActing(bool isActing)
        {
            IsActing = isActing;
        }
    }
}
