using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaHitSetup
    {
        private const string Root = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca";
        private const string FrameRoot = "Assets/Characters/Arca/PixelLab/HitSouthEastMasterLockedV2";
        private const string ClipPath = Root + "/Animations/Arca_Hit.anim";
        private const string ControllerPath = Root + "/Animations/Arca_Idle.controller";

        [MenuItem("Tools/2D Character/Arca Pixel64/Rebuild South-East Hit V2")]
        public static void Rebuild()
        {
            var sprites = new Sprite[6];
            for (var index = 0; index < sprites.Length; index++)
            {
                string path = $"{FrameRoot}/Arca_Hit_SouthEast_MasterLocked_V2_{index:00}.png";
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

                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[index] == null) throw new FileNotFoundException($"Hit sprite is missing: {path}");
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) != null) AssetDatabase.DeleteAsset(ClipPath);
            var clip = new AnimationClip { frameRate = 12f, name = "Arca_Hit" };
            var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
            var keys = new ObjectReferenceKeyframe[sprites.Length];
            for (var index = 0; index < sprites.Length; index++)
                keys[index] = new ObjectReferenceKeyframe { time = index / clip.frameRate, value = sprites[index] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            AssetDatabase.CreateAsset(clip, ClipPath);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new FileNotFoundException($"Controller is missing: {ControllerPath}");
            if (!controller.parameters.Any(parameter => parameter.name == "Hit"))
                controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.states.Select(child => child.state).First(state => state.name == "Idle");
            AnimatorState hit = stateMachine.states.Select(child => child.state).FirstOrDefault(state => state.name == "Hit");
            if (hit == null) hit = stateMachine.AddState("Hit", new Vector3(420f, 120f, 0f));
            hit.motion = clip;

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
                if (transition.destinationState == hit) stateMachine.RemoveAnyStateTransition(transition);
            AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(hit);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            enter.duration = 0f;
            enter.hasExitTime = false;

            foreach (AnimatorStateTransition transition in hit.transitions.ToArray()) hit.RemoveTransition(transition);
            AnimatorStateTransition exit = hit.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 1f;
            exit.duration = 0f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[ArcaHitSetup] Connected {0} South-East hit V2 frames at {1} FPS with looping disabled.", sprites.Length, clip.frameRate);
        }
    }
}
