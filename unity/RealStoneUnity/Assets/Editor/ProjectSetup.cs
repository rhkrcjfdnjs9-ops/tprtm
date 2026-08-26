using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealStone.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("Real Stone/Prepare Prototype")]
        public static void PreparePrototype()
        {
            AnimatorAssetBuilder.EnsureControllers();
            SpriteFrameNormalizer.EnsureNormalizedFrames();
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Real Stone Bootstrap").AddComponent<RealStoneBootstrap>();
            const string scenePath = "Assets/Scenes/Battle.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            PlayerSettings.companyName = "RealStoneStudio";
            PlayerSettings.productName = "Real Stone Raising";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.realstone.studio.game");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            AssetDatabase.SaveAssets();
            Debug.Log("REAL_STONE_SETUP_COMPLETE");
        }

        public static void BuildAndroidPrototype()
        {
            PreparePrototype();
            Directory.CreateDirectory(@"D:\UnityBuilds\RealStone");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Battle.unity" },
                locationPathName = @"D:\UnityBuilds\RealStone\RealStoneUnity.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Android build failed: {report.summary.result}");
            }
            Debug.Log($"REAL_STONE_ANDROID_BUILD_COMPLETE bytes={report.summary.totalSize}");
        }
    }
}
