using SpiritStone.Core;
using SpiritStone.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpiritStone.EditorTools
{
    public static class IdleBattlePrototypeSetup
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ScenePath = SceneFolder + "/IdleBattlePrototype.unity";

        [MenuItem("Tools/Spirit Stone/Create Idle Battle Prototype")]
        public static void CreateScene()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "IdleBattlePrototype";

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.backgroundColor = new Color(0.035f, 0.04f, 0.075f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject lightObject = new("Directional Light");
            lightObject.AddComponent<Light>().type = LightType.Directional;

            GameObject managerObject = new("GameManager");
            managerObject.AddComponent<GameManager>();

            GameObject prototypeObject = new("IdleBattlePrototype");
            prototypeObject.AddComponent<IdleBattlePrototype>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = prototypeObject;
            Debug.LogFormat("[IdleBattlePrototypeSetup] Prototype scene created at {0}.", ScenePath);
        }
    }
}
