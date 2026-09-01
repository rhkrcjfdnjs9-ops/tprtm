using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SpiritStone.Editor.VFX
{
    public static class VfxUrp2DProjectConfigurator
    {
        private const string SettingsFolder = "Assets/Settings/VFX";
        private const string RendererPath = SettingsFolder + "/SpiritStone_Renderer2D.asset";
        private const string PipelinePath = SettingsFolder + "/SpiritStone_URP2D.asset";

        [MenuItem("Tools/SpiritStone/VFX/Configure URP 2D")]
        public static void Configure()
        {
            EnsureFolder("Assets", "Settings");
            EnsureFolder("Assets/Settings", "VFX");

            Renderer2DData rendererData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                rendererData.name = "SpiritStone_Renderer2D";
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            UniversalRenderPipelineAsset pipelineAsset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                pipelineAsset.name = "SpiritStone_URP2D";
                AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = null;

            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(pipelineAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.LogFormat(
                "[VfxUrp2DProjectConfigurator] URP 2D configured. Pipeline={0}, Renderer={1}",
                PipelinePath,
                RendererPath);
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            string path = parentFolder + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }
    }
}
