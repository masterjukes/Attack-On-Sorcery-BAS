using BladeAndTitan.TitanShifting.Abstract;
using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.SpecialisedTitans;

public class AttackTitan : PlayerTitanBase
{
    public override string titanAddress => "AttackTitanPlayerAvatar";
    public override float footDistance => 2f;
    public override float stepSpeed => 1f;
    public override float maxHealth => 1000f;
    public override float jumpForce => 1f;
    public override float speedMultiplier => 2.5f;
    public override float handWeight => 10f;
    
    protected override string VRIKLeftFootName => "mixamorig:LeftFoot";
    protected override string VRIKRightFootName => "mixamorig:RightFoot";

    public override float HeadTargetForwardOffset => -0.15f;

    public override string stepSoundId => "CollTitanStepAudio";
    
    protected override Quaternion TitanHandLeftRotation => Quaternion.Euler(0, 90, 90);
    protected override Quaternion TitanHandRightRotation => Quaternion.Euler(0, -90, -90);
    protected override Quaternion TitanHeadRotation => Quaternion.Euler(0, 0, 0);
    
    Vector3 _thumbRotationRight = new Vector3(0, 0, 90);
    Vector3 _thumbRotationLeft = new Vector3(0, 0, -90);
    public override Vector3 thumbRotationLeft => _thumbRotationLeft;
    public override Vector3 thumbRotationRight => _thumbRotationRight;
    public override bool useXYThumbRotation => false;



    protected override void SetHands(GameObject o)
    {
        var handR = o.transform.FindChildRecursive("mixamorig:RightHand").gameObject.AddComponent<TitanHand>();
        handR.side = Side.Right;
        handR.thumbParentName = "mixamorig:RightHandThumb1";
        handR.indexParentName = "mixamorig:RightHandIndex1";
        handR.middleParentName = "mixamorig:RightHandMiddle1";
        handR.ringParentName = "mixamorig:RightHandRing1";
        handR.pinkyParentName = "mixamorig:RightHandPinky1";
        handR.Init();


        var handL = o.transform.FindChildRecursive("mixamorig:LeftHand").gameObject.AddComponent<TitanHand>();
        handL.side = Side.Left;
        handL.thumbParentName = "mixamorig:LeftHandThumb1";
        handL.indexParentName = "mixamorig:LeftHandIndex1";
        handL.middleParentName = "mixamorig:LeftHandMiddle1";
        handL.ringParentName = "mixamorig:LeftHandRing1";
        handL.pinkyParentName = "mixamorig:LeftHandPinky1";
        handL.Init();

        var vrik = o.GetComponent<VRIK>();

        const float stretch = 0.5f;
        
        vrik.solver.leftArm.stretchCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, stretch)
        );

        vrik.solver.rightArm.stretchCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, stretch)
        );
        
        handL.maxAllowed = 1;
        handL.requiredFlingVelocity = 4f;
        handR.requiredFlingVelocity = 4f;
        handR.maxAllowed = 1;
        
    }
}