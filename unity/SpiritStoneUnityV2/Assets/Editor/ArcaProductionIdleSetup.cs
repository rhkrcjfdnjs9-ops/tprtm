using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaProductionIdleSetup
    {
        private const string FrameRoot = "Assets/Characters/Arca/Animations/Idle/RuntimeFrames";
        private const string ClipPath = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/Animations/Arca_Idle_V14.anim";
        private const string ControllerPath = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/Animations/Arca_Idle.controller";
        private const int FrameCount = 6;
        private const float FrameRate = 8f;

        [MenuItem("Tools/2D Character/Arca Pixel64/Apply Production Idle V14")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var sprites = new Sprite[FrameCount];
            for (var index = 0; index < FrameCount; index++)
            {
                string path = $"{FrameRoot}/Arca_Idle_{index:00}.png";
                AssetDatabase.ImportAsset(
                    path,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[index] == null)
                    throw new FileNotFoundException($"Production Idle sprite is missing: {path}");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Arca_Idle_V14" };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.frameRate = FrameRate;
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = new ObjectReferenceKeyframe[FrameCount + 1];
            for (var index = 0; index < FrameCount; index++)
                keys[index] = new ObjectReferenceKeyframe { time = index / FrameRate, value = sprites[index] };
            keys[FrameCount] = new ObjectReferenceKeyframe { time = FrameCount / FrameRate, value = sprites[0] };
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
            Debug.LogFormat("[ArcaProductionIdleSetup] Applied {0} production Idle frames at {1} FPS.", FrameCount, FrameRate);
        }
    }
}
