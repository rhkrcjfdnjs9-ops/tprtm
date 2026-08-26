using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealStone.Editor
{
    public static class GraniaRigBuilder
    {
        private const string Root = "Assets/ArtSource/GraniaRig/Layers/";
        private const string PrefabPath = "Assets/Prefabs/GraniaRigPrototypeV3.prefab";

        [MenuItem("Real Stone/Grania Rig/Build Prototype Prefab")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            var rig = new GameObject("GraniaRigPrototype");
            rig.AddComponent<SortingGroup>();

            var root = Bone(rig.transform, "Root", Vector3.zero);
            var pelvis = Bone(root, "Pelvis", new Vector3(0f, 0.2f, 0f));
            var torso = Bone(pelvis, "Torso", new Vector3(0f, 2.1f, 0f));
            var neck = Bone(torso, "Neck", new Vector3(0f, 1.25f, 0f));
            var head = Bone(neck, "Head", new Vector3(0f, 0.95f, 0f));

            var shoulderR = Bone(torso, "Shoulder.R", new Vector3(-0.7f, 0.5f, 0f));
            var upperR = Bone(shoulderR, "UpperArm.R", Vector3.zero);
            var lowerR = Bone(upperR, "LowerArm.R", new Vector3(-0.28f, -0.82f, 0f));
            var handR = Bone(lowerR, "Hand.R", new Vector3(-0.12f, -0.72f, 0f));
            var sword = Bone(handR, "Sword", Vector3.zero);

            var shoulderL = Bone(torso, "Shoulder.L", new Vector3(0.7f, 0.5f, 0f));
            var upperL = Bone(shoulderL, "UpperArm.L", Vector3.zero);
            var lowerL = Bone(upperL, "LowerArm.L", new Vector3(0.28f, -0.82f, 0f));
            var handL = Bone(lowerL, "Hand.L", new Vector3(0.12f, -0.72f, 0f));

            var thighR = Bone(pelvis, "Thigh.R", new Vector3(-0.48f, 0f, 0f));
            Bone(thighR, "Calf.R", new Vector3(0f, -1.35f, 0f));
            var thighL = Bone(pelvis, "Thigh.L", new Vector3(0.48f, 0f, 0f));
            Bone(thighL, "Calf.L", new Vector3(0f, -1.35f, 0f));

            var hairLeft = Chain(head, "Hair.Left", 3, new Vector3(0.55f, 0.35f, 0f), new Vector3(0f, -0.9f, 0f));
            var hairRight = Chain(head, "Hair.Right", 3, new Vector3(-0.55f, 0.35f, 0f), new Vector3(0f, -0.9f, 0f));
            var skirtCenter = Chain(pelvis, "Skirt.Center", 2, new Vector3(0f, -0.25f, 0f), new Vector3(0f, -1.1f, 0f));
            Chain(pelvis, "Skirt.Left", 2, new Vector3(0.65f, -0.2f, 0f), new Vector3(0.2f, -1.1f, 0f));
            Chain(pelvis, "Skirt.Right", 2, new Vector3(-0.65f, -0.2f, 0f), new Vector3(-0.2f, -1.1f, 0f));

            Part(root, "HaloBack", "FX/HaloBack_v1.png", new Vector3(0f, 4.65f), 0.31f, 0);
            Part(root, "FloatingCrystals", "FX/FloatingCrystals_v1.png", new Vector3(0f, 2.65f), 0.31f, 1);
            Part(head, "BackHair", "Hair/Back/BackHairBase_v1.png", new Vector3(0f, -0.55f), 0.22f, 2);
            Part(skirtCenter, "LowerCostume", "Costume/LowerCostumeComplete_v1.png", new Vector3(0f, -0.62f), 0.35f, 3);
            Part(thighR, "Leg.R", "Leg/Right/LegComplete_v1.png", new Vector3(0f, -0.78f), 0.27f, 4);
            Part(thighL, "Leg.L", "Leg/Left/LegComplete_mirroredSource_v1.png", new Vector3(0f, -0.78f), 0.27f, 4, true);
            Part(torso, "TorsoArmor", "Body/TorsoArmor_v1.png", new Vector3(0f, 0f), 0.42f, 5);
            Part(neck, "NeckConnector", "Body/NeckConnector_v1.png", new Vector3(0f, 0.28f), 0.17f, 6);
            Part(upperR, "UpperArm.R.Sprite", "Arm/Right/UpperArm_v1.png", new Vector3(-0.08f, -0.32f), 0.13f, 7);
            Part(lowerR, "LowerArm.R.Sprite", "Arm/Right/LowerArm_v1.png", new Vector3(-0.04f, -0.38f), 0.13f, 8);
            Part(handR, "GripHand.R", "Arm/Right/GripHand_v1.png", Vector3.zero, 0.11f, 9);
            Part(sword, "SwordSprite", "Weapon/SwordComplete_v1.png", new Vector3(-1.05f, -0.92f), 0.31f, 8);
            Part(upperL, "UpperArm.L.Sprite", "Arm/Left/UpperArm_mirroredSource_v1.png", new Vector3(0.08f, -0.32f), 0.13f, 7, true);
            Part(lowerL, "LowerArm.L.Sprite", "Arm/Left/LowerArm_mirroredSource_v1.png", new Vector3(0.04f, -0.38f), 0.13f, 8, true);
            Part(handL, "RelaxedHand.L", "Arm/Left/RelaxedHand_v1.png", Vector3.zero, 0.052f, 9);
            Part(head, "FaceBase", "Head/FaceBase_v1.png", new Vector3(0f, 0f), 0.19f, 10);
            Part(hairLeft, "SideHair.L", "Hair/Side/CharacterLeftLock_v1.png", new Vector3(0.38f, -0.95f), 0.16f, 11);
            Part(hairRight, "SideHair.R", "Hair/Side/CharacterRightLock_mirroredSource_v1.png", new Vector3(-0.38f, -0.95f), 0.16f, 11, true);
            Part(head, "FrontHair", "Hair/Front/FrontHairBase_v1.png", new Vector3(0f, 0.08f), 0.19f, 12);
            Part(head, "CrystalCrown", "Accessory/CrystalCrown_v1.png", new Vector3(0f, 1.12f), 0.115f, 13);

            PrefabUtility.SaveAsPrefabAsset(rig, PrefabPath);
            Object.DestroyImmediate(rig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"GRANIA_RIG_PREFAB_BUILT: {PrefabPath}");
        }

        private static Transform Bone(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Transform Chain(Transform parent, string prefix, int count, Vector3 start, Vector3 step)
        {
            var current = Bone(parent, prefix + ".01", start);
            var first = current;
            for (var i = 2; i <= count; i++) current = Bone(current, $"{prefix}.{i:00}", step);
            return first;
        }

        private static void Part(Transform parent, string name, string relativePath, Vector2 localPosition,
            float scale, int sortingOrder, bool flipX = false)
        {
            var assetPath = Root + relativePath;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing rig texture", assetPath);
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.spritePixelsPerUnit != 100f || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) throw new FileNotFoundException("Missing rig sprite", assetPath);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.flipX = flipX;
        }
    }
}
