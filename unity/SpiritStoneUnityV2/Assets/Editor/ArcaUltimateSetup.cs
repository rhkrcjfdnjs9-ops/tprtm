using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaUltimateSetup
    {
        private const string Root = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca";
        private const string FrameRoot = "Assets/Characters/Arca/PixelLab/UltimateSouthEastV1/Final12";
        private const string ClipPath = Root + "/Animations/Arca_Ultimate.anim";
        private const string ControllerPath = Root + "/Animations/Arca_Idle.controller";
        private const int FrameCount = 12;
        private const float FrameRate = 12f;

        private static readonly string[] FrameNames =
        {
            "Arca_Ultimate_00_Start.png",
            "Arca_Ultimate_01_Rise_01.png",
            "Arca_Ultimate_02_Rise_02.png",
            "Arca_Ultimate_03_Rise_03.png",
            "Arca_Ultimate_04_Rise_04.png",
            "Arca_Ultimate_05_Rise_05.png",
            "Arca_Ultimate_06_Peak.png",
            "Arca_Ultimate_07_Return_01.png",
            "Arca_Ultimate_08_Return_02.png",
            "Arca_Ultimate_09_Return_03.png",
            "Arca_Ultimate_10_Return_04.png",
            "Arca_Ultimate_11_End.png"
        };

        [MenuItem("Tools/2D Character/Arca Pixel64/Rebuild Ultimate V1")]
        public static void Rebuild()
        {
            var sprites = new Sprite[FrameCount];
            for (var index = 0; index < FrameCount; index++)
            {
                string path = $"{FrameRoot}/{FrameNames[index]}";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException($"Ultimate sprite is missing: {path}");

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

                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[index] == null) throw new FileNotFoundException($"Ultimate sprite failed to import: {path}");
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) != null)
                AssetDatabase.DeleteAsset(ClipPath);

            var clip = new AnimationClip { frameRate = FrameRate, name = "Arca_Ultimate" };
            var binding = new EditorCurveBinding
            {
                path = "",
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = new ObjectReferenceKeyframe[FrameCount];
            for (var index = 0; index < FrameCount; index++)
                keys[index] = new ObjectReferenceKeyframe { time = index / FrameRate, value = sprites[index] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, ClipPath);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new FileNotFoundException($"Controller is missing: {ControllerPath}");
            if (!controller.parameters.Any(parameter => parameter.name == "Ultimate"))
                controller.AddParameter("Ultimate", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.states.Select(child => child.state)
                .First(state => state.name == "Idle");
            AnimatorState ultimate = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "Ultimate");
            if (ultimate == null) ultimate = stateMachine.AddState("Ultimate", new Vector3(820f, -120f, 0f));
            ultimate.motion = clip;

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
                if (transition.destinationState == ultimate) stateMachine.RemoveAnyStateTransition(transition);
            AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(ultimate);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Ultimate");
            enter.duration = 0f;
            enter.hasExitTime = false;

            foreach (AnimatorStateTransition transition in ultimate.transitions.ToArray())
                ultimate.RemoveTransition(transition);
            AnimatorStateTransition exit = ultimate.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 1f;
            exit.duration = 0f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[ArcaUltimateSetup] Connected {0} ultimate frames at {1} FPS.", FrameCount, FrameRate);
        }
    }
}
