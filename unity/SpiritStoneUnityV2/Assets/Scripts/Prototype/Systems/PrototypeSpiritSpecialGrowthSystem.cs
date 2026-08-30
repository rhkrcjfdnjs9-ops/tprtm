using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public enum PrototypeSpiritSpecialGrowthType
    {
        SkillPower,
        CooldownReduction
    }

    public sealed class PrototypeSpiritSpecialGrowthSystem
    {
        public const int MaximumLevel = 10;
        private readonly Dictionary<string, PrototypeSpiritSpecialGrowthData> growthBySpirit = new(StringComparer.OrdinalIgnoreCase);

        public void Initialize(PrototypeSaveData data)
        {
            growthBySpirit.Clear();
            foreach (PrototypeSpiritSpecialGrowthData growth in data.SpiritSpecialGrowth)
            {
                if (growth == null || string.IsNullOrWhiteSpace(growth.SpiritId)) continue;
                growthBySpirit[growth.SpiritId] = new PrototypeSpiritSpecialGrowthData
                {
                    SpiritId = growth.SpiritId,
                    SkillPowerLevel = Mathf.Clamp(growth.SkillPowerLevel, 0, MaximumLevel),
                    CooldownReductionLevel = Mathf.Clamp(growth.CooldownReductionLevel, 0, MaximumLevel)
                };
            }
        }

        public int GetLevel(string spiritId, PrototypeSpiritSpecialGrowthType type)
        {
            if (!growthBySpirit.TryGetValue(spiritId, out PrototypeSpiritSpecialGrowthData growth)) return 0;
            return type == PrototypeSpiritSpecialGrowthType.SkillPower ? growth.SkillPowerLevel : growth.CooldownReductionLevel;
        }

        public float GetSkillPowerMultiplier(string spiritId) =>
            1f + GetLevel(spiritId, PrototypeSpiritSpecialGrowthType.SkillPower) * 0.08f;

        public float GetCooldownMultiplier(string spiritId) =>
            Mathf.Max(0.7f, 1f - GetLevel(spiritId, PrototypeSpiritSpecialGrowthType.CooldownReduction) * 0.03f);

        public bool CanUpgrade(string spiritId, PrototypeSpiritSpecialGrowthType type) =>
            !string.IsNullOrWhiteSpace(spiritId) && GetLevel(spiritId, type) < MaximumLevel;

        public bool Upgrade(string spiritId, PrototypeSpiritSpecialGrowthType type)
        {
            if (!CanUpgrade(spiritId, type)) return false;
            if (!growthBySpirit.TryGetValue(spiritId, out PrototypeSpiritSpecialGrowthData growth))
            {
                growth = new PrototypeSpiritSpecialGrowthData { SpiritId = spiritId };
                growthBySpirit.Add(spiritId, growth);
            }
            if (type == PrototypeSpiritSpecialGrowthType.SkillPower) growth.SkillPowerLevel++;
            else growth.CooldownReductionLevel++;
            return true;
        }

        public void WriteTo(PrototypeSaveData data)
        {
            foreach (PrototypeSpiritSpecialGrowthData growth in growthBySpirit.Values)
                data.SpiritSpecialGrowth.Add(new PrototypeSpiritSpecialGrowthData
                {
                    SpiritId = growth.SpiritId,
                    SkillPowerLevel = growth.SkillPowerLevel,
                    CooldownReductionLevel = growth.CooldownReductionLevel
                });
        }
    }
}
