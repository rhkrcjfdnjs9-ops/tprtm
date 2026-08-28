using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;

public static class ArcaV2SkinningSetup
{
    const string PsbPath = "Assets/Characters/Arca/Rig/Arca_Rig_v2.psb";

    sealed class Segment
    {
        public string Sprite, Root, Tip;
        public Vector2 A, B;
        public Segment(string sprite, string root, string tip, Vector2 a, Vector2 b)
        { Sprite=sprite; Root=root; Tip=tip; A=a; B=b; }
    }

    static readonly Segment[] Segments = {
        new Segment("UpperArm_R","Shoulder_R","Elbow_R", new Vector2(-1.25f,6.75f),new Vector2(-2f,5.9f)),
        new Segment("Forearm_R","Elbow_R","Wrist_R", new Vector2(-2f,5.9f),new Vector2(-2.55f,5.25f)),
        new Segment("Hand_R","Wrist_R","HandTip_R", new Vector2(-2.55f,5.25f),new Vector2(-3.05f,4.95f)),
        new Segment("UpperArm_L","Shoulder_L","Elbow_L", new Vector2(1.25f,6.75f),new Vector2(2f,5.9f)),
        new Segment("Forearm_L","Elbow_L","Wrist_L", new Vector2(2f,5.9f),new Vector2(2.55f,5.25f)),
        new Segment("Hand_L","Wrist_L","HandTip_L", new Vector2(2.55f,5.25f),new Vector2(3.05f,4.95f)),
        new Segment("Thigh_R","Hip_R","Knee_R", new Vector2(-.55f,4.55f),new Vector2(-.75f,2.75f)),
        new Segment("Boot_R","Knee_R","Ankle_R", new Vector2(-.75f,2.75f),new Vector2(-.85f,.75f)),
        new Segment("Thigh_L","Hip_L","Knee_L", new Vector2(.55f,4.55f),new Vector2(.75f,2.75f)),
        new Segment("Boot_L","Knee_L","Ankle_L", new Vector2(.75f,2.75f),new Vector2(.85f,.75f)),
        new Segment("Torso","Pelvis","Chest", new Vector2(0,4.85f),new Vector2(0,6.45f)),
        new Segment("Hair_Head","Neck","HeadTop", new Vector2(0,7.65f),new Vector2(0,11.5f)),
    };

    [MenuItem("Tools/2D Character/Arca V2/Apply Bone Weights")]
    public static void Apply()
    {
        var importer = AssetImporter.GetAtPath(PsbPath);
        if (importer == null) throw new InvalidOperationException("Arca V2 PSB importer not found.");
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();
        var meshProvider = provider.GetDataProvider<ISpriteMeshDataProvider>();
        if (boneProvider == null || meshProvider == null) throw new InvalidOperationException("PSD bone/mesh provider unavailable.");

        var rects = provider.GetSpriteRects().ToDictionary(r => r.name, r => r);
        var importedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PsbPath);
        var spriteOrigins = importedRoot.GetComponentsInChildren<SpriteRenderer>(true)
            .ToDictionary(r => r.sprite.name, r => (Vector2)r.transform.localPosition);
        int applied = 0;
        foreach (var segment in Segments)
        {
            if (!rects.TryGetValue(segment.Sprite, out var rect)) continue;
            if (!spriteOrigins.TryGetValue(segment.Sprite, out var origin)) continue;
            // SpriteRect.pivot is normalized; bone positions and mesh vertices
            // use layer pixels. Convert the pivot before adding local offsets.
            Vector2 pixelPivot = Vector2.Scale(rect.rect.size, rect.pivot);
            Vector2 a = pixelPivot + (segment.A-origin)*provider.pixelsPerUnit;
            Vector2 b = pixelPivot + (segment.B-origin)*provider.pixelsPerUnit;
            Vector2 delta = b-a;
            float length = Mathf.Max(.001f, delta.magnitude);
            Vector2 worldDelta = segment.B-segment.A;
            float angle = Mathf.Atan2(worldDelta.y,worldDelta.x)*Mathf.Rad2Deg;
            var bones = new List<SpriteBone> {
                new SpriteBone { name=segment.Root, guid=GUID.Generate().ToString(), parentId=-1,
                    position=a, rotation=Quaternion.Euler(0,0,angle), length=length, color=Color.cyan },
                new SpriteBone { name=segment.Tip, guid=GUID.Generate().ToString(), parentId=0,
                    position=new Vector3(length,0,0), rotation=Quaternion.identity,
                    length=Mathf.Max(8f,length*.18f), color=Color.magenta }
            };
            boneProvider.SetBones(rect.spriteID,bones);
            var vertices=meshProvider.GetVertices(rect.spriteID);
            for(int i=0;i<vertices.Length;i++)
            {
                float t=Mathf.Clamp01(Vector2.Dot(vertices[i].position-a,delta)/(length*length));
                t=Mathf.SmoothStep(0,1,t);
                var w=new BoneWeight { boneIndex0=0,weight0=1-t,boneIndex1=1,weight1=t };
                vertices[i].boneWeight=w;
            }
            meshProvider.SetVertices(rect.spriteID,vertices);
            applied++;
        }
        provider.Apply();
        importer.SaveAndReimport();
        Debug.Log("ARCA_V2_SKINNING_APPLIED="+applied);
    }

}
