using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealStone.Editor.CharacterProduction
{
    public static class GraniaExactHeadExtractor
    {
        private const string SourcePath = "Assets/CharacterSource/GraniaProduction/GraniaProduction_Master_Standard512.png";
        private const string OutputFolder = "Assets/CharacterSource/GraniaExactParts/Head";

        [MenuItem("Tools/2D Character/Extract Exact Grania Head Pixels")]
        public static void Extract()
        {
            var source = LoadPng(SourcePath);
            if (source.width != 512 || source.height != 512)
                throw new InvalidDataException($"Expected 512x512 source, got {source.width}x{source.height}");

            var pixels = source.GetPixels32();
            var face = EmptyPixels();
            var frontHair = EmptyPixels();
            var backHair = EmptyPixels();
            var reference = EmptyPixels();
            var recomposed = EmptyPixels();

            for (var y = 0; y < 512; y++)
            for (var x = 0; x < 512; x++)
            {
                var index = y * 512 + x;
                var color = pixels[index];
                if (color.a <= 20) continue;

                var isFace = InEllipse(x, y, 256f, 343f, 31f, 31f) && IsSkin(color);
                var isFrontHair = IsSilverHair(color) && IsFrontHairRegion(x, y);
                var isBackHair = IsSilverHair(color) && IsBackHairRegion(x, y);

                // Every visible source pixel belongs to at most one layer.
                if (isFrontHair)
                    frontHair[index] = color;
                else if (isFace)
                    face[index] = color;
                else if (isBackHair)
                    backHair[index] = color;
                else
                    continue;

                reference[index] = color;
                recomposed[index] = color;
            }

            Directory.CreateDirectory(OutputFolder);
            SavePng(face, OutputFolder + "/FaceVisible.png");
            SavePng(frontHair, OutputFolder + "/FrontHairVisible.png");
            SavePng(backHair, OutputFolder + "/BackHairVisible.png");
            SavePng(reference, OutputFolder + "/HeadReferenceCutout.png");
            SavePng(recomposed, OutputFolder + "/HeadRecomposed.png");
            SaveComparison(reference, recomposed, OutputFolder + "/HeadComparison.png");

            Object.DestroyImmediate(source);
            AssetDatabase.Refresh();
            foreach (var path in new[]
                     {
                         OutputFolder + "/FaceVisible.png", OutputFolder + "/FrontHairVisible.png",
                         OutputFolder + "/BackHairVisible.png", OutputFolder + "/HeadReferenceCutout.png",
                         OutputFolder + "/HeadRecomposed.png", OutputFolder + "/HeadComparison.png"
                     })
                CharacterProductionUtility.ApplyImportSettings(path, true);
            Debug.Log("GRANIA_EXACT_HEAD_EXTRACTED: " + OutputFolder);
        }

        private static bool IsFrontHairRegion(int x, int y)
        {
            if (InEllipse(x, y, 256f, 382f, 43f, 35f)) return true;
            if (x >= 218 && x <= 242 && y >= 326 && y <= 391) return true;
            if (x >= 270 && x <= 294 && y >= 326 && y <= 391) return true;
            return false;
        }

        private static bool IsBackHairRegion(int x, int y)
        {
            if (x >= 181 && x <= 230 && y >= 238 && y <= 367) return true;
            if (x >= 282 && x <= 331 && y >= 238 && y <= 367) return true;
            if (x >= 213 && x <= 299 && y >= 302 && y <= 407) return true;
            return false;
        }

        private static bool IsSkin(Color32 color)
        {
            return color.r > 150 && color.g > 85 && color.b > 65 &&
                   color.r > color.g + 12 && color.g >= color.b - 12;
        }

        private static bool IsSilverHair(Color32 color)
        {
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            return max >= 105 && min >= 70 && max - min <= 58 &&
                   color.b >= color.r - 24 && color.b <= color.r + 55;
        }

        private static bool InEllipse(float x, float y, float centerX, float centerY, float radiusX, float radiusY)
        {
            var dx = (x - centerX) / radiusX;
            var dy = (y - centerY) / radiusY;
            return dx * dx + dy * dy <= 1f;
        }

        private static Texture2D LoadPng(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.GetFullPath(assetPath)), false))
                throw new InvalidDataException("Failed to load " + assetPath);
            return texture;
        }

        private static Color32[] EmptyPixels() => new Color32[512 * 512];

        private static void SavePng(Color32[] pixels, string assetPath)
        {
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void SaveComparison(Color32[] reference, Color32[] recomposed, string assetPath)
        {
            var texture = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var combined = new Color32[1024 * 512];
            for (var y = 0; y < 512; y++)
            for (var x = 0; x < 512; x++)
            {
                combined[y * 1024 + x] = reference[y * 512 + x];
                combined[y * 1024 + 512 + x] = recomposed[y * 512 + x];
            }
            texture.SetPixels32(combined);
            texture.Apply(false, false);
            File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
    }
}
