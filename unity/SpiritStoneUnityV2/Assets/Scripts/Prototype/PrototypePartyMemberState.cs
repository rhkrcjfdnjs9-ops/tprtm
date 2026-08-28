using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypePartyMemberState
    {
        public PrototypePartyMemberState(string id, string displayName, SpriteRenderer renderer, float maximumHealth, float defense, float targetWeight)
        {
            Id = id;
            DisplayName = displayName;
            Renderer = renderer;
            MaximumHealth = Mathf.Max(1f, maximumHealth);
            CurrentHealth = MaximumHealth;
            Defense = Mathf.Max(0f, defense);
            TargetWeight = Mathf.Max(0.01f, targetWeight);
            IsActive = true;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public SpriteRenderer Renderer { get; }
        public Transform Visual => Renderer != null ? Renderer.transform : null;
        public float CurrentHealth { get; private set; }
        public float MaximumHealth { get; private set; }
        public float Defense { get; private set; }
        public float TargetWeight { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsAlive => IsActive && CurrentHealth > 0f;

        public float CalculateDamage(float rawDamage)
        {
            return Mathf.Max(0f, rawDamage) * 100f / (100f + Defense);
        }

        public void TakeDamage(float damage)
        {
            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, damage));
            UpdateVisibility();
        }

        public void UpdateStats(float maximumHealth, float defense, float targetWeight, bool refillHealth)
        {
            IsActive = true;
            float previousRatio = MaximumHealth > 0f ? CurrentHealth / MaximumHealth : 0f;
            MaximumHealth = Mathf.Max(1f, maximumHealth);
            Defense = Mathf.Max(0f, defense);
            TargetWeight = Mathf.Max(0.01f, targetWeight);
            CurrentHealth = refillHealth ? MaximumHealth : Mathf.Clamp(previousRatio * MaximumHealth, 0f, MaximumHealth);
            UpdateVisibility();
        }

        public void Deactivate()
        {
            IsActive = false;
            CurrentHealth = 0f;
            UpdateVisibility();
        }

        public void Revive(float healthRatio)
        {
            CurrentHealth = MaximumHealth * Mathf.Clamp(healthRatio, 0.01f, 1f);
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (Renderer != null) Renderer.enabled = IsAlive;
        }
    }
}
