using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class HonokaIdleSetup
    {
        private const string Root = "Assets/Characters/Honoka/Pixel64/Resources/Characters/Honoka";
        private const string FrameRoot = Root + "/Idle";
        private const string WalkFrameRoot = Root + "/Walk";
        private const string AttackFrameRoot = Root + "/Attack";
        private const string ClipPath = Root + "/Animations/Honoka_Idle.anim";
        private const string WalkClipPath = Root + "/Animations/Honoka_Walk.anim";
        private const string AttackClipPath = Root + "/Animations/Honoka_Attack.anim";
        private const string ControllerPath = Root + "/Animations/Honoka.controller";
        private const int FrameCount = 7;
        private const float FrameRate = 7f;
        private const int WalkFrameCount = 9;
        private const float WalkFrameRate = 9f;
        private const int AttackFrameCount = 9;
        private const float AttackFrameRate = 10f;

        [MenuItem("Tools/2D Character/Honoka/Apply Approved South Idle")]
        public static void Apply()
        {
            Directory.CreateDirectory(Root + "/Animations");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Sprite[] sprites = new Sprite[FrameCount];
            for (int index = 0; index < FrameCount; index++)
            {
                string path = $"{FrameRoot}/Honoka_Idle_{index:00}.png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException($"Honoka Idle frame is missing: {path}");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                TextureImporterSettings textureSettings = new();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteAlignment = (int)SpriteAlignment.Center;
                textureSettings.spritePivot = new Vector2(0.5f, 0.5f);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Honoka_Idle" };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            clip.frameRate = FrameRate;
            EditorCurveBinding binding = new()
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[FrameCount + 1];
            for (int index = 0; index < FrameCount; index++)
                keys[index] = new ObjectReferenceKeyframe { time = index / FrameRate, value = sprites[index] };
            keys[^1] = new ObjectReferenceKeyframe { time = FrameCount / FrameRate, value = sprites[0] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Skill", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "SkillTwo", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Ultimate", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Death", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "Idle") ?? stateMachine.AddState("Idle");
            idle.motion = clip;
            AnimationClip walkClip = BuildWalkClip();
            AnimatorState walk = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "Walk") ?? stateMachine.AddState("Walk");
            walk.motion = walkClip;
            EnsureBoolTransition(idle, walk, "IsMoving", true);
            EnsureBoolTransition(walk, idle, "IsMoving", false);
            AnimationClip attackClip = BuildAttackClip();
            AnimatorState attack = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "Attack") ?? stateMachine.AddState("Attack");
            attack.motion = attackClip;
            EnsureTriggerTransition(stateMachine, attack, "Attack");
            EnsureExitTransition(attack, idle);
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[HonokaIdleSetup] Applied Idle {0}, Walk {1}, and Attack {2} frames.", FrameCount, WalkFrameCount, AttackFrameCount);
        }

        private static AnimationClip BuildAttackClip()
        {
            Sprite[] sprites = LoadSprites(AttackFrameRoot, "Honoka_Attack", AttackFrameCount);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Honoka_Attack" };
                AssetDatabase.CreateAsset(clip, AttackClipPath);
            }
            clip.frameRate = AttackFrameRate;
            SetSpriteCurve(clip, sprites, AttackFrameRate, false);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static Sprite[] LoadSprites(string frameRoot, string prefix, int frameCount)
        {
            Sprite[] sprites = new Sprite[frameCount];
            for (int index = 0; index < frameCount; index++)
            {
                string path = $"{frameRoot}/{prefix}_{index:00}.png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException($"Honoka animation frame is missing: {path}");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                TextureImporterSettings settings = new();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprites;
        }

        private static void SetSpriteCurve(AnimationClip clip, Sprite[] sprites, float frameRate, bool loop)
        {
            EditorCurveBinding binding = new()
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
            for (int index = 0; index < sprites.Length; index++)
                keys[index] = new ObjectReferenceKeyframe { time = index / frameRate, value = sprites[index] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void EnsureTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string parameter)
        {
            AnimatorStateTransition transition = stateMachine.anyStateTransitions.FirstOrDefault(item =>
                item.destinationState == destination && item.conditions.Any(condition => condition.parameter == parameter));
            if (transition == null) transition = stateMachine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0.02f;
            transition.canTransitionToSelf = false;
            transition.conditions = System.Array.Empty<AnimatorCondition>();
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void EnsureExitTransition(AnimatorState source, AnimatorState destination)
        {
            AnimatorStateTransition transition = source.transitions.FirstOrDefault(item =>
                item.destinationState == destination && item.conditions.Length == 0);
            if (transition == null) transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0.04f;
            transition.conditions = System.Array.Empty<AnimatorCondition>();
        }

        private static AnimationClip BuildWalkClip()
        {
            Sprite[] sprites = new Sprite[WalkFrameCount];
            for (int index = 0; index < WalkFrameCount; index++)
            {
                string path = $"{WalkFrameRoot}/Honoka_Walk_{index:00}.png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException($"Honoka Walk frame is missing: {path}");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                TextureImporterSettings textureSettings = new();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteAlignment = (int)SpriteAlignment.Center;
                textureSettings.spritePivot = new Vector2(0.5f, 0.5f);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
            if (walkClip == null)
            {
                walkClip = new AnimationClip { name = "Honoka_Walk" };
                AssetDatabase.CreateAsset(walkClip, WalkClipPath);
            }
            walkClip.frameRate = WalkFrameRate;
            EditorCurveBinding binding = new()
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[WalkFrameCount + 1];
            for (int index = 0; index < WalkFrameCount; index++)
                keys[index] = new ObjectReferenceKeyframe { time = index / WalkFrameRate, value = sprites[index] };
            keys[^1] = new ObjectReferenceKeyframe { time = WalkFrameCount / WalkFrameRate, value = sprites[0] };
            AnimationUtility.SetObjectReferenceCurve(walkClip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(walkClip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(walkClip, settings);
            EditorUtility.SetDirty(walkClip);
            return walkClip;
        }

        private static void EnsureBoolTransition(AnimatorState source, AnimatorState destination, string parameter, bool value)
        {
            AnimatorStateTransition transition = source.transitions.FirstOrDefault(item =>
                item.destinationState == destination && item.conditions.Any(condition => condition.parameter == parameter));
            if (transition == null) transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0.05f;
            transition.conditions = System.Array.Empty<AnimatorCondition>();
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            if (controller.parameters.Any(parameter => parameter.name == parameterName)) return;
            controller.AddParameter(parameterName, parameterType);
        }
    }
}
