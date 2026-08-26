using UnityEngine;

namespace RealStone
{
    [CreateAssetMenu(menuName = "Real Stone/Battle Settings", fileName = "BattleSettings")]
    public sealed class BattleSettings : ScriptableObject
    {
        public int heroMaxHp = 120;
        public int enemyBaseHp = 120;
        public int enemyHpPerStage = 12;
        public int[] comboDamage = { 32, 40, 48 };
        public int enemyDamage = 18;
        public float approachSeconds = 0.5f;
        public float nextWaveRunSeconds = 0.75f;
        public float timeBetweenHits = 0.08f;
        public float lightHitStop = 0.035f;
        public float heavyHitStop = 0.075f;
        public float lightShake = 0.07f;
        public float heavyShake = 0.14f;

        public int DamageForCombo(int index)
        {
            if (comboDamage == null || comboDamage.Length == 0) return 30;
            return comboDamage[Mathf.Clamp(index, 0, comboDamage.Length - 1)];
        }
    }
}
