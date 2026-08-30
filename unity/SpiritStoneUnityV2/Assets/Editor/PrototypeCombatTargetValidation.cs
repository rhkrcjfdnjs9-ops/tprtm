using System;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class PrototypeCombatTargetValidation
    {
        [MenuItem("Tools/Prototype/Validate Combat Targets")]
        public static void Run()
        {
            PrototypeCombatTargetSystem targets = new();
            targets.BeginEncounter(300f, 3);
            Require(targets.Count == 3, "The encounter must create three targets.");
            Require(targets.AliveCount == 3, "All targets must begin alive.");
            Require(targets.PrimaryTargetIndex == 0, "The first target must be selected initially.");

            float firstHit = targets.ApplyDamage(110f, 1);
            Require(Mathf.Approximately(firstHit, 110f), "Single-target damage must apply fully.");
            Require(!targets.IsAlive(0), "The first target must be defeated.");
            Require(targets.PrimaryTargetIndex == 1, "Target selection must advance to the second target.");
            Require(Mathf.Approximately(targets.GetHealth(1), 90f), "Overkill damage must carry into the next living target.");

            float chainHit = targets.ApplyDamage(90f, 3);
            Require(Mathf.Approximately(chainHit, 90f), "Chain damage must apply fully to living targets.");
            Require(Mathf.Approximately(targets.GetHealth(1), 45f), "Chain damage must split across living targets.");
            Require(Mathf.Approximately(targets.GetHealth(2), 55f), "Chain damage must split across living targets.");

            targets.ApplyDamage(50f, 1);
            Require(!targets.IsAlive(1), "The second target must be defeated.");
            Require(targets.PrimaryTargetIndex == 2, "Target selection must advance to the third target.");
            Require(targets.AliveCount == 1, "Exactly one target must remain alive.");
            Require(Mathf.Approximately(targets.TotalHealth, 50f), "Aggregate health must equal the final target health.");

            Debug.LogFormat("[PrototypeCombatTargetValidation] PASS: defeated targets are removed and primary target advanced 1 -> 2 -> 3.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"[PrototypeCombatTargetValidation] {message}");
        }
    }
}
