using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaGifHoverIdleSetup
    {
        private const string Root = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca";
        private const string FrameRoot = Root + "/IdleHoverGifV1";
        private const string ClipPath = Root + "/Animations/Arca_Idle_HoverGifV1.anim";
        private const string ControllerPath = Root + "/Animations/Arca_Idle.controller";
        private const int FrameCount = 24;
        private const float SecondsPerFrame = 0.07f;

        [MenuItem("Tools/2D Character/Arca Pixel64/Apply GIF Hover Idle V1")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var sprites = new Sprite[FrameCount];
            for (var index = 0; index < sprites.Length; index++)
            {
                string path = $"{FrameRoot}/Arca_Idle_Hover_V1_{index:00}.png";
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[index] == null)
                    throw new FileNotFoundException($"GIF Hover Idle sprite is missing: {path}");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Arca_Idle_HoverGifV1" };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.frameRate = 1f / SecondsPerFrame;
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (var index = 0; index < sprites.Length; index++)
                keys[index] = new ObjectReferenceKeyframe { time = index * SecondsPerFrame, value = sprites[index] };
            keys[^1] = new ObjectReferenceKeyframe { time = sprites.Length * SecondsPerFrame, value = sprites[0] };
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
            Debug.LogFormat("[ArcaGifHoverIdleSetup] Applied {0} Hover Idle frames at {1:F2} FPS.", FrameCount, clip.frameRate);
        }
    }
}
