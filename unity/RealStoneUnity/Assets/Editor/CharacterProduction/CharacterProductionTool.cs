using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealStone.Editor.CharacterProduction
{
    internal static class CharacterProductionStandard
    {
        public const string GuidelinePath = "Assets/Characters/AI_2D_CHARACTER_GUIDELINES.md";
        public const string CharactersRoot = "Assets/Characters";
        public const int CanvasWidth = 512;
        public const int CanvasHeight = 512;
        public const int CenterX = 256;
        public const int GroundY = 64;
        public const float PixelsPerUnit = 100f;

        public static readonly string[] RequiredFolders =
        {
            "Master", "Parts", "Parts/Face", "Equipment", "Equipment/Weapon", "Equipment/Armor",
            "Equipment/Accessories", "Animations", "Data"
        };

        public static readonly Dictionary<string, Vector2Int> JointPixels = new()
        {
            ["Neck"] = new Vector2Int(256, 350),
            // Anatomical sides: character right is viewer-left.
            ["Shoulder_R"] = new Vector2Int(185, 325),
            ["Shoulder_L"] = new Vector2Int(327, 325),
            ["Elbow_R"] = new Vector2Int(150, 250),
            ["Elbow_L"] = new Vector2Int(362, 250),
            ["Wrist_R"] = new Vector2Int(125, 185),
            ["Wrist_L"] = new Vector2Int(387, 185),
            ["Hip_R"] = new Vector2Int(220, 220),
            ["Hip_L"] = new Vector2Int(292, 220),
            ["Knee_R"] = new Vector2Int(220, 135),
            ["Knee_L"] = new Vector2Int(292, 135),
            ["Ankle_R"] = new Vector2Int(220, 70),
            ["Ankle_L"] = new Vector2Int(292, 70),
            ["WeaponGrip_R"] = new Vector2Int(125, 180),
            ["Waist"] = new Vector2Int(256, 220),
            ["HairRoot"] = new Vector2Int(256, 350),
        };

        public static readonly Dictionary<string, int> SortingOrders = new()
        {
            ["HaloBack"] = -10,
            ["BackHair"] = 0,
            ["Body"] = 10,
            ["SkirtBack"] = 15,
            ["Leg_L"] = 20,
            ["Leg_R"] = 20,
            ["Skirt_L"] = 25,
            ["Skirt_R"] = 25,
            ["Arm_L"] = 30,
            ["Arm_R"] = 30,
            ["Head"] = 40,
            ["Face"] = 41,
            ["Eyes"] = 42,
            ["Mouth"] = 43,
            ["Earrings"] = 44,
            ["FrontHair"] = 50,
            ["Crown"] = 55,
            ["HairOrnament"] = 55,
            ["Weapon"] = 60,
            ["Equipment"] = 60,
        };
    }

    internal enum ValidationSeverity { Info, Warning, Error }

    internal readonly struct ValidationItem
    {
        public readonly ValidationSeverity Severity;
        public readonly string Message;

        public ValidationItem(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    internal static class CharacterProductionUtility
    {
        public static Texture2D CreateStandardWorkingCopy(Texture2D source, string characterId)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!IsValidId(characterId)) throw new ArgumentException("Character ID is invalid.", nameof(characterId));

            var sourcePath = AssetDatabase.GetAssetPath(source);
            var absoluteSourcePath = Path.GetFullPath(sourcePath);
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(absoluteSourcePath), false))
                    throw new InvalidOperationException($"PNG를 읽을 수 없습니다: {sourcePath}");

                var sourcePixels = decoded.GetPixels32();
                var minX = decoded.width;
                var maxX = -1;
                var minY = decoded.height;
                var maxY = -1;
                for (var y = 0; y < decoded.height; y++)
                for (var x = 0; x < decoded.width; x++)
                {
                    if (sourcePixels[y * decoded.width + x].a <= 20) continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }

                if (maxX < minX || maxY < minY)
                    throw new InvalidOperationException("원본 이미지가 완전히 투명합니다.");

                const int topMargin = 16;
                const int sideMargin = 16;
                var contentWidth = maxX - minX + 1;
                var contentHeight = maxY - minY + 1;
                var availableWidth = CharacterProductionStandard.CanvasWidth - sideMargin * 2;
                var availableHeight = CharacterProductionStandard.CanvasHeight - CharacterProductionStandard.GroundY - topMargin;
                var scale = Mathf.Min(availableWidth / (float)contentWidth, availableHeight / (float)contentHeight);
                var targetWidth = Mathf.Max(1, Mathf.RoundToInt(contentWidth * scale));
                var targetHeight = Mathf.Max(1, Mathf.RoundToInt(contentHeight * scale));
                var targetMinX = CharacterProductionStandard.CenterX - targetWidth / 2;
                var targetMinY = CharacterProductionStandard.GroundY;

                var output = new Texture2D(CharacterProductionStandard.CanvasWidth,
                    CharacterProductionStandard.CanvasHeight, TextureFormat.RGBA32, false);
                output.SetPixels32(new Color32[CharacterProductionStandard.CanvasWidth * CharacterProductionStandard.CanvasHeight]);
                for (var y = 0; y < targetHeight; y++)
                for (var x = 0; x < targetWidth; x++)
                {
                    var sourceX = minX + Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) / scale), 0, contentWidth - 1);
                    var sourceY = minY + Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) / scale), 0, contentHeight - 1);
                    output.SetPixel(targetMinX + x, targetMinY + y, sourcePixels[sourceY * decoded.width + sourceX]);
                }
                output.Apply(false, false);

                var sourceFolder = $"Assets/CharacterSource/{characterId}";
                Directory.CreateDirectory(sourceFolder);
                var targetPath = $"{sourceFolder}/{characterId}_Master_Standard512.png";
                File.WriteAllBytes(Path.GetFullPath(targetPath), output.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(output);
                AssetDatabase.Refresh();
                ApplyImportSettings(targetPath, true);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }
        }

        public static Texture2D CreateAlignedPartCanvas(Texture2D source, RectInt targetBounds, string targetAssetPath)
        {
            return CreateAlignedPartSectionCanvas(source, new Rect(0f, 0f, 1f, 1f), targetBounds, targetAssetPath);
        }

        public static Texture2D CreateAlignedPartSectionCanvas(Texture2D source, Rect sourceContentNormalized,
            RectInt targetBounds, string targetAssetPath)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (targetBounds.width <= 0 || targetBounds.height <= 0)
                throw new ArgumentException("Target bounds must have a positive size.", nameof(targetBounds));
            if (targetBounds.xMin < 0 || targetBounds.yMin < 0 ||
                targetBounds.xMax > CharacterProductionStandard.CanvasWidth ||
                targetBounds.yMax > CharacterProductionStandard.CanvasHeight)
                throw new ArgumentOutOfRangeException(nameof(targetBounds), "Target bounds must fit inside 512x512.");

            var sourcePath = AssetDatabase.GetAssetPath(source);
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(Path.GetFullPath(sourcePath)), false))
                    throw new InvalidOperationException($"PNG를 읽을 수 없습니다: {sourcePath}");
                var pixels = decoded.GetPixels32();
                var minX = decoded.width;
                var maxX = -1;
                var minY = decoded.height;
                var maxY = -1;
                for (var y = 0; y < decoded.height; y++)
                for (var x = 0; x < decoded.width; x++)
                {
                    if (pixels[y * decoded.width + x].a <= 20) continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
                if (maxX < minX || maxY < minY)
                    throw new InvalidOperationException("파츠 이미지가 완전히 투명합니다.");

                var fullContentWidth = maxX - minX + 1;
                var fullContentHeight = maxY - minY + 1;
                var sectionMinX = minX + Mathf.FloorToInt(Mathf.Clamp01(sourceContentNormalized.xMin) * fullContentWidth);
                var sectionMinY = minY + Mathf.FloorToInt(Mathf.Clamp01(sourceContentNormalized.yMin) * fullContentHeight);
                var sectionMaxX = minX + Mathf.CeilToInt(Mathf.Clamp01(sourceContentNormalized.xMax) * fullContentWidth) - 1;
                var sectionMaxY = minY + Mathf.CeilToInt(Mathf.Clamp01(sourceContentNormalized.yMax) * fullContentHeight) - 1;
                sectionMinX = Mathf.Clamp(sectionMinX, minX, maxX);
                sectionMinY = Mathf.Clamp(sectionMinY, minY, maxY);
                sectionMaxX = Mathf.Clamp(sectionMaxX, sectionMinX, maxX);
                sectionMaxY = Mathf.Clamp(sectionMaxY, sectionMinY, maxY);
                var contentWidth = sectionMaxX - sectionMinX + 1;
                var contentHeight = sectionMaxY - sectionMinY + 1;
                var output = new Texture2D(CharacterProductionStandard.CanvasWidth,
                    CharacterProductionStandard.CanvasHeight, TextureFormat.RGBA32, false);
                output.SetPixels32(new Color32[CharacterProductionStandard.CanvasWidth * CharacterProductionStandard.CanvasHeight]);
                for (var y = 0; y < targetBounds.height; y++)
                for (var x = 0; x < targetBounds.width; x++)
                {
                    var sourceX = sectionMinX + Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) * contentWidth / targetBounds.width), 0, contentWidth - 1);
                    var sourceY = sectionMinY + Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) * contentHeight / targetBounds.height), 0, contentHeight - 1);
                    output.SetPixel(targetBounds.x + x, targetBounds.y + y, pixels[sourceY * decoded.width + sourceX]);
                }
                output.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(targetAssetPath) ?? CharacterProductionStandard.CharactersRoot);
                File.WriteAllBytes(Path.GetFullPath(targetAssetPath), output.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(output);
                AssetDatabase.Refresh();
                ApplyImportSettings(targetAssetPath, true);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(targetAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }
        }

        public static string CharacterFolder(string id) => $"{CharacterProductionStandard.CharactersRoot}/{id}";

        public static bool IsValidId(string id) => !string.IsNullOrWhiteSpace(id) &&
                                                    id.All(c => char.IsLetterOrDigit(c) || c == '_');

        public static void EnsureFolders(string characterFolder)
        {
            foreach (var relative in CharacterProductionStandard.RequiredFolders)
                Directory.CreateDirectory(Path.Combine(characterFolder, relative));
        }

        public static void ApplyImportSettings(string assetPath, bool forceReimport)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = CharacterProductionStandard.PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = InferPivot(Path.GetFileNameWithoutExtension(assetPath));
            importer.SetTextureSettings(settings);
            if (forceReimport) importer.SaveAndReimport();
            else AssetDatabase.WriteImportSettingsIfDirty(assetPath);
        }

        private static Vector2 InferPivot(string fileName)
        {
            var key = fileName.ToLowerInvariant();
            string joint = null;
            if (key.Contains("forearm_l")) joint = "Elbow_L";
            else if (key.Contains("forearm_r")) joint = "Elbow_R";
            else if (key.Contains("hand_l")) joint = "Wrist_L";
            else if (key.Contains("hand_r")) joint = "Wrist_R";
            else if (key.Contains("arm_l")) joint = "Shoulder_L";
            else if (key.Contains("arm_r")) joint = "Shoulder_R";
            else if (key.Contains("foot_l")) joint = "Ankle_L";
            else if (key.Contains("foot_r")) joint = "Ankle_R";
            else if (key.Contains("lowerleg_l")) joint = "Knee_L";
            else if (key.Contains("lowerleg_r")) joint = "Knee_R";
            else if (key.Contains("leg_l")) joint = "Hip_L";
            else if (key.Contains("leg_r")) joint = "Hip_R";
            else if (key.Contains("head") || key.Contains("face") || key.Contains("eyes") || key.Contains("mouth")) joint = "Neck";
            else if (key.Contains("weapon")) joint = "WeaponGrip_R";
            else if (key.Contains("skirt")) joint = "Waist";
            else if (key.Contains("crown") || key.Contains("hairornament") || key.Contains("earrings")) joint = "Neck";
            else if (key.Contains("hair")) joint = "HairRoot";
            if (joint == null) return new Vector2(0.5f, CharacterProductionStandard.GroundY / 512f);
            var pixel = CharacterProductionStandard.JointPixels[joint];
            return new Vector2(pixel.x / 512f, pixel.y / 512f);
        }

        public static GameObject BuildHierarchy(string characterName, string characterFolder = null)
        {
            var root = new GameObject("CharacterRoot");
            root.AddComponent<SortingGroup>();
            AddPart(root.transform, "BackHair", characterFolder);
            AddPart(root.transform, "Body", characterFolder);
            var skirt = AddPart(root.transform, "Skirt", characterFolder);
            AddPart(skirt, "SkirtBack", characterFolder, "Parts/SkirtBack.png");
            AddPart(skirt, "Skirt_R", characterFolder, "Parts/Skirt_R.png");
            AddPart(skirt, "Skirt_L", characterFolder, "Parts/Skirt_L.png");
            var head = AddPart(root.transform, "Head", characterFolder);
            AddPart(head, "Face", characterFolder, "Parts/Face/Face.png");
            var eyes = AddPart(head, "Eyes", characterFolder, "Parts/Face/Eyes.png");
            var mouth = AddPart(head, "Mouth", characterFolder, "Parts/Face/Mouth.png");
            AddPart(head, "Earrings", characterFolder, "Equipment/Accessories/Earrings.png");
            AddPart(head, "Crown", characterFolder, "Equipment/Accessories/Crown.png");
            AddPart(head, "HairOrnament", characterFolder, "Equipment/Accessories/HairOrnament.png");

            var armL = AddPart(root.transform, "Arm_L", characterFolder);
            var forearmL = AddPart(armL, "Forearm_L", characterFolder);
            AddPart(forearmL, "Hand_L", characterFolder);
            var armR = AddPart(root.transform, "Arm_R", characterFolder);
            var forearmR = AddPart(armR, "Forearm_R", characterFolder);
            var handR = AddPart(forearmR, "Hand_R", characterFolder);
            AddPart(handR, "Weapon", characterFolder, "Equipment/Weapon/Weapon.png");

            var legL = AddPart(root.transform, "Leg_L", characterFolder);
            var lowerLegL = AddPart(legL, "LowerLeg_L", characterFolder);
            AddPart(lowerLegL, "Foot_L", characterFolder);
            var legR = AddPart(root.transform, "Leg_R", characterFolder);
            var lowerLegR = AddPart(legR, "LowerLeg_R", characterFolder);
            AddPart(lowerLegR, "Foot_R", characterFolder);

            AddPart(root.transform, "FrontHair", characterFolder);
            var equipment = AddPart(root.transform, "Equipment", characterFolder);
            AddPart(equipment, "Hair", characterFolder, "Equipment/Accessories/Hair.png");
            AddPart(equipment, "Hat", characterFolder, "Equipment/Accessories/Hat.png");
            AddPart(equipment, "Accessory", characterFolder, "Equipment/Accessories/Accessory.png");
            var effects = AddPart(root.transform, "Effects", characterFolder);
            AddPart(effects, "HaloBack", characterFolder, "Parts/Effects.png");
            if (!string.IsNullOrEmpty(characterFolder))
            {
                var expression = root.AddComponent<RealStone.Character.CharacterExpressionController>();
                expression.Configure(
                    eyes.GetComponent<SpriteRenderer>(),
                    mouth.GetComponent<SpriteRenderer>(),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{characterFolder}/Parts/Face/Eyes.png"),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{characterFolder}/Animations/Expressions/Eyes_Blink.png"),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{characterFolder}/Animations/Expressions/Eyes_Attack.png"),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{characterFolder}/Animations/Expressions/Eyes_Hit.png"),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{characterFolder}/Parts/Face/Mouth.png"),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{characterFolder}/Animations/Expressions/Mouth_Attack.png"),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{characterFolder}/Animations/Expressions/Mouth_Hit.png"));
            }
            ApplyStandardLocalPositions(root.transform);
            root.name = "CharacterRoot";
            return root;
        }

        private static void ApplyStandardLocalPositions(Transform root)
        {
            root.localPosition = Vector3.zero;
            foreach (Transform child in root) ApplyStandardLocalPositionRecursive(child, root);
        }

        private static void ApplyStandardLocalPositionRecursive(Transform current, Transform root)
        {
            var parentPixel = current.parent == root
                ? new Vector2(CharacterProductionStandard.CenterX, CharacterProductionStandard.GroundY)
                : PivotPixelForName(current.parent.name);
            var currentPixel = PivotPixelForName(current.name);
            current.localPosition = (currentPixel - parentPixel) / CharacterProductionStandard.PixelsPerUnit;
            current.localRotation = Quaternion.identity;
            current.localScale = Vector3.one;
            foreach (Transform child in current) ApplyStandardLocalPositionRecursive(child, root);
        }

        private static Vector2 PivotPixelForName(string objectName)
        {
            var key = objectName.ToLowerInvariant();
            if (key == "head" || key == "face" || key == "eyes" || key == "mouth" ||
                key == "backhair" || key == "fronthair" || key == "earrings" ||
                key == "crown" || key == "hairornament" || key == "hair")
                return CharacterProductionStandard.JointPixels["Neck"];
            if (key == "arm_r") return CharacterProductionStandard.JointPixels["Shoulder_R"];
            if (key == "arm_l") return CharacterProductionStandard.JointPixels["Shoulder_L"];
            if (key == "forearm_r") return CharacterProductionStandard.JointPixels["Elbow_R"];
            if (key == "forearm_l") return CharacterProductionStandard.JointPixels["Elbow_L"];
            if (key == "hand_r") return CharacterProductionStandard.JointPixels["Wrist_R"];
            if (key == "hand_l") return CharacterProductionStandard.JointPixels["Wrist_L"];
            if (key == "weapon") return CharacterProductionStandard.JointPixels["WeaponGrip_R"];
            if (key == "leg_r") return CharacterProductionStandard.JointPixels["Hip_R"];
            if (key == "leg_l") return CharacterProductionStandard.JointPixels["Hip_L"];
            if (key == "lowerleg_r") return CharacterProductionStandard.JointPixels["Knee_R"];
            if (key == "lowerleg_l") return CharacterProductionStandard.JointPixels["Knee_L"];
            if (key == "foot_r") return CharacterProductionStandard.JointPixels["Ankle_R"];
            if (key == "foot_l") return CharacterProductionStandard.JointPixels["Ankle_L"];
            if (key == "skirt" || key == "skirtback" || key == "skirt_r" || key == "skirt_l")
                return CharacterProductionStandard.JointPixels["Waist"];
            return new Vector2(CharacterProductionStandard.CenterX, CharacterProductionStandard.GroundY);
        }

        private static Transform AddPart(Transform parent, string name, string characterFolder, string explicitPath = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var sortingKey = name.StartsWith("Forearm") || name.StartsWith("Hand") ?
                (name.EndsWith("_L") ? "Arm_L" : "Arm_R") :
                name.StartsWith("LowerLeg") || name.StartsWith("Foot") ?
                    (name.EndsWith("_L") ? "Leg_L" : "Leg_R") : name;
            if (CharacterProductionStandard.SortingOrders.TryGetValue(sortingKey, out var order))
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = order;
                if (!string.IsNullOrEmpty(characterFolder))
                {
                    var path = explicitPath ?? $"Parts/{name}.png";
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{characterFolder}/{path}");
                }
            }
            return go.transform;
        }

        public static List<ValidationItem> ValidateCharacterFolder(string folder)
        {
            var result = new List<ValidationItem>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                result.Add(new ValidationItem(ValidationSeverity.Error, $"캐릭터 폴더가 없습니다: {folder}"));
                return result;
            }

            foreach (var required in CharacterProductionStandard.RequiredFolders)
                if (!AssetDatabase.IsValidFolder($"{folder}/{required}"))
                    result.Add(new ValidationItem(ValidationSeverity.Error, $"필수 폴더 누락: {required}"));

            var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (textures.Length == 0)
                result.Add(new ValidationItem(ValidationSeverity.Warning, "PNG/Sprite 이미지가 없습니다."));

            foreach (var path in textures)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null && (texture.width != 512 || texture.height != 512))
                    result.Add(new ValidationItem(ValidationSeverity.Error,
                        $"Canvas 위반: {path} ({texture.width}x{texture.height}, 요구 512x512)"));
                ValidateImporter(path, result);
            }
            var masters = textures.Where(x => x.Contains("/Master/")).ToArray();
            foreach (var master in masters) ValidateMasterAlignment(master, result);

            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (prefabs.Length == 0)
                result.Add(new ValidationItem(ValidationSeverity.Warning, "캐릭터 Prefab이 없습니다."));
            foreach (var prefab in prefabs) ValidatePrefab(prefab, result);

            if (result.All(x => x.Severity != ValidationSeverity.Error))
                result.Add(new ValidationItem(ValidationSeverity.Info, "치명적인 규격 위반이 없습니다."));
            return result;
        }

        private static void ValidateMasterAlignment(string assetPath, List<ValidationItem> result)
        {
            var absolute = Path.GetFullPath(assetPath);
            if (!File.Exists(absolute)) return;
            var temp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(temp, File.ReadAllBytes(absolute), false)) return;
                var pixels = temp.GetPixels32();
                var minX = temp.width;
                var maxX = -1;
                var minY = temp.height;
                for (var y = 0; y < temp.height; y++)
                for (var x = 0; x < temp.width; x++)
                {
                    if (pixels[y * temp.width + x].a <= 20) continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                }
                if (maxX < 0)
                {
                    result.Add(new ValidationItem(ValidationSeverity.Error, $"Master가 완전히 투명함: {assetPath}"));
                    return;
                }
                var center = (minX + maxX) * 0.5f;
                if (Mathf.Abs(center - CharacterProductionStandard.CenterX) > 2f)
                    result.Add(new ValidationItem(ValidationSeverity.Error,
                        $"중심선 위반: {assetPath} (불투명 중심 X={center:0.0}, 요구 256)"));
                if (Mathf.Abs(minY - CharacterProductionStandard.GroundY) > 2)
                    result.Add(new ValidationItem(ValidationSeverity.Error,
                        $"발 기준선 위반: {assetPath} (Ground Y={minY}, 요구 64)"));
            }
            finally { UnityEngine.Object.DestroyImmediate(temp); }
        }

        private static void ValidateImporter(string path, List<ValidationItem> result)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                result.Add(new ValidationItem(ValidationSeverity.Error, $"Sprite Single 설정 위반: {path}"));
            if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.001f)
                result.Add(new ValidationItem(ValidationSeverity.Error, $"Pixels Per Unit 위반: {path}"));
            if (importer.mipmapEnabled)
                result.Add(new ValidationItem(ValidationSeverity.Error, $"Mip Maps가 켜져 있음: {path}"));
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                result.Add(new ValidationItem(ValidationSeverity.Error, $"압축이 None이 아님: {path}"));
            if (importer.filterMode != FilterMode.Bilinear)
                result.Add(new ValidationItem(ValidationSeverity.Warning, $"현재 규격은 Bilinear 사용: {path}"));
        }

        private static void ValidatePrefab(string path, List<ValidationItem> result)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.name != "CharacterRoot")
                    result.Add(new ValidationItem(ValidationSeverity.Error, $"Prefab Root 이름 위반: {path}"));
                ValidateTransform(root.transform, root.transform, path, result);
                var required = new[] { "Body", "Head", "Arm_L", "Arm_R", "Leg_L", "Leg_R", "Equipment", "Effects" };
                foreach (var name in required)
                    if (root.transform.Find(name) == null)
                        result.Add(new ValidationItem(ValidationSeverity.Error, $"Hierarchy 누락: {name} ({path})"));
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var key = renderer.name.StartsWith("Forearm") || renderer.name.StartsWith("Hand") ?
                        (renderer.name.EndsWith("_L") ? "Arm_L" : "Arm_R") : renderer.name;
                    if (CharacterProductionStandard.SortingOrders.TryGetValue(key, out var expected) &&
                        renderer.sortingOrder != expected)
                        result.Add(new ValidationItem(ValidationSeverity.Error,
                            $"Sorting Order 위반: {renderer.name}={renderer.sortingOrder}, 요구 {expected} ({path})"));
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ValidateTransform(Transform transform, Transform root, string path, List<ValidationItem> result)
        {
            var expectedPosition = Vector3.zero;
            if (transform != root)
            {
                var parentPixel = transform.parent == root
                    ? new Vector2(CharacterProductionStandard.CenterX, CharacterProductionStandard.GroundY)
                    : PivotPixelForName(transform.parent.name);
                expectedPosition = (PivotPixelForName(transform.name) - parentPixel) /
                                   CharacterProductionStandard.PixelsPerUnit;
            }
            if ((transform.localPosition - expectedPosition).sqrMagnitude > 0.000001f ||
                Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.001f ||
                (transform.localScale - Vector3.one).sqrMagnitude > 0.000001f)
                result.Add(new ValidationItem(ValidationSeverity.Error,
                    $"기본 Local Transform 위반: {transform.name}, 위치={transform.localPosition}, 요구={expectedPosition} ({path})"));
            foreach (Transform child in transform) ValidateTransform(child, root, path, result);
        }

        public static string BuildYaml(string id, string displayName)
        {
            return $"character:\n  id: {id}\n  name: {displayName}\n  canvas:\n    width: 512\n    height: 512\n" +
                   "  alignment:\n    center_x: 256\n    ground_y: 64\n  pose:\n    type: neutral_a_pose\n" +
                   "  unity:\n    root: CharacterRoot\n    local_position: [0, 0, 0]\n    local_rotation: [0, 0, 0]\n    local_scale: [1, 1, 1]\n";
        }
    }

    public sealed class CharacterCreatorWindow : EditorWindow
    {
        private string characterId = "character_001";
        private string displayName = "New Character";
        private Texture2D masterImage;
        private DefaultAsset existingCharacterFolder;
        private string status;

        [MenuItem("Tools/2D Character/Character Creator")]
        public static void Open() => GetWindow<CharacterCreatorWindow>("Character Creator");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("2D Character Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("AI_2D_CHARACTER_GUIDELINES.md 규격을 통과한 512x512 원본만 등록합니다. 이미지를 자동 리사이즈하지 않습니다.", MessageType.Info);
            characterId = EditorGUILayout.TextField("Character ID", characterId);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            masterImage = (Texture2D)EditorGUILayout.ObjectField("Master PNG", masterImage, typeof(Texture2D), false);
            GUI.enabled = masterImage != null && CharacterProductionUtility.IsValidId(characterId);
            if (GUILayout.Button("Create Non-Destructive 512x512 Working Copy")) CreateWorkingCopy();
            GUI.enabled = true;
            GUI.enabled = masterImage != null && CharacterProductionUtility.IsValidId(characterId);
            if (GUILayout.Button("Create Standard Character")) CreateCharacter();
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, status.StartsWith("완료") ? MessageType.Info : MessageType.Error);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Existing Character", EditorStyles.boldLabel);
            existingCharacterFolder = (DefaultAsset)EditorGUILayout.ObjectField("Character Folder", existingCharacterFolder, typeof(DefaultAsset), false);
            GUI.enabled = existingCharacterFolder != null;
            if (GUILayout.Button("Build / Refresh Prefab From Standard Parts")) RefreshPrefab();
            GUI.enabled = true;
        }

        private void CreateWorkingCopy()
        {
            try
            {
                masterImage = CharacterProductionUtility.CreateStandardWorkingCopy(masterImage, characterId);
                status = $"완료: 원본을 보존하고 512x512 작업본을 만들었습니다. ({AssetDatabase.GetAssetPath(masterImage)})";
                Selection.activeObject = masterImage;
                EditorGUIUtility.PingObject(masterImage);
            }
            catch (Exception exception)
            {
                status = $"실패: {exception.Message}";
            }
        }

        private void CreateCharacter()
        {
            if (masterImage.width != 512 || masterImage.height != 512)
            {
                status = $"실패: Master Canvas가 512x512가 아닙니다. 현재 {masterImage.width}x{masterImage.height}";
                return;
            }
            var folder = CharacterProductionUtility.CharacterFolder(characterId);
            if (AssetDatabase.IsValidFolder(folder))
            {
                status = "실패: 같은 Character ID 폴더가 이미 존재합니다.";
                return;
            }
            CharacterProductionUtility.EnsureFolders(folder);
            AssetDatabase.Refresh();
            var sourcePath = AssetDatabase.GetAssetPath(masterImage);
            var targetPath = $"{folder}/Master/{characterId}_Master.png";
            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                status = "실패: Master PNG 복사에 실패했습니다.";
                return;
            }
            CharacterProductionUtility.ApplyImportSettings(targetPath, true);
            File.WriteAllText($"{folder}/Data/{characterId}.yaml", CharacterProductionUtility.BuildYaml(characterId, displayName), new UTF8Encoding(false));
            var root = CharacterProductionUtility.BuildHierarchy(displayName, folder);
            PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/CharacterRoot.prefab");
            DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            status = $"완료: {folder}";
        }

        private void RefreshPrefab()
        {
            var folder = AssetDatabase.GetAssetPath(existingCharacterFolder);
            if (!folder.StartsWith(CharacterProductionStandard.CharactersRoot + "/", StringComparison.Ordinal))
            {
                status = "실패: Assets/Characters 아래의 캐릭터 폴더를 선택하세요.";
                return;
            }
            var id = Path.GetFileName(folder);
            var root = CharacterProductionUtility.BuildHierarchy(id, folder);
            PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/CharacterRoot.prefab");
            DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            status = $"완료: {folder}/CharacterRoot.prefab";
        }
    }

    public sealed class CharacterValidatorWindow : EditorWindow
    {
        private DefaultAsset characterFolder;
        private Vector2 scroll;
        private List<ValidationItem> items = new();

        [MenuItem("Tools/2D Character/Character Validator")]
        public static void Open() => GetWindow<CharacterValidatorWindow>("Character Validator");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("2D Character Validator", EditorStyles.boldLabel);
            characterFolder = (DefaultAsset)EditorGUILayout.ObjectField("Character Folder", characterFolder, typeof(DefaultAsset), false);
            GUI.enabled = characterFolder != null;
            if (GUILayout.Button("Validate Selected Character"))
                items = CharacterProductionUtility.ValidateCharacterFolder(AssetDatabase.GetAssetPath(characterFolder));
            GUI.enabled = true;
            if (GUILayout.Button("Validate All Characters")) ValidateAll();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var item in items)
            {
                var type = item.Severity == ValidationSeverity.Error ? MessageType.Error :
                    item.Severity == ValidationSeverity.Warning ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox(item.Message, type);
            }
            EditorGUILayout.EndScrollView();
        }

        private void ValidateAll()
        {
            items.Clear();
            foreach (var path in AssetDatabase.GetSubFolders(CharacterProductionStandard.CharactersRoot))
            {
                if (path.EndsWith("/CharacterProduction")) continue;
                items.Add(new ValidationItem(ValidationSeverity.Info, $"--- {path} ---"));
                items.AddRange(CharacterProductionUtility.ValidateCharacterFolder(path));
            }
        }
    }

    public sealed class CharacterProjectSettingsWindow : EditorWindow
    {
        private Vector2 scroll;

        [MenuItem("Tools/2D Character/Project Settings")]
        public static void Open() => GetWindow<CharacterProjectSettingsWindow>("2D Character Settings");

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Production Standard (Read Only)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("Guideline", CharacterProductionStandard.GuidelinePath);
                EditorGUILayout.IntField("Canvas Width", CharacterProductionStandard.CanvasWidth);
                EditorGUILayout.IntField("Canvas Height", CharacterProductionStandard.CanvasHeight);
                EditorGUILayout.IntField("Center X", CharacterProductionStandard.CenterX);
                EditorGUILayout.IntField("Ground Y", CharacterProductionStandard.GroundY);
                EditorGUILayout.FloatField("Pixels Per Unit", CharacterProductionStandard.PixelsPerUnit);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Global Joint Layout", EditorStyles.boldLabel);
                foreach (var pair in CharacterProductionStandard.JointPixels)
                    EditorGUILayout.Vector2IntField(pair.Key, pair.Value);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Sorting Order", EditorStyles.boldLabel);
                foreach (var pair in CharacterProductionStandard.SortingOrders)
                    EditorGUILayout.IntField(pair.Key, pair.Value);
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Apply Import Settings To All Character Textures")) ApplyAllImports();
            if (GUILayout.Button("Open Single Source Guideline"))
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(CharacterProductionStandard.GuidelinePath);
            EditorGUILayout.EndScrollView();
        }

        private static void ApplyAllImports()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { CharacterProductionStandard.CharactersRoot })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            try
            {
                for (var i = 0; i < paths.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("2D Character Import", paths[i], i / (float)Mathf.Max(1, paths.Length));
                    CharacterProductionUtility.ApplyImportSettings(paths[i], true);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
        }
    }
}
