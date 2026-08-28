using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public sealed class ArcaAssetImporter : AssetPostprocessor
    {
        private const string ArcaSourceRoot = "Assets/Characters/Arca/Source/";
        private const string ArcaRigRoot = "Assets/Characters/Arca/Rig/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArcaSourceRoot) && !assetPath.StartsWith(ArcaRigRoot)) return;
            if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
        }
    }
}
