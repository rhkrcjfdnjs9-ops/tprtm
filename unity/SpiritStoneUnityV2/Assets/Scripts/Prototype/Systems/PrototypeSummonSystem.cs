using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeSummonSystem
    {
        public const int SummonCost = 100;
        public const int MaximumBreakthrough = 6;
        public const int CommonShardExchangeCost = 2;

        private readonly HashSet<string> ownedSpiritIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> spiritShards = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> spiritBreakthroughs = new(StringComparer.OrdinalIgnoreCase);

        public int SpiritStones { get; private set; }
        public int SsrCommonShards { get; private set; }
        public bool CanSummon => SpiritStones >= SummonCost;

        public void Initialize(PrototypeSaveData saveData)
        {
            SpiritStones = Mathf.Max(0, saveData.SpiritStones);
            SsrCommonShards = Mathf.Max(0, saveData.SsrCommonShards);
            ownedSpiritIds.Clear();
            spiritShards.Clear();
            spiritBreakthroughs.Clear();

            foreach (string spiritId in saveData.OwnedSpiritIds)
                if (!string.IsNullOrWhiteSpace(spiritId)) ownedSpiritIds.Add(spiritId);
            ownedSpiritIds.Add("arca");

            foreach (PrototypeSpiritShardData shard in saveData.SpiritShards)
                if (shard != null && !string.IsNullOrWhiteSpace(shard.SpiritId))
                    spiritShards[shard.SpiritId] = Mathf.Max(0, shard.Amount);

            foreach (PrototypeSpiritBreakthroughData breakthrough in saveData.SpiritBreakthroughs)
                if (breakthrough != null && !string.IsNullOrWhiteSpace(breakthrough.SpiritId))
                    spiritBreakthroughs[breakthrough.SpiritId] = Mathf.Clamp(breakthrough.Level, 0, MaximumBreakthrough);
        }

        public bool IsOwned(string spiritId) => !string.IsNullOrWhiteSpace(spiritId) && ownedSpiritIds.Contains(spiritId);
        public int GetShards(string spiritId) => spiritShards.TryGetValue(spiritId, out int amount) ? amount : 0;
        public int GetBreakthrough(string spiritId) => spiritBreakthroughs.TryGetValue(spiritId, out int level) ? level : 0;

        public bool TrySpendSummonCost()
        {
            if (!CanSummon) return false;
            SpiritStones -= SummonCost;
            return true;
        }

        public bool RegisterSummon(PrototypeSpiritData spirit, out bool convertedToCommonShard)
        {
            convertedToCommonShard = false;
            if (ownedSpiritIds.Add(spirit.Id)) return true;
            if (GetBreakthrough(spirit.Id) >= MaximumBreakthrough)
            {
                SsrCommonShards++;
                convertedToCommonShard = true;
            }
            else
            {
                spiritShards[spirit.Id] = GetShards(spirit.Id) + 1;
            }
            return false;
        }

        public bool CanBreakthrough(string spiritId) => IsOwned(spiritId) && GetBreakthrough(spiritId) < MaximumBreakthrough && GetShards(spiritId) > 0;

        public int Breakthrough(string spiritId)
        {
            if (!CanBreakthrough(spiritId)) return GetBreakthrough(spiritId);
            spiritShards[spiritId] = GetShards(spiritId) - 1;
            int level = GetBreakthrough(spiritId) + 1;
            spiritBreakthroughs[spiritId] = level;
            return level;
        }

        public bool CanExchange(string spiritId) => IsOwned(spiritId) && GetBreakthrough(spiritId) < MaximumBreakthrough && SsrCommonShards >= CommonShardExchangeCost;

        public bool Exchange(string spiritId)
        {
            if (!CanExchange(spiritId)) return false;
            SsrCommonShards -= CommonShardExchangeCost;
            spiritShards[spiritId] = GetShards(spiritId) + 1;
            return true;
        }

        public void AddSpiritStones(int amount) => SpiritStones = Mathf.Max(0, SpiritStones + amount);

        public List<PrototypeSpiritData> GetOwnedSpirits()
        {
            List<PrototypeSpiritData> spirits = new();
            foreach (PrototypeSpiritData spirit in PrototypeSpiritCatalog.GetAll())
                if (ownedSpiritIds.Contains(spirit.Id)) spirits.Add(spirit);
            return spirits;
        }

        public void WriteTo(PrototypeSaveData saveData)
        {
            saveData.SpiritStones = SpiritStones;
            saveData.SsrCommonShards = SsrCommonShards;
            saveData.IsOwnershipInitialized = true;
            foreach (string spiritId in ownedSpiritIds) saveData.OwnedSpiritIds.Add(spiritId);
            foreach (KeyValuePair<string, int> shard in spiritShards)
                saveData.SpiritShards.Add(new PrototypeSpiritShardData { SpiritId = shard.Key, Amount = shard.Value });
            foreach (KeyValuePair<string, int> breakthrough in spiritBreakthroughs)
                saveData.SpiritBreakthroughs.Add(new PrototypeSpiritBreakthroughData { SpiritId = breakthrough.Key, Level = breakthrough.Value });
        }
    }
}
