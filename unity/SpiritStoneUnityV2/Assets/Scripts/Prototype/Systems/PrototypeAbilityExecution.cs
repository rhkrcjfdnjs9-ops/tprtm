namespace SpiritStone.Prototype
{
    public readonly struct PrototypeAbilityExecution
    {
        public PrototypeAbilityExecution(PrototypeSpiritAbilityData ability, SpiritAbilitySlot slot, float damage, float shield)
        {
            Ability = ability;
            Slot = slot;
            Damage = damage;
            Shield = shield;
        }

        public PrototypeSpiritAbilityData Ability { get; }
        public SpiritAbilitySlot Slot { get; }
        public float Damage { get; }
        public float Shield { get; }
        public float EnergyGain => Ability.EnergyGain;
        public SpiritAbilityEffect Effect => Ability.Effect;
        public bool DealsDamage => Damage > 0f;
        public bool GrantsShield => Shield > 0f;
    }
}
