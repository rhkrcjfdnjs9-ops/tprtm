using System;
using SpiritStone.Prototype;
using UnityEditor;

namespace SpiritStone.Editor
{
    public static class PrototypeCombatStatusValidation
    {
        [MenuItem("Tools/Spirit Stone/Validate Combat Status")]
        public static void Run()
        {
            PrototypeCombatStatusSystem system = new();
            system.Apply(PrototypeCombatStatusType.AttackPower, PrototypeCombatStatusTarget.Team,
                string.Empty, "team_buff", 0.35f, 5f);
            RequireApproximately(system.GetAttackPowerMultiplier("arca"), 1.35f, "Team attack buff");

            system.Apply(PrototypeCombatStatusType.AttackSpeed, PrototypeCombatStatusTarget.Spirit,
                "arca", "speed", 0.25f, 5f);
            RequireApproximately(system.GetAttackIntervalMultiplier("arca"), 0.8f, "Attack speed buff");
            RequireApproximately(system.GetAttackIntervalMultiplier("ignis"), 1f, "Spirit targeting");

            system.Apply(PrototypeCombatStatusType.Defense, PrototypeCombatStatusTarget.Team,
                string.Empty, "defense", 1f, 5f);
            RequireApproximately(system.GetIncomingDamageMultiplier(), 0.5f, "Defense buff");

            system.Apply(PrototypeCombatStatusType.Stun, PrototypeCombatStatusTarget.Enemy,
                string.Empty, "shock", 1f, 0.5f);
            Require(system.IsEnemyStunned, "Stun was not applied.");
            system.Tick(0.6f);
            Require(!system.IsEnemyStunned, "Expired stun was not removed.");

            system.Apply(PrototypeCombatStatusType.Burn, PrototypeCombatStatusTarget.Enemy,
                string.Empty, "burn", 10f, 4f, 3);
            system.Apply(PrototypeCombatStatusType.Burn, PrototypeCombatStatusTarget.Enemy,
                string.Empty, "burn", 10f, 4f, 3);
            RequireApproximately(system.Tick(1f), 20f, "Stacked burn tick");
            system.Clear();
            Require(system.GetSummary() == "상태 효과 없음", "Clear did not remove all statuses.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void RequireApproximately(float actual, float expected, string label)
        {
            if (Math.Abs(actual - expected) > 0.001f)
                throw new InvalidOperationException($"{label}: expected {expected}, actual {actual}");
        }
    }
}
