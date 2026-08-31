using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaLightningOrbSetup
    {
        private const string EffectRoot = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/Effects/LightningOrbV1";

        [MenuItem("Tools/2D Character/Arca Pixel64/Configure Lightning Orb V1")]
        public static void Configure()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { EffectRoot });
            foreach (string textureGuid in textureGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(textureGuid);
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[ArcaLightningOrbSetup] Configured {0} Lightning Orb textures at PPU 32.", textureGuids.Length);
        }
    }
}
