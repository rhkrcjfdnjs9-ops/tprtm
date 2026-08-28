using UnityEngine;

public sealed class ArcaWalkPreviewController : MonoBehaviour
{
    [Range(0.5f, 5f)] public float speed = 2.4f;
    [Range(0f, .35f)] public float armSwing = .14f;
    [Range(0f, .25f)] public float footStride = .08f;
    [Range(0f, .25f)] public float footLift = .10f;

    Transform armR, armL, legR, legL;
    Vector3 armR0, armL0, legR0, legL0;

    void Awake()
    {
        armR = FindDeep("IK_Arm_R_Target");
        armL = FindDeep("IK_Arm_L_Target");
        legR = FindDeep("IK_Leg_R_Target");
        legL = FindDeep("IK_Leg_L_Target");
        if (!armR || !armL || !legR || !legL) { enabled=false; return; }
        armR0=armR.localPosition; armL0=armL.localPosition;
        legR0=legR.localPosition; legL0=legL.localPosition;
    }

    void Update()
    {
        ApplyPose(Mathf.Repeat(Time.time*speed,1f));
    }

    public void ApplyPose(float normalizedTime)
    {
        if(!armR) Awake();
        if(!enabled || !armR) return;
        float phase=normalizedTime*Mathf.PI*2f;
        float wave=Mathf.Sin(phase);
        float opposite=-wave;
        armR.localPosition=armR0+new Vector3(wave*.055f,wave*armSwing,0);
        armL.localPosition=armL0+new Vector3(opposite*.055f,opposite*armSwing,0);
        legR.localPosition=legR0+new Vector3(wave*footStride,Mathf.Max(0,wave)*footLift,0);
        legL.localPosition=legL0+new Vector3(opposite*footStride,Mathf.Max(0,opposite)*footLift,0);
    }

    void OnDisable()
    {
        if(armR) armR.localPosition=armR0;
        if(armL) armL.localPosition=armL0;
        if(legR) legR.localPosition=legR0;
        if(legL) legL.localPosition=legL0;
    }

    Transform FindDeep(string target)
    {
        foreach(var t in GetComponentsInChildren<Transform>(true)) if(t.name==target) return t;
        return null;
    }
}
