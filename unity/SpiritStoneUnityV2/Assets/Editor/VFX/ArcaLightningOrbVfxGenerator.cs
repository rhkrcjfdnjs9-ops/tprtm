using System;
using System.Collections.Generic;
using System.IO;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor.VFX
{
    public static class ArcaLightningOrbVfxGenerator
    {
        private const int CanvasSize = 128;
        private const float PixelsPerUnit = 128f;
        private const string Root = "Assets/VFX/Resources/VFX/Lightning/LightningOrb";
        private const string MaterialFolder = "Assets/VFX/Resources/VFX/Materials";
        private const string MaterialPath = MaterialFolder + "/mat_vfx_lightning_sprite_unlit.mat";

        private static readonly Color Void = Hex("180326");
        private static readonly Color Deep = Hex("2B0644");
        private static readonly Color Dark = Hex("60109A");
        private static readonly Color Purple = Hex("7D1AC4");
        private static readonly Color Mid = Hex("9830DF");
        private static readonly Color Vivid = Hex("B84DF2");
        private static readonly Color Bright = Hex("D26CFF");
        private static readonly Color Pale = Hex("F2C2FF");
        private static readonly Color Peak = Color.white;

        [MenuItem("Tools/SpiritStone/VFX/Generate Arca Lightning Orb")]
        public static void Generate()
        {
            EnsureFolder("Assets", "VFX");
            EnsureFolder("Assets/VFX", "Resources");
            EnsureFolder("Assets/VFX/Resources", "VFX");
            EnsureFolder("Assets/VFX/Resources/VFX", "Lightning");
            EnsureFolder("Assets/VFX/Resources/VFX/Lightning", "LightningOrb");
            EnsureFolder(Root, "Gather");
            EnsureFolder(Root, "Projectile");
            EnsureFolder(Root, "Impact");
            EnsureFolder("Assets/VFX/Resources/VFX", "Materials");

            GenerateGatherFrames();
            GenerateProjectileFrames();
            GenerateImpactFrames();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextures();
            CreateMaterial();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.LogFormat("[ArcaLightningOrbVfxGenerator] Generated 20 Lightning Orb frames and URP 2D material.");
        }

        [MenuItem("Tools/SpiritStone/VFX/Preview Arca Lightning Orb")]
        public static void Preview()
        {
            IdleBattlePrototype battle = UnityEngine.Object.FindFirstObjectByType<IdleBattlePrototype>();
            if (battle == null)
            {
                Debug.LogWarningFormat("[ArcaLightningOrbVfxGenerator] Enter Play Mode with IdleBattlePrototype before previewing.");
                return;
            }
            battle.PreviewArcaLightningOrbVfx();
        }

        [MenuItem("Tools/SpiritStone/VFX/Configure Imported Arca Lightning Orb")]
        public static void ConfigureImportedFrames()
        {
            AssetDatabase.Refresh();
            ConfigureTextures();
            AssetDatabase.SaveAssets();
            Debug.LogFormat("[ArcaLightningOrbVfxGenerator] Configured imported Lightning Orb frames.");
        }

        [MenuItem("Tools/SpiritStone/VFX/Preview Arca Lightning Orb", true)]
        private static bool CanPreview() => EditorApplication.isPlaying;

        private static void GenerateGatherFrames()
        {
            for (int frame = 0; frame < 6; frame++)
            {
                Texture2D texture = NewTexture();
                Vector2 center = Center;
                int radius = 3 + frame * 2;
                DrawOrb(texture, center, radius, frame >= 4);
                DrawBrokenRing(texture, center, 18 + frame * 2, frame * 19f, 3, Bright, 1);
                int arcCount = 2 + frame / 2;
                for (int arc = 0; arc < arcCount; arc++)
                {
                    float angle = frame * 23f + arc * (360f / arcCount);
                    Vector2 start = center + Direction(angle) * (34 - frame * 2);
                    Vector2 end = center + Direction(angle + 11f) * (radius + 3);
                    DrawJaggedBolt(texture, start, end, 300 + frame * 17 + arc, 2, 2, false);
                }
                Save(texture, "Gather", "gather", frame);
            }
        }

        private static void GenerateProjectileFrames()
        {
            for (int frame = 0; frame < 6; frame++)
            {
                Texture2D texture = NewTexture();
                Vector2 center = Center + new Vector2((frame % 2 == 0 ? 1 : -1), 0f);
                DrawOrb(texture, center, 12, true);
                DrawBrokenRing(texture, center, 18, frame * 31f, 3, Vivid, 2);

                for (int arc = 0; arc < 3; arc++)
                {
                    float angle = frame * 41f + arc * 127f;
                    Vector2 start = center + Direction(angle) * 15f;
                    Vector2 end = center + Direction(angle + (arc % 2 == 0 ? 24f : -28f)) * (25 + arc * 3);
                    DrawJaggedBolt(texture, start, end, 500 + frame * 13 + arc, 2, 2, false);
                }
                Save(texture, "Projectile", "projectile", frame);
            }
        }

        private static void GenerateImpactFrames()
        {
            for (int frame = 0; frame < 8; frame++)
            {
                Texture2D texture = NewTexture();
                Vector2 center = Center;
                switch (frame)
                {
                    case 0:
                        DrawOrb(texture, center, 7, true);
                        DrawInwardSpikes(texture, center, 23, 4, 700);
                        break;
                    case 1:
                        DrawFlash(texture, center, 19, 8);
                        break;
                    case 2:
                        DrawExplosion(texture, center, 50, 810, true);
                        break;
                    case 3:
                        DrawExplosion(texture, center, 55, 850, false);
                        break;
                    case 4:
                        DrawExplosion(texture, center, 40, 890, false);
                        DrawFragments(texture, center, 28, 6, 930);
                        break;
                    case 5:
                        DrawFragments(texture, center, 38, 6, 970);
                        DrawBrokenRing(texture, center, 23, 17f, 4, Mid, 1);
                        break;
                    case 6:
                        DrawFragments(texture, center, 45, 4, 1010);
                        break;
                    default:
                        DrawFragments(texture, center, 49, 2, 1050);
                        break;
                }
                Save(texture, "Impact", "impact", frame);
            }
        }

        private static void DrawExplosion(Texture2D texture, Vector2 center, int radius, int seed, bool peak)
        {
            DrawCircle(texture, center, peak ? 15 : 12, Deep);
            DrawCircle(texture, center, peak ? 11 : 9, Bright);
            DrawCircle(texture, center, peak ? 7 : 5, peak ? Peak : Pale);

            float[] angles = { 8f, 71f, 151f, 224f, 301f };
            for (int index = 0; index < angles.Length; index++)
            {
                float lengthScale = 0.72f + index * 0.065f;
                Vector2 end = center + Direction(angles[index] + (index % 2 == 0 ? 5f : -7f)) * radius * lengthScale;
                DrawJaggedBolt(texture, center, end, seed + index * 29, 4, 4, index < 3 && peak);
            }

            DrawFragments(texture, center, radius - 8, peak ? 5 : 3, seed + 200);
        }

        private static void DrawInwardSpikes(Texture2D texture, Vector2 center, int distance, int count, int seed)
        {
            for (int index = 0; index < count; index++)
            {
                float angle = index * (360f / count) + 13f;
                Vector2 start = center + Direction(angle) * distance;
                Vector2 end = center + Direction(angle + 4f) * 9f;
                DrawJaggedBolt(texture, start, end, seed + index * 11, 2, 2, false);
            }
        }

        private static void DrawFlash(Texture2D texture, Vector2 center, int radius, int rays)
        {
            DrawCircle(texture, center, radius, Bright);
            DrawCircle(texture, center, radius - 5, Pale);
            DrawCircle(texture, center, radius - 10, Peak);
            for (int ray = 0; ray < rays; ray++)
            {
                float angle = ray * (360f / rays) + (ray % 2 == 0 ? 3f : -4f);
                int length = ray % 2 == 0 ? 37 : 27;
                DrawLine(texture, center + Direction(angle) * (radius - 2), center + Direction(angle) * length,
                    ray % 2 == 0 ? 2 : 1, Peak);
            }
        }

        private static void DrawFragments(Texture2D texture, Vector2 center, int distance, int count, int seed)
        {
            var random = new System.Random(seed);
            for (int index = 0; index < count; index++)
            {
                float angle = (float)random.NextDouble() * 360f;
                float length = distance * (0.72f + (float)random.NextDouble() * 0.28f);
                Vector2 point = center + Direction(angle) * length;
                Vector2 tail = point - Direction(angle + 9f) * (3 + random.Next(0, 4));
                DrawLine(texture, tail, point, index % 3 == 0 ? 2 : 1, index % 2 == 0 ? Bright : Vivid);
            }
        }

        private static void DrawOrb(Texture2D texture, Vector2 center, int radius, bool peak)
        {
            DrawCircle(texture, center, radius + 3, Void);
            DrawCircle(texture, center, radius + 1, Dark);
            DrawCircle(texture, center, radius, Purple);
            DrawCircle(texture, center, Mathf.Max(1, radius - 3), Bright);
            if (peak) DrawCircle(texture, center, Mathf.Max(1, radius / 3), Peak);
        }

        private static void DrawBrokenRing(Texture2D texture, Vector2 center, int radius, float rotation,
            int segments, Color color, int thickness)
        {
            const int samplesPerSegment = 15;
            float segmentSpan = 54f;
            for (int segment = 0; segment < segments; segment++)
            {
                float start = rotation + segment * (360f / segments);
                Vector2 previous = center + Direction(start) * radius;
                for (int sample = 1; sample <= samplesPerSegment; sample++)
                {
                    float angle = start + segmentSpan * sample / samplesPerSegment;
                    Vector2 next = center + Direction(angle) * radius;
                    DrawLine(texture, previous, next, thickness, color);
                    previous = next;
                }
            }
        }

        private static void DrawJaggedBolt(Texture2D texture, Vector2 start, Vector2 end, int seed,
            int segmentCount, int startWidth, bool whiteRoot)
        {
            var random = new System.Random(seed);
            Vector2 direction = end - start;
            Vector2 perpendicular = direction.sqrMagnitude > 0.001f
                ? new Vector2(-direction.y, direction.x).normalized
                : Vector2.up;
            var points = new List<Vector2> { start };
            for (int segment = 1; segment < segmentCount; segment++)
            {
                float t = segment / (float)segmentCount;
                float irregularT = Mathf.Clamp01(t + ((float)random.NextDouble() - 0.5f) * 0.16f);
                float offset = ((float)random.NextDouble() - 0.5f) * direction.magnitude * 0.24f;
                points.Add(Vector2.Lerp(start, end, irregularT) + perpendicular * offset);
            }
            points.Add(end);

            for (int index = 0; index < points.Count - 1; index++)
            {
                int width = Mathf.Max(1, startWidth - index);
                int outlineWidth = index == points.Count - 2 ? width : width + 2;
                DrawLine(texture, points[index], points[index + 1], outlineWidth, Deep);
                DrawLine(texture, points[index], points[index + 1], width, index == 0 && whiteRoot ? Pale : Vivid);
            }
        }

        private static void DrawCircle(Texture2D texture, Vector2 center, int radius, Color color)
        {
            int minX = Mathf.FloorToInt(center.x - radius);
            int maxX = Mathf.CeilToInt(center.x + radius);
            int minY = Mathf.FloorToInt(center.y - radius);
            int maxY = Mathf.CeilToInt(center.y + radius);
            int radiusSquared = radius * radius;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = Mathf.RoundToInt(x - center.x);
                    int dy = Mathf.RoundToInt(y - center.y);
                    if (dx * dx + dy * dy <= radiusSquared) SetPixel(texture, x, y, color);
                }
        }

        private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, int width, Color color)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance * 1.5f));
            for (int step = 0; step <= steps; step++)
            {
                Vector2 point = Vector2.Lerp(start, end, step / (float)steps);
                DrawCircle(texture, point, Mathf.Max(1, width), color);
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height) return;
            texture.SetPixel(x, y, color);
        }

        private static Texture2D NewTexture()
        {
            var texture = new Texture2D(CanvasSize, CanvasSize, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[CanvasSize * CanvasSize];
            Array.Fill(pixels, Color.clear);
            texture.SetPixels(pixels);
            return texture;
        }

        private static void Save(Texture2D texture, string phaseFolder, string phase, int frame)
        {
            texture.Apply(false, false);
            string assetPath = $"{Root}/{phaseFolder}/spr_vfx_arca_lightningorb_{phase}_{frame:00}.png";
            File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ConfigureTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                importer.spritePixelsPerUnit = importedTexture != null
                    ? Mathf.Max(1f, importedTexture.width)
                    : PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        private static void CreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP 2D Sprite-Unlit-Default shader was not found.");
            material = new Material(shader) { name = "mat_vfx_lightning_sprite_unlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }

        private static Vector2 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static Vector2 Center => new(CanvasSize * 0.5f, CanvasSize * 0.5f);
    }
}
