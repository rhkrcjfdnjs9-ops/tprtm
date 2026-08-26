using UnityEditor;
using UnityEngine;

namespace RealStone.Editor
{
    public sealed class RealStoneAssetImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("Assets/Resources/Art") &&
                !assetPath.Contains("Assets/ArtSource/GraniaRig/Layers/") &&
                !assetPath.Contains("Assets/Characters/Grania/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = assetPath.Contains("Assets/ArtSource/GraniaRig/Layers/") ||
                                  assetPath.Contains("Assets/Characters/Grania/")
                ? FilterMode.Bilinear
                : FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.Contains("Assets/Resources/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.DecompressOnLoad,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.7f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                preloadAudioData = true,
            };
        }
    }
}
