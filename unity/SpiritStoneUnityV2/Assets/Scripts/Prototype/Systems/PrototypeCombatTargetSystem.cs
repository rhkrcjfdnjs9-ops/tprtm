using System;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeCombatTargetSystem
    {
        private float[] health = Array.Empty<float>();
        private float[] maximumHealth = Array.Empty<float>();

        public int Count => health.Length;
        public int AliveCount { get; private set; }
        public int PrimaryTargetIndex
        {
            get
            {
                for (int index = 0; index < health.Length; index++)
                    if (health[index] > 0f) return index;
                return -1;
            }
        }

        public void BeginEncounter(float totalHealth, int targetCount)
        {
            int count = Mathf.Max(1, targetCount);
            health = new float[count];
            maximumHealth = new float[count];
            float healthPerTarget = Mathf.Max(1f, totalHealth) / count;
            for (int index = 0; index < count; index++) health[index] = maximumHealth[index] = healthPerTarget;
            AliveCount = count;
        }

        public float TotalHealth
        {
            get
            {
                float total = 0f;
                for (int index = 0; index < health.Length; index++) total += health[index];
                return total;
            }
        }

        public float ApplyDamage(float totalDamage, int maximumTargets)
        {
            float remainingDamage = Mathf.Max(0f, totalDamage);
            float appliedDamage = 0f;
            int targetsRemaining = Mathf.Clamp(maximumTargets, 1, Mathf.Max(1, AliveCount));
            int searchIndex = 0;
            while (remainingDamage > 0f && targetsRemaining > 0)
            {
                int targetIndex = FindAliveFrom(searchIndex);
                if (targetIndex < 0) break;
                float share = remainingDamage / targetsRemaining;
                float applied = Mathf.Min(health[targetIndex], share);
                health[targetIndex] -= applied;
                remainingDamage -= applied;
                appliedDamage += applied;
                targetsRemaining--;
                searchIndex = targetIndex + 1;
                if (health[targetIndex] <= 0f) AliveCount--;
            }
            while (remainingDamage > 0f)
            {
                int targetIndex = PrimaryTargetIndex;
                if (targetIndex < 0) break;
                float applied = Mathf.Min(health[targetIndex], remainingDamage);
                health[targetIndex] -= applied;
                remainingDamage -= applied;
                appliedDamage += applied;
                if (health[targetIndex] <= 0f) AliveCount--;
            }
            return appliedDamage;
        }

        public bool IsAlive(int index) => index >= 0 && index < health.Length && health[index] > 0f;
        public float GetHealth(int index) => index >= 0 && index < health.Length ? health[index] : 0f;
        public float GetMaximumHealth(int index) => index >= 0 && index < maximumHealth.Length ? maximumHealth[index] : 0f;

        private int FindAliveFrom(int startIndex)
        {
            for (int index = Mathf.Max(0, startIndex); index < health.Length; index++)
                if (health[index] > 0f) return index;
            return -1;
        }
    }
}
