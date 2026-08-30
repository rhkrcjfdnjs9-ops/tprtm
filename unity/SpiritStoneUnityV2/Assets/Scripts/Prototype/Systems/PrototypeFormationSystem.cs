using System;
using System.Collections.Generic;

namespace SpiritStone.Prototype
{
    public sealed class PrototypeFormationSystem
    {
        public PrototypeSpiritSlot[] Slots { get; private set; }

        public void Initialize(PrototypeSaveData saveData, PrototypeSummonSystem summonSystem)
        {
            Slots = new[] { new PrototypeSpiritSlot(0), new PrototypeSpiritSlot(1), new PrototypeSpiritSlot(2) };
            HashSet<string> assignedIds = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < Slots.Length; index++)
            {
                string savedId = index < saveData.FormationSpiritIds.Count ? saveData.FormationSpiritIds[index] : string.Empty;
                if (!IsValidUnassigned(savedId, assignedIds, summonSystem)) continue;
                assignedIds.Add(savedId);
                Slots[index].Assign(PrototypeSpiritCatalog.GetRequired(savedId), InitialCooldown(index));
            }
            if (saveData.FormationSpiritIds.Count == 0 && assignedIds.Count == 0 && summonSystem.IsOwned("arca"))
                Slots[0].Assign(PrototypeSpiritCatalog.GetRequired("arca"), InitialCooldown(0));
        }

        public PrototypeSpiritData Cycle(int slotIndex, PrototypeSummonSystem summonSystem)
        {
            if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Length) return null;
            List<PrototypeSpiritData> ownedSpirits = summonSystem.GetOwnedSpirits();
            if (ownedSpirits.Count == 0) return null;

            int currentIndex = ownedSpirits.FindIndex(spirit => spirit.Id.Equals(Slots[slotIndex].SpiritId, StringComparison.OrdinalIgnoreCase));
            PrototypeSpiritData nextSpirit = null;
            for (int offset = 1; offset <= ownedSpirits.Count; offset++)
            {
                PrototypeSpiritData candidate = ownedSpirits[(currentIndex + offset + ownedSpirits.Count) % ownedSpirits.Count];
                bool assignedElsewhere = Array.Exists(Slots, slot => slot != Slots[slotIndex] && slot.SpiritId.Equals(candidate.Id, StringComparison.OrdinalIgnoreCase));
                if (!assignedElsewhere || Slots[slotIndex].IsAssigned)
                {
                    nextSpirit = candidate;
                    break;
                }
            }
            if (nextSpirit == null || Slots[slotIndex].SpiritId.Equals(nextSpirit.Id, StringComparison.OrdinalIgnoreCase)) return null;

            int occupiedIndex = Array.FindIndex(Slots, slot => slot.SpiritId.Equals(nextSpirit.Id, StringComparison.OrdinalIgnoreCase));
            PrototypeSpiritData previousSpirit = Slots[slotIndex].Spirit;
            Slots[slotIndex].Assign(nextSpirit, InitialCooldown(slotIndex));
            if (occupiedIndex >= 0 && occupiedIndex != slotIndex && previousSpirit != null)
                Slots[occupiedIndex].Assign(previousSpirit, InitialCooldown(occupiedIndex));
            else if (occupiedIndex >= 0 && occupiedIndex != slotIndex)
                Slots[occupiedIndex].Clear();
            return nextSpirit;
        }

        public bool Assign(int slotIndex, string spiritId, PrototypeSummonSystem summonSystem)
        {
            if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Length || !summonSystem.IsOwned(spiritId)) return false;
            PrototypeSpiritData nextSpirit;
            try
            {
                nextSpirit = PrototypeSpiritCatalog.GetRequired(spiritId);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (Slots[slotIndex].SpiritId.Equals(spiritId, StringComparison.OrdinalIgnoreCase)) return false;

            int occupiedIndex = Array.FindIndex(Slots, slot => slot.SpiritId.Equals(spiritId, StringComparison.OrdinalIgnoreCase));
            PrototypeSpiritData previousSpirit = Slots[slotIndex].Spirit;
            Slots[slotIndex].Assign(nextSpirit, InitialCooldown(slotIndex));
            if (occupiedIndex >= 0 && occupiedIndex != slotIndex)
            {
                if (previousSpirit != null) Slots[occupiedIndex].Assign(previousSpirit, InitialCooldown(occupiedIndex));
                else Slots[occupiedIndex].Clear();
            }
            return true;
        }

        public bool Unassign(int slotIndex)
        {
            if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Length) return false;
            PrototypeSpiritSlot slot = Slots[slotIndex];
            if (!slot.IsAssigned || slot.IsActing) return false;
            slot.Clear();
            return true;
        }

        public void WriteTo(PrototypeSaveData saveData)
        {
            if (Slots == null) return;
            foreach (PrototypeSpiritSlot slot in Slots) saveData.FormationSpiritIds.Add(slot.SpiritId);
        }

        private static float InitialCooldown(int slotIndex) => 0.4f + slotIndex * 0.15f;

        private static bool IsValidUnassigned(string spiritId, HashSet<string> assignedIds, PrototypeSummonSystem summonSystem)
        {
            if (string.IsNullOrWhiteSpace(spiritId) || assignedIds.Contains(spiritId) || !summonSystem.IsOwned(spiritId)) return false;
            try
            {
                PrototypeSpiritCatalog.GetRequired(spiritId);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
