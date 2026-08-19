using BladeAndTitan.TitanShifting.Abstract;
using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.SpecialisedTitans;

public class WarhammerTitan : PlayerTitanBase
{
    public override string titanAddress => "WarhammerTitanPlayerAvatar";
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
    /*
    protected override Quaternion TitanHandLeftRotation => Quaternion.Euler(90, 90, 0);
    protected override Quaternion TitanHandRightRotation => Quaternion.Euler(90, -90, 0);
    protected override Quaternion TitanHeadRotation => Quaternion.Euler(0, 0, 0);
    */


    private static Animator spikeAnimator;
    private ParticleSystem electricEffect;
    
    private static readonly int Healing = Animator.StringToHash("Healing");
    private static readonly int SpikesAoE = Animator.StringToHash("SpikesAoE");
    private static readonly int SpikesForward = Animator.StringToHash("SpikesForward");
    private static readonly int SpikesCircle = Animator.StringToHash("SpikesCircle");
    

    protected override void OnTitanPossess()
    {
        base.OnTitanPossess();
        spikeAnimator = titan.transform.FindChildRecursive("DamageSpikes").GetComponent<Animator>();
        electricEffect = titan.transform.FindChildRecursive($"Electric{spellCaster.side.ToString()}").GetComponent<ParticleSystem>();
    }

    public override void Throw(Vector3 velocity)
    {
        base.Throw(velocity);

        Vector3 direction = velocity.normalized;

        bool palmFacingForward =
            Vector3.Dot(spellCaster.ragdollHand.PalmDir, Player.local.transform.forward) > 0.8f;
        
        bool palmFacingUp = Vector3.Dot(spellCaster.ragdollHand.PalmDir, Vector3.up) > 0.8f;

        bool thrownForward =
            Vector3.Dot(direction, Player.local.transform.forward) > 0.8f;

        bool thrownUp =
            Vector3.Dot(direction, Vector3.up) > 0.8f;

        if (palmFacingForward && thrownForward)
        {
            CastAbility(SpikesForward);
        }
        else if (thrownUp && palmFacingUp)
        {
            if (velocity.magnitude > 10f)
            {
                CastAbility(SpikesAoE);
            }
            else
            {
                CastAbility(SpikesCircle);
            }
        }
    }


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
        
        /* left -> 45, 90, 0 */
        /* right -> 45, -90, 0 */
        
    }



    void CastAbility(int hash)
    {
        if(AnimatorIsPlaying()) return;
        spikeAnimator.Play(hash);
        PlaySound("ElectricWarhammerAudio", spellCaster.transform.position);
        PlaySound("SpikeCreationWarhammer", spellCaster.transform.position);
        electricEffect.Play();
    }
    
    static bool AnimatorIsPlaying(){
        return spikeAnimator.GetCurrentAnimatorStateInfo(0).length >
               spikeAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }
    
}