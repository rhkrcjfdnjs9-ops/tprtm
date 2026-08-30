using System;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class PrototypeCoreFlowValidation
    {
        [MenuItem("Tools/Prototype/Validate Core Flow")]
        public static void Run()
        {
            PrototypeSaveData initial = new() { SpiritStones = 1000, IsOwnershipInitialized = true };
            initial.OwnedSpiritIds.Add("arca");
            initial.FormationSpiritIds.Add("arca");
            initial.FormationSpiritIds.Add(string.Empty);
            initial.FormationSpiritIds.Add(string.Empty);

            PrototypeSummonSystem summons = new();
            summons.Initialize(initial);
            Require(summons.CanSummonTen && summons.TrySpendSummonCost(10) && summons.SpiritStones == 0,
                "Ten summons must spend exactly 1,000 spirit stones in one transaction.");
            Require(summons.IsOwned("arca"), "Arca must be owned at initialization.");
            Require(!summons.IsOwned("ignis"), "Ignis must begin locked in this validation save.");

            PrototypeSpiritData ignis = PrototypeSpiritCatalog.GetRequired("ignis");
            Require(summons.RegisterSummon(ignis, out bool converted) && !converted,
                "The first Ignis summon must unlock the spirit without granting a shard.");
            Require(summons.GetShards("ignis") == 0 && summons.GetBreakthrough("ignis") == 0,
                "A newly unlocked spirit must begin at breakthrough zero with no shard.");

            Require(!summons.RegisterSummon(ignis, out converted) && !converted && summons.GetShards("ignis") == 1,
                "A duplicate before maximum breakthrough must grant one dedicated shard.");
            Require(summons.Breakthrough("ignis") == 1 && summons.GetShards("ignis") == 0,
                "Breakthrough must consume exactly one dedicated shard.");

            for (int level = 1; level < PrototypeSummonSystem.MaximumBreakthrough; level++)
            {
                summons.RegisterSummon(ignis, out converted);
                Require(summons.Breakthrough("ignis") == level + 1, "Ignis must advance one breakthrough per shard.");
            }
            Require(summons.GetBreakthrough("ignis") == PrototypeSummonSystem.MaximumBreakthrough,
                "Ignis must stop at the configured maximum breakthrough.");
            summons.RegisterSummon(ignis, out converted);
            Require(converted && summons.SsrCommonShards == 1,
                "A duplicate after maximum breakthrough must become one common shard.");

            PrototypeSpiritData windy = PrototypeSpiritCatalog.GetRequired("windy");
            Require(summons.RegisterSummon(windy, out converted), "The first Windy summon must unlock Windy.");
            summons.RegisterSummon(ignis, out converted);
            Require(summons.Exchange("windy") && summons.SsrCommonShards == 0 && summons.GetShards("windy") == 1,
                "Two common shards must exchange for one Windy shard.");

            PrototypeFormationSystem formation = new();
            formation.Initialize(initial, summons);
            Require(formation.Assign(1, "ignis", summons), "Ignis must be assignable to slot two.");
            Require(formation.Assign(1, "windy", summons), "Windy must replace Ignis in slot two.");
            Require(formation.Slots[1].SpiritId == "windy" && formation.Slots[1].Spirit.Element == SpiritElement.Wind,
                "The runtime slot must retain Windy's identity and wind element.");

            for (int index = 0; index < 105; index++) summons.RegisterSummon(windy, out converted);
            Require(summons.SummonHistoryIds.Count == PrototypeSummonSystem.MaximumHistoryCount,
                "Summon history must retain only the most recent 100 results.");

            PrototypeSaveData roundTrip = new();
            formation.WriteTo(roundTrip);
            summons.WriteTo(roundTrip);
            PrototypeSummonSystem restoredSummons = new();
            restoredSummons.Initialize(roundTrip);
            PrototypeFormationSystem restoredFormation = new();
            restoredFormation.Initialize(roundTrip, restoredSummons);
            Require(restoredFormation.Slots[1].SpiritId == "windy",
                "Saving and restoring must preserve Windy in slot two.");
            Require(restoredSummons.GetBreakthrough("ignis") == PrototypeSummonSystem.MaximumBreakthrough,
                "Saving and restoring must preserve breakthrough progress.");
            Require(restoredSummons.SummonHistoryIds.Count == PrototypeSummonSystem.MaximumHistoryCount,
                "Saving and restoring must preserve the capped summon history.");
            Require(restoredFormation.Unassign(1), "An assigned, idle formation slot must be removable.");
            Require(!restoredFormation.Slots[1].IsAssigned && string.IsNullOrEmpty(restoredFormation.Slots[1].SpiritId),
                "Removing a spirit must leave the formation slot empty.");

            Debug.LogFormat("[PrototypeCoreFlowValidation] PASS: summon, shards, breakthrough, exchange, formation, and data round-trip are valid.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"[PrototypeCoreFlowValidation] {message}");
        }
    }
}
