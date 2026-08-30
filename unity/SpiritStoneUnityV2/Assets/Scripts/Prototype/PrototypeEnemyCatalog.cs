using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpiritStone.Prototype
{
    public static class PrototypeEnemyCatalog
    {
        private static PrototypeEnemyDefinition[] normalEnemies;
        private static PrototypeEnemyDefinition boss;

        public static PrototypeEnemyDefinition GetNormalForWave(int wave)
        {
            EnsureLoaded();
            return normalEnemies[(Mathf.Max(1, wave) - 1) % normalEnemies.Length];
        }

        public static PrototypeEnemyDefinition GetNormal(PrototypeEnemyArchetype archetype)
        {
            EnsureLoaded();
            return normalEnemies.FirstOrDefault(enemy => enemy.Archetype == archetype) ?? normalEnemies[0];
        }

        public static PrototypeEnemyDefinition GetBoss()
        {
            EnsureLoaded();
            return boss;
        }

        public static IReadOnlyList<PrototypeEnemyDefinition> GetAll()
        {
            EnsureLoaded();
            return normalEnemies.Concat(new[] { boss }).ToArray();
        }

        public static void Reload()
        {
            normalEnemies = null;
            boss = null;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (normalEnemies != null && boss != null) return;
            PrototypeEnemyDefinition[] definitions = Resources.LoadAll<PrototypeEnemyDefinition>("Prototype/Enemies");
            if (definitions.Length == 0)
                throw new InvalidOperationException("No enemy definition assets were found in Resources/Prototype/Enemies.");

            HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
            foreach (PrototypeEnemyDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                    throw new InvalidOperationException("Enemy definition contains an empty id.");
                if (!ids.Add(definition.Id)) throw new InvalidOperationException($"Duplicate enemy definition id: {definition.Id}");
            }

            normalEnemies = definitions.Where(definition => !definition.IsBoss)
                .OrderBy(definition => definition.RotationOrder).ToArray();
            PrototypeEnemyDefinition[] bosses = definitions.Where(definition => definition.IsBoss).ToArray();
            if (normalEnemies.Length == 0) throw new InvalidOperationException("At least one normal enemy definition is required.");
            if (bosses.Length != 1) throw new InvalidOperationException("Exactly one boss definition is required.");
            boss = bosses[0];
        }
    }
}
