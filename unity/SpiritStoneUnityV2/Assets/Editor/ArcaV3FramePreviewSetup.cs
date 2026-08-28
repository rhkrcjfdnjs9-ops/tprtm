using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArcaV3FramePreviewSetup
{
    private const string Root = "Assets/Characters/Arca/FrameAnimation";
    private const string Idle = Root + "/Idle";

    [MenuItem("Tools/2D Character/Arca V3/Create Frame Preview")]
    public static void Create()
    {
        Directory.CreateDirectory(Root + "/Animations");
        Directory.CreateDirectory(Root + "/Scenes");

        var sprites = new Sprite[4];
        for (var i = 0; i < sprites.Length; i++)
        {
            var path = $"{Idle}/Arca_Idle_{i:00}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 200f;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        var clipPath = Root + "/Animations/Arca_V3_Idle_Frame.anim";
        AssetDatabase.DeleteAsset(clipPath);
        var clip = new AnimationClip { frameRate = 8f, name = "Arca_V3_Idle_Frame" };
        var binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        var keys = new ObjectReferenceKeyframe[5];
        for (var i = 0; i < 4; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / 8f, value = sprites[i] };
        keys[4] = new ObjectReferenceKeyframe { time = 4f / 8f, value = sprites[0] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, clipPath);

        var controllerPath = Root + "/Animations/Arca_V3_Frame.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.layers[0].stateMachine.AddState("Idle").motion = clip;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraGo = new GameObject("Main Camera");
        var camera = cameraGo.AddComponent<Camera>();
        cameraGo.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 3.6f;
        camera.backgroundColor = new Color(0.025f, 0.02f, 0.06f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);

        var arca = new GameObject("Arca_V3_FrameCharacter");
        var renderer = arca.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites[0];
        arca.AddComponent<Animator>().runtimeAnimatorController = controller;

        EditorSceneManager.SaveScene(scene, Root + "/Scenes/Arca_V3_FramePreview.unity");
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = arca;
        Debug.Log("[Arca V3] Four-frame idle preview created.");
    }
}
