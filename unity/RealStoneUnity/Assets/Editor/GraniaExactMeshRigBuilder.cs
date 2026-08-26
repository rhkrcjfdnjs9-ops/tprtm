using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RealStone.Editor
{
    public static class GraniaExactMeshRigBuilder
    {
        private const string SourcePath = "Assets/Characters/Grania/Master/Grania_Master.png";
        private const string OutputFolder = "Assets/Generated/GraniaExactRig";
        private const string PrefabPath = OutputFolder + "/GraniaExactRig.prefab";
        private const string PreviewPath = OutputFolder + "/GraniaExactRig_NeutralPreview.png";
        private const string IdleClipPath = OutputFolder + "/GraniaExactRig_Idle.anim";
        private const string ControllerPath = OutputFolder + "/GraniaExactRig.controller";
        private const int Columns = 48;
        private const int Rows = 50;
        private const float Height = 8f;

        // Hand-authored UV boundaries measured directly against Grania_Master.png.
        // These are fixed production masks, not generated or inferred regions.
        private static readonly Vector2[] HaloRegion =
        {
            new(0.29f, 0.62f), new(0.34f, 0.88f), new(0.45f, 1.00f),
            new(0.58f, 1.00f), new(0.78f, 0.88f), new(0.78f, 0.62f),
            new(0.65f, 0.62f), new(0.62f, 0.78f), new(0.39f, 0.78f), new(0.38f, 0.62f)
        };
        private static readonly Vector2[] SwordRegion =
        {
            new(0.05f, 0.05f), new(0.15f, 0.34f), new(0.33f, 0.50f),
            new(0.45f, 0.45f), new(0.37f, 0.27f), new(0.22f, 0.08f)
        };
        private static readonly Vector2[] HeadRegion =
        {
            new(0.42f, 0.58f), new(0.43f, 0.76f), new(0.49f, 0.84f),
            new(0.61f, 0.82f), new(0.67f, 0.72f), new(0.64f, 0.57f), new(0.52f, 0.54f)
        };
        private static readonly Vector2[] HairRegion =
        {
            new(0.27f, 0.34f), new(0.31f, 0.62f), new(0.41f, 0.78f),
            new(0.68f, 0.74f), new(0.75f, 0.58f), new(0.75f, 0.34f),
            new(0.65f, 0.36f), new(0.61f, 0.57f), new(0.43f, 0.57f), new(0.38f, 0.34f)
        };
        private static readonly Vector2[] RightArmRegion =
        {
            new(0.36f, 0.39f), new(0.42f, 0.57f), new(0.49f, 0.57f),
            new(0.47f, 0.39f), new(0.41f, 0.34f)
        };
        private static readonly Vector2[] LeftArmRegion =
        {
            new(0.57f, 0.57f), new(0.65f, 0.57f), new(0.72f, 0.39f),
            new(0.67f, 0.34f), new(0.59f, 0.39f)
        };
        private static readonly Vector2[] TorsoRegion =
        {
            new(0.43f, 0.36f), new(0.44f, 0.58f), new(0.62f, 0.58f),
            new(0.64f, 0.35f), new(0.55f, 0.29f), new(0.48f, 0.30f)
        };
        private static readonly Vector2[] SkirtRegion =
        {
            new(0.32f, 0.10f), new(0.39f, 0.37f), new(0.48f, 0.42f),
            new(0.58f, 0.42f), new(0.72f, 0.34f), new(0.73f, 0.11f), new(0.57f, 0.16f), new(0.48f, 0.16f)
        };
        private static readonly Vector2[] RightLegRegion =
        {
            new(0.39f, 0.02f), new(0.41f, 0.31f), new(0.50f, 0.36f),
            new(0.52f, 0.25f), new(0.49f, 0.02f)
        };
        private static readonly Vector2[] LeftLegRegion =
        {
            new(0.51f, 0.02f), new(0.52f, 0.25f), new(0.55f, 0.36f),
            new(0.64f, 0.30f), new(0.64f, 0.02f)
        };

        [MenuItem("Real Stone/Grania Rig/Build Exact Original Mesh Rig")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputFolder);
            EnsureTexture();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
            if (texture == null) throw new FileNotFoundException("Original Grania image missing", SourcePath);

            var root = new GameObject("GraniaExactRig");
            var bones = CreateBones(root.transform);
            var mesh = CreateMesh(texture, root.transform, bones);
            var meshPath = OutputFolder + "/GraniaExactMesh.asset";
            ReplaceAsset(mesh, meshPath);

            var material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "GraniaExactMaterial",
                mainTexture = texture
            };
            var materialPath = OutputFolder + "/GraniaExactMaterial.mat";
            ReplaceAsset(material, materialPath);

            var visual = new GameObject("OriginalArtworkMesh");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.bones = bones;
            renderer.rootBone = bones[0];
            renderer.updateWhenOffscreen = true;
            renderer.localBounds = mesh.bounds;

            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = CreateIdleController();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            RenderNeutralPreview(root);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"GRANIA_EXACT_RIG_BUILT: {PrefabPath}");
        }

        private static RuntimeAnimatorController CreateIdleController()
        {
            var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            if (existingClip != null) AssetDatabase.DeleteAsset(IdleClipPath);
            var clip = new AnimationClip { name = "GraniaExactRig_Idle", frameRate = 60f };
            SetCurve(clip, "", "m_LocalPosition.x", -1.35f, 1.35f, -1.35f);
            SetCurve(clip, "Root/Pelvis", "m_LocalPosition.y", 2.08f, 2.18f, 2.08f);
            SetCurve(clip, "Root/Pelvis/Torso", "localEulerAnglesRaw.z", -1.2f, 1.2f, -1.2f);
            SetCurve(clip, "Root/Pelvis/Torso/Head", "localEulerAnglesRaw.z", 1.5f, -1.5f, 1.5f);
            SetCurve(clip, "Root/Pelvis/Torso/Head/Hair", "localEulerAnglesRaw.z", -2.2f, 2.2f, -2.2f);
            SetCurve(clip, "Root/Pelvis/Torso/UpperArm.R", "localEulerAnglesRaw.z", 3.0f, -3.0f, 3.0f);
            SetCurve(clip, "Root/Pelvis/Torso/UpperArm.L", "localEulerAnglesRaw.z", -3.0f, 3.0f, -3.0f);
            SetCurve(clip, "Root/Pelvis/Leg.R", "localEulerAnglesRaw.z", -2.5f, 2.5f, -2.5f);
            SetCurve(clip, "Root/Pelvis/Leg.L", "localEulerAnglesRaw.z", 2.5f, -2.5f, 2.5f);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, IdleClipPath);

            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("Idle");
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void SetCurve(AnimationClip clip, string path, string property,
            float start, float middle, float end)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, start), new Keyframe(1.5f, middle), new Keyframe(3f, end));
            for (var i = 0; i < curve.length; i++)
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        private static void RenderNeutralPreview(GameObject rig)
        {
            const int previewLayer = 31;
            var transforms = rig.GetComponentsInChildren<Transform>(true);
            var originalLayers = new int[transforms.Length];
            for (var i = 0; i < transforms.Length; i++)
            {
                originalLayers[i] = transforms[i].gameObject.layer;
                transforms[i].gameObject.layer = previewLayer;
            }
            var cameraObject = new GameObject("GraniaRigPreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.25f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << previewLayer;

            var target = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            var output = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            output.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
            output.Apply(false, false);
            File.WriteAllBytes(Path.GetFullPath(PreviewPath), output.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            for (var i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = originalLayers[i];
        }

        private static void EnsureTexture()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Original Grania texture importer missing", SourcePath);
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Transform[] CreateBones(Transform root)
        {
            var result = new Transform[14];
            result[0] = Bone(root, "Root", new Vector2(0f, -3.6f));
            result[1] = Bone(result[0], "Pelvis", new Vector2(0f, 2.1f));
            result[2] = Bone(result[1], "Torso", new Vector2(0f, 1.65f));
            result[3] = Bone(result[2], "Head", new Vector2(0.15f, 1.75f));
            result[4] = Bone(result[2], "UpperArm.R", new Vector2(-1.15f, 0.45f));
            result[5] = Bone(result[4], "Forearm.R", new Vector2(-0.55f, -0.85f));
            result[6] = Bone(result[2], "UpperArm.L", new Vector2(1.15f, 0.45f));
            result[7] = Bone(result[6], "Forearm.L", new Vector2(0.55f, -0.85f));
            result[8] = Bone(result[1], "Leg.R", new Vector2(-0.52f, -0.35f));
            result[9] = Bone(result[1], "Leg.L", new Vector2(0.52f, -0.35f));
            result[10] = Bone(result[3], "Hair", new Vector2(-0.1f, -0.35f));
            result[11] = Bone(result[1], "Skirt", new Vector2(0f, 0.15f));
            result[12] = Bone(result[5], "Sword", new Vector2(-0.75f, -0.55f));
            result[13] = Bone(result[3], "Halo", new Vector2(-0.15f, 1.25f));
            return result;
        }

        private static Transform Bone(Transform parent, string name, Vector2 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Mesh CreateMesh(Texture2D texture, Transform root, Transform[] bones)
        {
            var width = Height * texture.width / texture.height;
            var vertices = new Vector3[(Columns + 1) * (Rows + 1)];
            var uvs = new Vector2[vertices.Length];
            var weights = new BoneWeight[vertices.Length];
            var triangles = new int[Columns * Rows * 6];

            for (var y = 0; y <= Rows; y++)
            {
                var v = y / (float)Rows;
                for (var x = 0; x <= Columns; x++)
                {
                    var u = x / (float)Columns;
                    var i = y * (Columns + 1) + x;
                    vertices[i] = new Vector3((u - 0.5f) * width, (v - 0.5f) * Height, 0f);
                    uvs[i] = new Vector2(u, v);
                    weights[i] = WeightFor(u, v);
                }
            }

            var t = 0;
            for (var y = 0; y < Rows; y++)
            for (var x = 0; x < Columns; x++)
            {
                var i = y * (Columns + 1) + x;
                triangles[t++] = i;
                triangles[t++] = i + Columns + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + Columns + 1;
                triangles[t++] = i + Columns + 2;
            }

            var bindPoses = new Matrix4x4[bones.Length];
            for (var i = 0; i < bones.Length; i++) bindPoses[i] = bones[i].worldToLocalMatrix * root.localToWorldMatrix;
            var mesh = new Mesh { name = "GraniaExactMesh" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.boneWeights = weights;
            mesh.bindposes = bindPoses;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static BoneWeight WeightFor(float u, float v)
        {
            var point = new Vector2(u, v);
            if (Inside(point, SwordRegion)) return Single(12);
            if (Inside(point, HeadRegion)) return Blend(3, 2, Mathf.InverseLerp(0.56f, 0.63f, v));
            if (Inside(point, HairRegion)) return Blend(10, 3, Mathf.InverseLerp(0.38f, 0.58f, v));
            if (Inside(point, HaloRegion)) return Blend(13, 3, Mathf.InverseLerp(0.74f, 0.82f, v));
            if (Inside(point, RightArmRegion))
                return Blend(v < 0.46f ? 5 : 4, 2, Mathf.InverseLerp(0.42f, 0.55f, v));
            if (Inside(point, LeftArmRegion))
                return Blend(v < 0.46f ? 7 : 6, 2, Mathf.InverseLerp(0.42f, 0.55f, v));
            if (Inside(point, TorsoRegion)) return Blend(2, 1, Mathf.InverseLerp(0.31f, 0.41f, v));
            if (Inside(point, RightLegRegion)) return Blend(8, 1, Mathf.InverseLerp(0.25f, 0.34f, v));
            if (Inside(point, LeftLegRegion)) return Blend(9, 1, Mathf.InverseLerp(0.25f, 0.34f, v));
            if (Inside(point, SkirtRegion)) return Blend(11, 1, Mathf.InverseLerp(0.17f, 0.37f, v));
            return Single(0);
        }

        private static BoneWeight Single(int bone) => new() { boneIndex0 = bone, weight0 = 1f };

        private static BoneWeight Blend(int child, int parent, float childWeight)
        {
            childWeight = Mathf.SmoothStep(0.15f, 0.95f, Mathf.Clamp01(childWeight));
            return new BoneWeight
            {
                boneIndex0 = child, weight0 = childWeight,
                boneIndex1 = parent, weight1 = 1f - childWeight
            };
        }

        private static bool Inside(Vector2 point, Vector2[] polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > point.y) == (b.y > point.y)) continue;
                var crossingX = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (point.x < crossingX) inside = !inside;
            }
            return inside;
        }

        private static void ReplaceAsset(Object asset, string path)
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
