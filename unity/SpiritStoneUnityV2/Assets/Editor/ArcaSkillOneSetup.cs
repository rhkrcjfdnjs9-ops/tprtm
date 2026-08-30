using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class ArcaSkillOneSetup
    {
        private const string Root = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca";
        private const string FrameRoot = "Assets/Characters/Arca/PixelLab/SkillSouthEastPixelLabV4Review";
        private const string ClipPath = Root + "/Animations/Arca_SkillOne.anim";
        private const string ControllerPath = Root + "/Animations/Arca_Idle.controller";
        private const int FrameCount = 9;
        private const float FrameRate = 12f;

        [MenuItem("Tools/2D Character/Arca Pixel64/Rebuild South-East Skill One V4")]
        public static void Rebuild()
        {
            var sprites = new Sprite[FrameCount];
            for (var index = 0; index < FrameCount; index++)
            {
                string path = $"{FrameRoot}/Arca_Skill_SE_PixelLab_V4_{index:00}.png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException($"Skill-one sprite is missing: {path}");

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
                if (sprites[index] == null) throw new FileNotFoundException($"Skill-one sprite failed to import: {path}");
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) != null)
                AssetDatabase.DeleteAsset(ClipPath);

            var clip = new AnimationClip { frameRate = FrameRate, name = "Arca_SkillOne" };
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
            if (!controller.parameters.Any(parameter => parameter.name == "Skill"))
                controller.AddParameter("Skill", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.states.Select(child => child.state)
                .First(state => state.name == "Idle");
            AnimatorState skill = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "Skill");
            if (skill == null) skill = stateMachine.AddState("Skill", new Vector3(420f, -120f, 0f));
            skill.motion = clip;

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
                if (transition.destinationState == skill) stateMachine.RemoveAnyStateTransition(transition);
            AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(skill);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Skill");
            enter.duration = 0f;
            enter.hasExitTime = false;

            foreach (AnimatorStateTransition transition in skill.transitions.ToArray())
                skill.RemoveTransition(transition);
            AnimatorStateTransition exit = skill.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 1f;
            exit.duration = 0f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[ArcaSkillOneSetup] Connected {0} South-East skill-one frames at {1} FPS.", FrameCount, FrameRate);
        }
    }
}
