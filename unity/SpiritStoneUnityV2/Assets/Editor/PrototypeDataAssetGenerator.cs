using System;
using System.Collections.Generic;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEngine;

public static class PrototypeDataAssetGenerator
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string PrototypeFolder = ResourcesFolder + "/Prototype";
    private const string SpiritsFolder = PrototypeFolder + "/Spirits";

    [MenuItem("Tools/Prototype/Rebuild Data Assets")]
    public static void RebuildDataAssets()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder(ResourcesFolder, "Prototype");
        EnsureFolder(PrototypeFolder, "Spirits");

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
        Debug.LogFormat("[PrototypeDataAssetGenerator] Generated {0} spirit definitions and stage balance data.", ids.Count);
    }

    [MenuItem("Tools/Prototype/Validate Data Assets")]
    public static void ValidateDataAssets()
    {
        PrototypeSpiritDefinition[] definitions = Resources.LoadAll<PrototypeSpiritDefinition>("Prototype/Spirits");
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < definitions.Length; index++)
        {
            PrototypeSpiritDefinition definition = definitions[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                throw new InvalidOperationException("Spirit definition contains an empty id.");
            if (!ids.Add(definition.Id)) throw new InvalidOperationException($"Duplicate spirit id: {definition.Id}");
            definition.CreateRuntimeData();
        }
        if (Resources.Load<PrototypeStageBalanceDefinition>("Prototype/StageBalance") == null)
            throw new InvalidOperationException("StageBalance asset is missing.");
        Debug.LogFormat("[PrototypeDataAssetGenerator] Validation passed for {0} spirit definitions.", ids.Count);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
