using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEngine;

public static class PrototypeDataAssetGenerator
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string PrototypeFolder = ResourcesFolder + "/Prototype";
    private const string SpiritsFolder = PrototypeFolder + "/Spirits";
    private const string EnemiesFolder = PrototypeFolder + "/Enemies";

    [MenuItem("Tools/Prototype/Rebuild Data Assets")]
    public static void RebuildDataAssets()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder(ResourcesFolder, "Prototype");
        EnsureFolder(PrototypeFolder, "Spirits");
        EnsureFolder(PrototypeFolder, "Enemies");

        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        foreach (PrototypeSpiritData spirit in PrototypeSpiritCatalog.GetBuiltInDefaults())
        {
            if (!ids.Add(spirit.Id)) throw new InvalidOperationException($"Duplicate spirit id: {spirit.Id}");
            string assetPath = $"{SpiritsFolder}/{spirit.Id}.asset";
            PrototypeSpiritDefinition definition = AssetDatabase.LoadAssetAtPath<PrototypeSpiritDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PrototypeSpiritDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }
            definition.Configure(spirit);
            EditorUtility.SetDirty(definition);
        }

        CreateDefaultEnemyAssets();

        string stagePath = $"{PrototypeFolder}/StageBalance.asset";
        PrototypeStageBalanceDefinition stageBalance = AssetDatabase.LoadAssetAtPath<PrototypeStageBalanceDefinition>(stagePath);
        if (stageBalance == null)
        {
            stageBalance = ScriptableObject.CreateInstance<PrototypeStageBalanceDefinition>();
            AssetDatabase.CreateAsset(stageBalance, stagePath);
        }
        EditorUtility.SetDirty(stageBalance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        PrototypeSpiritCatalog.Reload();
        PrototypeEnemyCatalog.Reload();
        Debug.LogFormat("[PrototypeDataAssetGenerator] Generated {0} spirit definitions and stage balance data.", ids.Count);
    }

    [MenuItem("Tools/Prototype/Rebuild Enemy Data Assets")]
    public static void RebuildEnemyDataAssets()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder(ResourcesFolder, "Prototype");
        EnsureFolder(PrototypeFolder, "Enemies");
        CreateDefaultEnemyAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        PrototypeEnemyCatalog.Reload();
        Debug.LogFormat("[PrototypeDataAssetGenerator] Generated 4 enemy definitions.");
    }

    private static void CreateDefaultEnemyAssets()
    {
        CreateEnemy("balanced", "일반 적 A · 균형형", PrototypeEnemyArchetype.Balanced, false, 0,
            1f, 1f, 1.5f, 1f, new Color(0.92f, 0.35f, 0.2f));
        CreateEnemy("heavy", "일반 적 B · 중장형", PrototypeEnemyArchetype.Heavy, false, 1,
            1.45f, 0.9f, 1.9f, 1.2f, new Color(0.62f, 0.3f, 0.18f));
        CreateEnemy("fast", "일반 적 C · 고속형", PrototypeEnemyArchetype.Fast, false, 2,
            0.75f, 1.15f, 1f, 1f, new Color(0.95f, 0.58f, 0.15f));
        CreateEnemy("stage_boss", "스테이지 보스", PrototypeEnemyArchetype.Boss, true, 0,
            5f, 1.8f, 1.35f, 2f, new Color(0.72f, 0.08f, 0.18f));
    }

    private static void CreateEnemy(string id, string displayName, PrototypeEnemyArchetype archetype, bool isBoss,
        int order, float health, float damage, float interval, float reward, Color color)
    {
        string assetPath = $"{EnemiesFolder}/{id}.asset";
        PrototypeEnemyDefinition definition = AssetDatabase.LoadAssetAtPath<PrototypeEnemyDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<PrototypeEnemyDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }
        definition.Configure(id, displayName, archetype, isBoss, order, health, damage, interval, reward, color);
        EditorUtility.SetDirty(definition);
    }

    [MenuItem("Tools/Prototype/Validate Data Assets")]
    public static void ValidateDataAssets()
    {
        PrototypeSpiritDefinition[] definitions = Resources.LoadAll<PrototypeSpiritDefinition>("Prototype/Spirits");
        if (definitions.Length == 0) throw new InvalidOperationException("No spirit definition assets were found.");
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < definitions.Length; index++)
        {
            PrototypeSpiritDefinition definition = definitions[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                throw new InvalidOperationException("Spirit definition contains an empty id.");
            if (!ids.Add(definition.Id)) throw new InvalidOperationException($"Duplicate spirit id: {definition.Id}");
            if (!Regex.IsMatch(definition.Id, "^[a-z0-9_]+$"))
                throw new InvalidOperationException($"Spirit id must use lowercase letters, numbers, or underscores: {definition.Id}");
            string assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(definition));
            if (!assetName.Equals(definition.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Spirit asset filename must match its id: {assetName} != {definition.Id}");
            ValidateSpirit(definition.CreateRuntimeData());
        }
        PrototypeEnemyDefinition[] enemyDefinitions = Resources.LoadAll<PrototypeEnemyDefinition>("Prototype/Enemies");
        if (enemyDefinitions.Length == 0) throw new InvalidOperationException("No enemy definition assets were found.");
        HashSet<string> enemyIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<int> normalRotationOrders = new();
        int bossCount = 0;
        for (int index = 0; index < enemyDefinitions.Length; index++)
        {
            PrototypeEnemyDefinition enemy = enemyDefinitions[index];
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.Id) || string.IsNullOrWhiteSpace(enemy.DisplayName))
                throw new InvalidOperationException("Every enemy definition requires an id and display name.");
            if (!enemyIds.Add(enemy.Id)) throw new InvalidOperationException($"Duplicate enemy id: {enemy.Id}");
            if (!Regex.IsMatch(enemy.Id, "^[a-z0-9_]+$"))
                throw new InvalidOperationException($"Enemy id must use lowercase letters, numbers, or underscores: {enemy.Id}");
            string enemyAssetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(enemy));
            if (!enemyAssetName.Equals(enemy.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Enemy asset filename must match its id: {enemyAssetName} != {enemy.Id}");
            if (enemy.HealthMultiplier <= 0f || enemy.DamageMultiplier <= 0f || enemy.AttackInterval <= 0f || enemy.RewardMultiplier < 0f)
                throw new InvalidOperationException($"{enemy.Id}: enemy combat values are invalid.");
            if (enemy.IsBoss)
            {
                bossCount++;
                if (enemy.Archetype != PrototypeEnemyArchetype.Boss)
                    throw new InvalidOperationException($"{enemy.Id}: a boss asset must use the Boss archetype.");
            }
            else if (!normalRotationOrders.Add(enemy.RotationOrder))
            {
                throw new InvalidOperationException($"Duplicate normal enemy rotation order: {enemy.RotationOrder}");
            }
        }
        if (bossCount != 1) throw new InvalidOperationException("Exactly one boss definition is required.");
        PrototypeEnemyCatalog.Reload();
        if (Resources.Load<PrototypeStageBalanceDefinition>("Prototype/StageBalance") == null)
            throw new InvalidOperationException("StageBalance asset is missing.");
        Debug.LogFormat("[PrototypeDataAssetGenerator] Validation passed for {0} spirits and {1} enemies.", ids.Count, enemyIds.Count);
    }

    private static void ValidateSpirit(PrototypeSpiritData spirit)
    {
        if (!Enum.IsDefined(typeof(PrototypeSpiritRarity), spirit.Rarity))
            throw new InvalidOperationException($"{spirit.Id}: invalid rarity.");
        if (!Enum.IsDefined(typeof(SpiritElement), spirit.Element))
            throw new InvalidOperationException($"{spirit.Id}: invalid element.");
        if (!Enum.IsDefined(typeof(SpiritCombatRole), spirit.CombatRole))
            throw new InvalidOperationException($"{spirit.Id}: invalid combat role.");
        if (spirit.BaseAttack <= 0f || spirit.AttackInterval <= 0f || spirit.UltimateEnergyMaximum <= 0f)
            throw new InvalidOperationException($"{spirit.Id}: combat values must be positive.");

        HashSet<string> abilityIds = new(StringComparer.OrdinalIgnoreCase);
        ValidateAbility(spirit, spirit.BasicAttack, abilityIds);
        ValidateAbility(spirit, spirit.SkillOne, abilityIds);
        ValidateAbility(spirit, spirit.SkillTwo, abilityIds);
        ValidateAbility(spirit, spirit.Ultimate, abilityIds);

        if (spirit.EvolutionMilestones.Count == 0 || spirit.EvolutionMilestones[0].RequiredLevel != 1)
            throw new InvalidOperationException($"{spirit.Id}: evolution milestones must begin at level 1.");
        int previousLevel = 0;
        for (int index = 0; index < spirit.EvolutionMilestones.Count; index++)
        {
            PrototypeSpiritEvolutionMilestone milestone = spirit.EvolutionMilestones[index];
            if (milestone.RequiredLevel <= previousLevel || milestone.Data.AttackMultiplier <= 0f ||
                string.IsNullOrWhiteSpace(milestone.Data.DisplayName))
                throw new InvalidOperationException($"{spirit.Id}: invalid evolution milestone at index {index}.");
            previousLevel = milestone.RequiredLevel;
        }
    }

    private static void ValidateAbility(PrototypeSpiritData spirit, PrototypeSpiritAbilityData ability, HashSet<string> abilityIds)
    {
        if (ability == null || string.IsNullOrWhiteSpace(ability.Id) || string.IsNullOrWhiteSpace(ability.DisplayName))
            throw new InvalidOperationException($"{spirit.Id}: every ability requires an id and display name.");
        if (!abilityIds.Add(ability.Id))
            throw new InvalidOperationException($"{spirit.Id}: duplicate ability id {ability.Id}.");
        if (!Enum.IsDefined(typeof(SpiritAbilityEffect), ability.Effect) || ability.PowerMultiplier < 0f ||
            ability.Cooldown < 0f || ability.Duration < 0f || ability.EnergyGain < 0f || ability.MaximumTargets < 1)
            throw new InvalidOperationException($"{spirit.Id}: invalid values in ability {ability.Id}.");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
