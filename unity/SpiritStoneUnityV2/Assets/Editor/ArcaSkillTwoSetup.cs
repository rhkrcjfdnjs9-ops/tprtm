using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaSkillTwoSetup
    {
        private const string Root = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca";
        private const string FrameRoot = "Assets/Characters/Arca/PixelLab/SkillTwoOverchargeSouthEastV2";
        private const string ClipPath = Root + "/Animations/Arca_SkillTwo_Overcharge.anim";
        private const string ControllerPath = Root + "/Animations/Arca_Idle.controller";
        private const int FrameCount = 8;
        private const float FrameRate = 12f;
        private static readonly string[] FrameNames =
        {
            "Arca_Overcharge_V2_00_Idle.png",
            "Arca_Overcharge_V2_01_Anticipation.png",
            "Arca_Overcharge_V2_02_Gather.png",
            "Arca_Overcharge_V2_03_CommandReady.png",
            "Arca_Overcharge_V2_04_Command.png",
            "Arca_Overcharge_V2_05_Peak.png",
            "Arca_Overcharge_V2_06_Recoil.png",
            "Arca_Overcharge_V2_07_Recover.png"
        };

        [MenuItem("Tools/2D Character/Arca Pixel64/Rebuild Skill Two Overcharge V2")]
        public static void Rebuild()
        {
            var sprites = new Sprite[FrameCount];
            for (var index = 0; index < FrameCount; index++)
            {
                string path = $"{FrameRoot}/{FrameNames[index]}";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException($"Skill-two sprite is missing: {path}");

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
                if (sprites[index] == null) throw new FileNotFoundException($"Skill-two sprite failed to import: {path}");
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) != null)
                AssetDatabase.DeleteAsset(ClipPath);

            var clip = new AnimationClip { frameRate = FrameRate, name = "Arca_SkillTwo_Overcharge" };
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
            if (!controller.parameters.Any(parameter => parameter.name == "SkillTwo"))
                controller.AddParameter("SkillTwo", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.states.Select(child => child.state)
                .First(state => state.name == "Idle");
            AnimatorState skillTwo = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "SkillTwo");
            if (skillTwo == null) skillTwo = stateMachine.AddState("SkillTwo", new Vector3(620f, -120f, 0f));
            skillTwo.motion = clip;

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
                if (transition.destinationState == skillTwo) stateMachine.RemoveAnyStateTransition(transition);
            AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(skillTwo);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "SkillTwo");
            enter.duration = 0f;
            enter.hasExitTime = false;

            foreach (AnimatorStateTransition transition in skillTwo.transitions.ToArray())
                skillTwo.RemoveTransition(transition);
            AnimatorStateTransition exit = skillTwo.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 1f;
            exit.duration = 0f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[ArcaSkillTwoSetup] Connected {0} overcharge V2 frames at {1} FPS.", FrameCount, FrameRate);
        }
    }
}
