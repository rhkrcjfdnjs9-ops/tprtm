using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public enum PrototypeCombatStatusType
    {
        AttackPower,
        AttackSpeed,
        Defense,
        Burn,
        Stun
    }

    public enum PrototypeCombatStatusTarget
    {
        Team,
        Spirit,
        Enemy
    }

    public sealed class PrototypeCombatStatusSystem
    {
        private sealed class Status
        {
            public PrototypeCombatStatusType Type;
            public PrototypeCombatStatusTarget Target;
            public string TargetId;
            public string SourceId;
            public float Value;
            public float Remaining;
            public int Stacks;
            public int MaximumStacks;
        }

        private readonly List<Status> statuses = new();
        private float burnTickTimer;

        public void Clear()
        {
            statuses.Clear();
            burnTickTimer = 0f;
        }

        public void Apply(PrototypeCombatStatusType type, PrototypeCombatStatusTarget target, string targetId,
            string sourceId, float value, float duration, int maximumStacks = 1)
        {
            string safeTargetId = targetId ?? string.Empty;
            string safeSourceId = sourceId ?? string.Empty;
            Status status = statuses.Find(item => item.Type == type && item.Target == target
                && string.Equals(item.TargetId, safeTargetId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.SourceId, safeSourceId, StringComparison.OrdinalIgnoreCase));
            if (status == null)
            {
                status = new Status
                {
                    Type = type,
                    Target = target,
                    TargetId = safeTargetId,
                    SourceId = safeSourceId,
                    Value = value,
                    Stacks = 1,
                    MaximumStacks = Mathf.Max(1, maximumStacks)
                };
                statuses.Add(status);
            }
            else
            {
                status.Value = value;
                status.Stacks = Mathf.Min(status.MaximumStacks, status.Stacks + 1);
            }
            status.Remaining = Mathf.Max(status.Remaining, Mathf.Max(0.05f, duration));
        }

        public float Tick(float deltaTime)
        {
            float safeDelta = Mathf.Max(0f, deltaTime);
            for (int index = statuses.Count - 1; index >= 0; index--)
            {
                statuses[index].Remaining -= safeDelta;
                if (statuses[index].Remaining <= 0f) statuses.RemoveAt(index);
            }
            burnTickTimer += safeDelta;
            if (burnTickTimer < 1f) return 0f;
            burnTickTimer -= 1f;
            return statuses.Where(status => status.Target == PrototypeCombatStatusTarget.Enemy
                    && status.Type == PrototypeCombatStatusType.Burn)
                .Sum(status => status.Value * status.Stacks);
        }

        public bool IsEnemyStunned => statuses.Any(status => status.Target == PrototypeCombatStatusTarget.Enemy
            && status.Type == PrototypeCombatStatusType.Stun);

        public float GetAttackPowerMultiplier(string spiritId) => 1f + SumBonus(PrototypeCombatStatusType.AttackPower, spiritId);
        public float GetAttackIntervalMultiplier(string spiritId) => 1f / Mathf.Max(0.1f, 1f + SumBonus(PrototypeCombatStatusType.AttackSpeed, spiritId));
        public float GetIncomingDamageMultiplier() => 1f / Mathf.Max(0.1f, 1f + SumBonus(PrototypeCombatStatusType.Defense, string.Empty));
        public float GetEnemyAttackMultiplier() => Mathf.Clamp(1f + SumEnemyBonus(PrototypeCombatStatusType.AttackPower), 0.1f, 5f);

        public string GetSummary()
        {
            if (statuses.Count == 0) return "상태 효과 없음";
            return string.Join(" · ", statuses.GroupBy(status => status.Type).Select(group =>
            {
                Status status = group.First();
                int stacks = group.Sum(item => item.Stacks);
                return $"{GetDisplayName(group.Key)}{(stacks > 1 ? $" x{stacks}" : string.Empty)} {group.Max(item => item.Remaining):0.0}초";
            }));
        }

        private float SumBonus(PrototypeCombatStatusType type, string spiritId) => statuses
            .Where(status => status.Type == type && (status.Target == PrototypeCombatStatusTarget.Team
                || status.Target == PrototypeCombatStatusTarget.Spirit
                && string.Equals(status.TargetId, spiritId, StringComparison.OrdinalIgnoreCase)))
            .Sum(status => status.Value * status.Stacks);

        private float SumEnemyBonus(PrototypeCombatStatusType type) => statuses
            .Where(status => status.Type == type && status.Target == PrototypeCombatStatusTarget.Enemy)
            .Sum(status => status.Value * status.Stacks);

        private static string GetDisplayName(PrototypeCombatStatusType type)
        {
            return type switch
            {
                PrototypeCombatStatusType.AttackPower => "공격력",
                PrototypeCombatStatusType.AttackSpeed => "공격속도",
                PrototypeCombatStatusType.Defense => "방어",
                PrototypeCombatStatusType.Burn => "화상",
                PrototypeCombatStatusType.Stun => "감전",
                _ => type.ToString()
            };
        }
    }
}
