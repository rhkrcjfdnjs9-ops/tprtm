using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RealStone.Editor
{
    public static class AnimatorAssetBuilder
    {
        private const string Folder = "Assets/Resources/Data/Animation";

        public static void EnsureControllers()
        {
            Directory.CreateDirectory(Folder);
            CreateController("GraniaMotion", false);
            CreateController("GolemMotion", true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateController(string name, bool heavy)
        {
            var path = $"{Folder}/{name}.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) return;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            var idle = AddState(controller, machine, "Idle", IdleClip(heavy));
            AddState(controller, machine, "Run", RunClip(heavy));
            AddState(controller, machine, "Attack", AttackClip(heavy));
            AddState(controller, machine, "Hit", HitClip(heavy));
            AddState(controller, machine, "Death", DeathClip(heavy));
            machine.defaultState = idle;
        }

        private static AnimatorState AddState(AnimatorController controller, AnimatorStateMachine machine,
            string stateName, AnimationClip clip)
        {
            clip.name = stateName;
            AssetDatabase.AddObjectToAsset(clip, controller);
            var state = machine.AddState(stateName);
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        private static AnimationClip IdleClip(bool heavy)
        {
            var amount = heavy ? 0.018f : 0.03f;
            return CreateClip(1f, true, "m_LocalPosition.y",
                new Keyframe(0f, 0f), new Keyframe(0.5f, amount), new Keyframe(1f, 0f));
        }

        private static AnimationClip RunClip(bool heavy)
        {
            var amount = heavy ? 0.055f : 0.075f;
            return CreateClip(0.42f, true, "m_LocalPosition.y",
                new Keyframe(0f, 0f), new Keyframe(0.105f, amount), new Keyframe(0.21f, 0f),
                new Keyframe(0.315f, amount), new Keyframe(0.42f, 0f));
        }

        private static AnimationClip AttackClip(bool heavy)
        {
            var amount = heavy ? 0.08f : 0.15f;
            return CreateClip(0.58f, false, "m_LocalPosition.x",
                new Keyframe(0f, 0f), new Keyframe(0.14f, -0.04f), new Keyframe(0.31f, amount),
                new Keyframe(0.58f, 0f));
        }

        private static AnimationClip HitClip(bool heavy)
        {
            var amount = heavy ? -0.07f : -0.12f;
            return CreateClip(0.42f, false, "m_LocalPosition.x",
                new Keyframe(0f, 0f), new Keyframe(0.08f, amount), new Keyframe(0.42f, 0f));
        }

        private static AnimationClip DeathClip(bool heavy)
        {
            var amount = heavy ? -0.08f : -0.12f;
            return CreateClip(0.68f, false, "m_LocalPosition.y",
                new Keyframe(0f, 0f), new Keyframe(0.68f, amount));
        }

        private static AnimationClip CreateClip(float length, bool loop, string property, params Keyframe[] keys)
        {
            var clip = new AnimationClip { frameRate = 60f };
            clip.SetCurve("Visual", typeof(Transform), property, new AnimationCurve(keys));
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = length;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }
    }
}
