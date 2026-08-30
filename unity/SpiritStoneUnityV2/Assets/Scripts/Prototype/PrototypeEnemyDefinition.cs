using UnityEngine;

namespace SpiritStone.Prototype
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Spirit Stone/Enemy Definition")]
    public sealed class PrototypeEnemyDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private PrototypeEnemyArchetype archetype;
        [SerializeField] private bool isBoss;
        [SerializeField, Min(0)] private int rotationOrder;
        [SerializeField, Min(0.01f)] private float healthMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1.5f;
        [SerializeField, Min(0f)] private float rewardMultiplier = 1f;
        [SerializeField] private Color displayColor = Color.white;

        public string Id => id;
        public string DisplayName => displayName;
        public PrototypeEnemyArchetype Archetype => archetype;
        public bool IsBoss => isBoss;
        public int RotationOrder => rotationOrder;
        public float HealthMultiplier => healthMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public float AttackInterval => attackInterval;
        public float RewardMultiplier => rewardMultiplier;

        public PrototypeEnemyData CreateRuntimeData(float baseHealth, float baseDamage, int baseGold,
            int baseExperience, SpiritElement element, string overrideDisplayName = null)
        {
            string runtimeName = string.IsNullOrWhiteSpace(overrideDisplayName) ? displayName : overrideDisplayName;
            return new PrototypeEnemyData(runtimeName, baseHealth * healthMultiplier, baseDamage * damageMultiplier,
                attackInterval, Mathf.RoundToInt(baseGold * rewardMultiplier),
                Mathf.RoundToInt(baseExperience * rewardMultiplier), element, displayColor, isBoss, archetype);
        }

        public void Configure(string enemyId, string enemyName, PrototypeEnemyArchetype enemyArchetype,
            bool boss, int order, float health, float damage, float interval, float reward, Color color)
        {
            id = enemyId;
            displayName = enemyName;
            archetype = enemyArchetype;
            isBoss = boss;
            rotationOrder = Mathf.Max(0, order);
            healthMultiplier = Mathf.Max(0.01f, health);
            damageMultiplier = Mathf.Max(0.01f, damage);
            attackInterval = Mathf.Max(0.1f, interval);
            rewardMultiplier = Mathf.Max(0f, reward);
            displayColor = color;
        }
    }
}
