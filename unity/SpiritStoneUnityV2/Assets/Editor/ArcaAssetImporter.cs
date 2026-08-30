using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public sealed class ArcaAssetImporter : AssetPostprocessor
    {
        private const string ArcaSourceRoot = "Assets/Characters/Arca/Source/";
        private const string ArcaRigRoot = "Assets/Characters/Arca/Rig/";
        private const string ArcaPixel64Root = "Assets/Characters/Arca/Pixel64/";
        private const string ArcaAnimationRoot = "Assets/Characters/Arca/Animations/";

        private void OnPreprocessTexture()
        {
            bool isProductionAnimation = assetPath.StartsWith(ArcaAnimationRoot) &&
                (assetPath.Contains("/RuntimeFrames/") || assetPath.Contains("/Runtime/"));
            bool isPixel64 = assetPath.StartsWith(ArcaPixel64Root) || isProductionAnimation;
            bool isPixelEffect = isPixel64 && assetPath.Contains("/Effects/");
            if (!assetPath.StartsWith(ArcaSourceRoot) && !assetPath.StartsWith(ArcaRigRoot) && !isPixel64) return;
            if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = isPixel64 ? 32f : 256f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = isPixel64 ? FilterMode.Point : FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePivot = isPixelEffect
                ? new Vector2(0.5f, 0.5f)
                : isPixel64 ? new Vector2(0.5f, 2f / 64f) : new Vector2(0.5f, 0.5f);
        }
    }
}
