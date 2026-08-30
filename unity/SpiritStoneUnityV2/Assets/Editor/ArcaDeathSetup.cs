using System.IO;
using System.Linq;
using SpiritStone.Characters;
using SpiritStone.Characters.Arca;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritStone.Editor
{
    [InitializeOnLoad]
    public static class ArcaDeathSetup
    {
        private const string Root = "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca";
        private const string FrameRoot = Root + "/DeathV2";
        private const string ClipPath = Root + "/Animations/Arca_Death.anim";
        private const string ControllerPath = Root + "/Animations/Arca_Idle.controller";
        private const string PreviewScenePath = "Assets/Characters/Arca/Pixel64/Scenes/ArcaDeathPreview.unity";
        private const int FrameCount = 9;
        private const float FrameRate = 10f;

        [MenuItem("Tools/2D Character/Arca Pixel64/Rebuild South-East Death V2")]
        public static void Rebuild()
        {
            var sprites = new Sprite[FrameCount];
            for (var index = 0; index < FrameCount; index++)
            {
                string path = $"{FrameRoot}/Arca_Death_SE_V2_{index:00}.png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException($"Death sprite is missing: {path}");
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
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) != null)
                AssetDatabase.DeleteAsset(ClipPath);
            var clip = new AnimationClip { frameRate = FrameRate, name = "Arca_Death" };
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
            if (!controller.parameters.Any(parameter => parameter.name == "Death"))
                controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState death = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "Death");
            if (death == null) death = stateMachine.AddState("Death", new Vector3(620f, 180f, 0f));
            death.motion = clip;

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
                if (transition.destinationState == death) stateMachine.RemoveAnyStateTransition(transition);
            AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(death);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Death");
            enter.duration = 0f;
            enter.hasExitTime = false;
            foreach (AnimatorStateTransition transition in death.transitions.ToArray())
                death.RemoveTransition(transition);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("[ArcaDeathSetup] Connected {0} South-East death frames at {1} FPS with the final pose held.", FrameCount, FrameRate);
        }

        [MenuItem("Tools/2D Character/Arca Pixel64/Rebuild And Preview South-East Death V2")]
        public static void RebuildAndPreview()
        {
            Rebuild();
            CreatePreviewScene();
            PortraitGameViewSetup.Apply();
            EditorApplication.isPlaying = true;
        }

        private static void CreatePreviewScene()
        {
            string sceneDirectory = Path.GetDirectoryName(PreviewScenePath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(sceneDirectory) && !AssetDatabase.IsValidFolder(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
                AssetDatabase.Refresh();
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.025f, 0.07f, 1f);

            Sprite initialSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{FrameRoot}/Arca_Death_SE_V2_00.png");
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            var arca = new GameObject("Arca Death Preview");
            arca.transform.position = new Vector3(0f, -0.8f, 0f);
            SpriteRenderer renderer = arca.AddComponent<SpriteRenderer>();
            Animator animator = arca.AddComponent<Animator>();
            PixelCharacterView view = arca.AddComponent<PixelCharacterView>();
            view.Configure(initialSprite, 10);
            view.SetAnimatorController(controller);
            arca.AddComponent<ArcaDeathPreviewLoop>();

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            Debug.LogFormat("[ArcaDeathSetup] Created portrait death-only preview scene at {0}.", PreviewScenePath);
        }
    }
}
