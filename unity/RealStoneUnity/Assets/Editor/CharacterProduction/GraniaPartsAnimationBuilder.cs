using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RealStone.Editor.CharacterProduction
{
    public static class GraniaPartsAnimationBuilder
    {
        private const string CharacterFolder = "Assets/Characters/GraniaPartsV1";
        private const string PrefabPath = CharacterFolder + "/CharacterRoot.prefab";
        private const string AnimationFolder = CharacterFolder + "/Animations/Runtime";
        private const string IdleClipPath = AnimationFolder + "/Grania_Idle.anim";
        private const string ControllerPath = AnimationFolder + "/GraniaParts.controller";

        [MenuItem("Tools/2D Character/Build Grania Parts Idle")]
        public static void BuildIdle()
        {
            Directory.CreateDirectory(AnimationFolder);
            AssetDatabase.Refresh();

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Grania_Idle", frameRate = 60f };
                AssetDatabase.CreateAsset(clip, IdleClipPath);
            }

            clip.ClearCurves();
            SetLoop(clip);
            SetFloatCurve(clip, "Body", "m_LocalPosition.y", 0f, 0.018f, 0f);
            SetFloatCurve(clip, "Head", "m_LocalPosition.y", 2.86f, 2.872f, 2.86f);
            SetRotationZ(clip, "Head", -0.35f, 0.35f, -0.35f);
            SetRotationZ(clip, "BackHair", -0.25f, 0.45f, -0.25f);
            SetRotationZ(clip, "Arm_R", 0.45f, -0.45f, 0.45f);
            SetRotationZ(clip, "Arm_L", -0.45f, 0.45f, -0.45f);
            SetRotationZ(clip, "Skirt/Skirt_R", -0.35f, 0.45f, -0.35f);
            SetRotationZ(clip, "Skirt/Skirt_L", 0.35f, -0.45f, 0.35f);
            SetBlinkCurve(clip);
            EditorUtility.SetDirty(clip);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var state in stateMachine.states)
                stateMachine.RemoveState(state.state);
            var idleState = stateMachine.AddState("Idle");
            idleState.motion = clip;
            stateMachine.defaultState = idleState;

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var animator = root.GetComponent<Animator>();
                if (animator == null) animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = clip;
            EditorGUIUtility.PingObject(clip);
            Debug.Log($"GRANIA_PARTS_IDLE_BUILT: {IdleClipPath}");
        }

        private static void SetLoop(AnimationClip clip)
        {
            var serialized = new SerializedObject(clip);
            var settings = serialized.FindProperty("m_AnimationClipSettings");
            settings.FindPropertyRelative("m_LoopTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloatCurve(AnimationClip clip, string path, string property,
            float start, float middle, float end)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, start),
                new Keyframe(1f, middle),
                new Keyframe(2f, end));
            Smooth(curve);
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        private static void SetRotationZ(AnimationClip clip, string path, float startDegrees,
            float middleDegrees, float endDegrees)
        {
            var start = Quaternion.Euler(0f, 0f, startDegrees);
            var middle = Quaternion.Euler(0f, 0f, middleDegrees);
            var end = Quaternion.Euler(0f, 0f, endDegrees);
            SetQuaternionCurve(clip, path, "x", start.x, middle.x, end.x);
            SetQuaternionCurve(clip, path, "y", start.y, middle.y, end.y);
            SetQuaternionCurve(clip, path, "z", start.z, middle.z, end.z);
            SetQuaternionCurve(clip, path, "w", start.w, middle.w, end.w);
        }

        private static void SetQuaternionCurve(AnimationClip clip, string path, string component,
            float start, float middle, float end)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, start),
                new Keyframe(1f, middle),
                new Keyframe(2f, end));
            Smooth(curve);
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + component), curve);
        }

        private static void SetBlinkCurve(AnimationClip clip)
        {
            var neutral = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterFolder + "/Parts/Face/Eyes.png");
            var blink = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterFolder + "/Animations/Expressions/Eyes_Blink.png");
            if (neutral == null || blink == null) return;
            var binding = EditorCurveBinding.PPtrCurve("Head/Eyes", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = neutral },
                new ObjectReferenceKeyframe { time = 1.58f, value = neutral },
                new ObjectReferenceKeyframe { time = 1.63f, value = blink },
                new ObjectReferenceKeyframe { time = 1.72f, value = neutral },
                new ObjectReferenceKeyframe { time = 2f, value = neutral }
            });
        }

        private static void Smooth(AnimationCurve curve)
        {
            for (var i = 0; i < curve.length; i++)
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            for (var i = 0; i < curve.length; i++)
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }
    }
}
