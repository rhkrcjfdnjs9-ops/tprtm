using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaPixelLabIdleSetup
    {
        private const string Root = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca";
        private const string FrameRoot = Root + "/IdlePixelLabV2";
        private const string ClipPath = Root + "/Animations/Arca_Idle_PixelLabV2.anim";
        private const string ControllerPath = Root + "/Animations/Arca_Idle.controller";
        private const string PrototypeScenePath = "Assets/Scenes/IdleBattlePrototype.unity";
        private const int FrameCount = 7;
        private const float FrameRate = 7f;

        [MenuItem("Tools/2D Character/Arca Pixel64/Apply PixelLab Idle V2")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var sprites = new Sprite[FrameCount];
            for (var index = 0; index < sprites.Length; index++)
            {
                string path = $"{FrameRoot}/Arca_Idle_Front_V2_{index:00}.png";
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[index] == null)
                    throw new FileNotFoundException($"PixelLab Idle sprite is missing: {path}");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Arca_Idle_PixelLabV2" };
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
            Debug.LogFormat("[ArcaPixelLabIdleSetup] Applied {0} PixelLab Idle frames at {1} FPS.", FrameCount, FrameRate);
        }

        public static void OpenPrototypeAndPlay()
        {
            Apply();
            PortraitGameViewSetup.Apply();
            EditorSceneManager.OpenScene(PrototypeScenePath);
            EditorApplication.isPlaying = true;
            Debug.LogFormat("[ArcaPixelLabIdleSetup] Opened {0} and entered Play Mode.", PrototypeScenePath);
        }
    }
}
