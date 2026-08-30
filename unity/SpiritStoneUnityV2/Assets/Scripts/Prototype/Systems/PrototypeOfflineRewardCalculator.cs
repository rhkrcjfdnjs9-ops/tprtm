using System;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public readonly struct PrototypeOfflineReward
    {
        public PrototypeOfflineReward(int completedMinutes, int gold, int experience)
        {
            CompletedMinutes = completedMinutes;
            Gold = gold;
            Experience = experience;
        }

        public int CompletedMinutes { get; }
        public int Gold { get; }
        public int Experience { get; }
        public bool HasReward => CompletedMinutes > 0;
    }

    public static class PrototypeOfflineRewardCalculator
    {
        public static PrototypeOfflineReward Calculate(DateTime utcNow, DateTime lastActiveUtc, int stage,
            float maximumOfflineHours, float efficiency)
        {
            double maximumMinutes = Mathf.Max(0f, maximumOfflineHours) * 60d;
            double elapsedMinutes = Math.Min(maximumMinutes,
                Math.Max(0d, (utcNow.ToUniversalTime() - lastActiveUtc.ToUniversalTime()).TotalMinutes));
            int completedMinutes = Mathf.FloorToInt((float)elapsedMinutes);
            if (completedMinutes < 1) return new PrototypeOfflineReward(0, 0, 0);

            int safeStage = Mathf.Max(1, stage);
            float safeEfficiency = Mathf.Clamp01(efficiency);
            int gold = Mathf.FloorToInt(completedMinutes * (5f + safeStage * 1.5f) * safeEfficiency);
            int experience = Mathf.FloorToInt(completedMinutes * (3f + safeStage) * safeEfficiency);
            return new PrototypeOfflineReward(completedMinutes, gold, experience);
        }
    }
}
