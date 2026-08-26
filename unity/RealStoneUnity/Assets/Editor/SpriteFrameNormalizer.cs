using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealStone.Editor
{
    public static class SpriteFrameNormalizer
    {
        private const string SourceFolder = "Assets/Resources/Art/Golem/Attack";
        private const string OutputFolder = "Assets/Resources/ArtNormalized/Golem/Attack";

        private const int OutputWidth = 320;
        private const int OutputHeight = 256;
        private const int TargetContentHeight = 165;

        public static void EnsureNormalizedFrames()
        {
            Directory.CreateDirectory(OutputFolder);
            for (var i = 0; i < 8; i++)
            {
                var source = $"{SourceFolder}/golem_attack_v2_{i}.png";
                var output = $"{OutputFolder}/golem_attack_v2_{i}.png";
                Normalize(source, output);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void Normalize(string sourcePath, string outputPath)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(sourcePath));
            var pixels = source.GetPixels32();
            var minX = source.width;
            var minY = source.height;
            var maxX = -1;
            var maxY = -1;
            for (var y = 0; y < source.height; y++)
            for (var x = 0; x < source.width; x++)
            {
                if (pixels[y * source.width + x].a <= 8) continue;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
            if (maxX < minX || maxY < minY)
            {
                Object.DestroyImmediate(source);
                return;
            }

            var contentWidth = maxX - minX + 1;
            var contentHeight = maxY - minY + 1;
            var scale = (float)TargetContentHeight / contentHeight;
            var scaledWidth = Mathf.Min(OutputWidth - 10, Mathf.RoundToInt(contentWidth * scale));
            var scaledHeight = TargetContentHeight;
            var startX = (OutputWidth - scaledWidth) / 2;
            const int baseline = 10;
            var outputPixels = new Color32[OutputWidth * OutputHeight];

            for (var y = 0; y < scaledHeight; y++)
            for (var x = 0; x < scaledWidth; x++)
            {
                var sourceX = minX + Mathf.Clamp(Mathf.FloorToInt(x / scale), 0, contentWidth - 1);
                var sourceY = minY + Mathf.Clamp(Mathf.FloorToInt(y / scale), 0, contentHeight - 1);
                var targetX = startX + x;
                var targetY = baseline + y;
                if (targetX >= 0 && targetX < OutputWidth && targetY >= 0 && targetY < OutputHeight)
                    outputPixels[targetY * OutputWidth + targetX] = pixels[sourceY * source.width + sourceX];
            }

            var output = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
            output.name = Path.GetFileNameWithoutExtension(outputPath);
            output.filterMode = FilterMode.Point;
            output.SetPixels32(outputPixels);
            output.Apply();
            File.WriteAllBytes(outputPath, output.EncodeToPNG());
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(output);
        }
    }
}
