using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaComfyIdleSetup
    {
        private const string Root = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca";
        private const string FrameRoot = Root + "/IdleComfyV1";
        private const string ClipPath = Root + "/Animations/Arca_Idle_ComfyV1.anim";
        private const string ControllerPath = Root + "/Animations/Arca_Idle.controller";
        private const int FrameCount = 6;
        private const float FrameRate = 6f;

        [MenuItem("Tools/2D Character/Arca Pixel64/Apply Comfy Idle V1")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var sprites = new Sprite[FrameCount];
            for (var index = 0; index < sprites.Length; index++)
            {
                string path = $"{FrameRoot}/character_arca_idle_comfy_{index + 1:00}.png";
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[index] == null)
                    throw new FileNotFoundException($"Comfy Idle sprite is missing: {path}");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Arca_Idle_ComfyV1" };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.frameRate = FrameRate;
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (var index = 0; index < sprites.Length; index++)
                keys[index] = new ObjectReferenceKeyframe { time = index / FrameRate, value = sprites[index] };
            keys[^1] = new ObjectReferenceKeyframe { time = sprites.Length / FrameRate, value = sprites[0] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                throw new FileNotFoundException($"Animator controller is missing: {ControllerPath}");

            AnimatorState idleState = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(state => state.name == "Idle");
            if (idleState == null)
                throw new InvalidDataException("Arca Animator controller has no Idle state.");

            idleState.motion = clip;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[ArcaComfyIdleSetup] Applied {0} Comfy-guided Idle frames at {1} FPS.", FrameCount, FrameRate);
        }
    }
}
