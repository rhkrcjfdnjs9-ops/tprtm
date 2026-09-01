using UnityEditor;
using UnityEngine;
using SpiritStone.Prototype;

namespace SpiritStone.Editor.VFX
{
    public static class ArcaOverchargeVfxImporter
    {
        private const string Root = "Assets/VFX/Resources/VFX/Lightning/OverchargeV3";

        [MenuItem("Tools/SpiritStone/VFX/Configure Arca Overcharge Rear Aura V3")]
        public static void Configure()
        {
            AssetDatabase.Refresh();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = texture != null ? Mathf.Max(1f, texture.width) : 64f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.LogFormat("[ArcaOverchargeVfxImporter] Configured {0} rear-aura V3 frames.", guids.Length);
        }

        [MenuItem("Tools/SpiritStone/VFX/Preview Arca Overcharge Rear Aura V3")]
        public static void Preview()
        {
            IdleBattlePrototype battle = Object.FindFirstObjectByType<IdleBattlePrototype>();
            if (battle == null)
            {
                Debug.LogWarningFormat("[ArcaOverchargeVfxImporter] Enter Play Mode before previewing Overcharge.");
                return;
            }
            battle.PreviewArcaOverchargeVfx();
        }

        [MenuItem("Tools/SpiritStone/VFX/Preview Arca Overcharge Rear Aura V3", true)]
        private static bool CanPreview() => EditorApplication.isPlaying;
    }
}
