using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaFloatMoveSetup
    {
        private const string AnimationRoot = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/Animations";
        private const string FrameRoot = "Assets/Characters/Arca/PixelLab/WalkSouthEastPaletteLockedV2/Normalized64";
        private const string ClipPath = AnimationRoot + "/Arca_Walk.anim";
        private const string ControllerPath = AnimationRoot + "/Arca_Idle.controller";
        private const int FrameCount = 7;
        private const float FrameRate = 9f;

        [MenuItem("Tools/2D Character/Arca Pixel64/Rebuild Walk From PixelLab South-East Master")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(AnimationRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var sprites = new Sprite[FrameCount];
            for (var i = 0; i < sprites.Length; i++)
            {
                var path = $"{FrameRoot}/Arca_Walk_SouthEast_PaletteLocked_V2_64_{i:00}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32f;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.spritePivot = new Vector2(0.5f, 2f / 64f);
                    importer.SaveAndReimport();
                }
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[i] == null)
                    throw new FileNotFoundException($"Walk sprite is missing: {path}");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Arca_Walk" };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            clip.frameRate = FrameRate;
            var binding = new EditorCurveBinding
            {
                path = "",
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (var i = 0; i < sprites.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = sprites[i] };
            keys[^1] = new ObjectReferenceKeyframe { time = sprites.Length / clip.frameRate, value = sprites[0] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                throw new FileNotFoundException($"Animator controller is missing: {ControllerPath}");

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var state in stateMachine.states)
            {
                if (state.state.name != "Walk" && state.state.name != "FloatMove") continue;
                state.state.name = "Walk";
                state.state.motion = clip;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[ArcaFloatMoveSetup] Applied {0} normalized PixelLab South-East frames as ground Walk at {1} FPS.", FrameCount, FrameRate);
        }
    }
}
