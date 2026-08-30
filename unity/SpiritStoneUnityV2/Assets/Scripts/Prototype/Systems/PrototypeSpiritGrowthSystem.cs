using System;
using System.Collections.Generic;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeSpiritGrowthSystem
    {
        private readonly Dictionary<string, PrototypeSpiritProgress> progressById = new(StringComparer.OrdinalIgnoreCase);

        public void Initialize(PrototypeSaveData saveData)
        {
            progressById.Clear();
            foreach (PrototypeSpiritData spirit in PrototypeSpiritCatalog.GetAll())
            {
                PrototypeSpiritProgressData saved = saveData.SpiritProgress.Find(item =>
                    item != null && item.SpiritId.Equals(spirit.Id, StringComparison.OrdinalIgnoreCase));
                progressById.Add(spirit.Id, new PrototypeSpiritProgress(spirit.Id, saved?.Level ?? 1, saved?.Experience ?? 0));
            }
        }

        public PrototypeSpiritProgress Get(string spiritId)
        {
            return !string.IsNullOrWhiteSpace(spiritId) && progressById.TryGetValue(spiritId, out PrototypeSpiritProgress progress)
                ? progress
                : null;
        }

        public void Reset(string spiritId)
        {
            Get(spiritId)?.Reset();
        }

        public void WriteTo(PrototypeSaveData saveData)
        {
            foreach (PrototypeSpiritProgress progress in progressById.Values)
                saveData.SpiritProgress.Add(progress.CreateSaveData());
        }
    }
}
