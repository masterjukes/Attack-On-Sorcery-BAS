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
    
    public override bool useXYThumbRotation => false;

    protected override Quaternion TitanHandLeftRotation => Quaternion.Euler(0, 180, 315);
    protected override Quaternion TitanHandRightRotation => Quaternion.Euler(0, 180, 225); 

    protected override Quaternion TitanHeadRotation => Quaternion.Euler(0, 0, 0);

    Vector3 _thumbRotationRight = new Vector3(0, 0, 90);
    Vector3 _thumbRotationLeft = new Vector3(0, 0, -90);
    public override Vector3 thumbRotationLeft => _thumbRotationLeft;
    public override Vector3 thumbRotationRight => _thumbRotationRight;

    
    

    private static Animator spikeAnimator;
    private ParticleSystem electricEffect;
    
    private static readonly int Healing = Animator.StringToHash("Healing");
    private static readonly int SpikesAoE = Animator.StringToHash("SpikesAoE");
    private static readonly int SpikesForward = Animator.StringToHash("SpikesForward");
    private static readonly int SpikesCircle = Animator.StringToHash("SpikesCircle");
    
    
    public float cooldown = 0f;
    public float velocityThreshold = 1.5f;
    public float angleThreshold = 40f;

    private float leftLastTriggerTime = -Mathf.Infinity;
    private float rightLastTriggerTime = -Mathf.Infinity;
    

    protected override void OnTitanPossess()
    {
        base.OnTitanPossess();
        spikeAnimator = titan.transform.FindChildRecursive("DamageSpikes").GetComponent<Animator>();
        electricEffect = titan.transform.FindChildRecursive($"Electric{spellCaster.side.ToString()}").GetComponent<ParticleSystem>();
    }

    public override void Throw(Vector3 velocity)
    {
        base.Throw(velocity);

        bool isRightHand = spellCaster.side == Side.Right;
        float lastTriggerTime = isRightHand
            ? rightLastTriggerTime
            : leftLastTriggerTime;

        if (Time.time - lastTriggerTime <= cooldown || AnimatorIsPlaying())
            return;

        float speed = velocity.magnitude;

        var hand = spellCaster.ragdollHand;
        var cameraTransform = Player.local.head.cam.transform;
        var flingDirection = velocity.normalized;

        bool IsGesture(Vector3 targetDirection)
        {
            return Vector3.Angle(targetDirection, flingDirection) < angleThreshold &&
                   Vector3.Dot(hand.PalmDir.normalized, targetDirection) > 0.5f;
        }

        // Forward fling, palm facing forward
        if (IsGesture(cameraTransform.forward))
        {
            CastAbility(SpikesForward);
        }
        // Upward fling, palm facing up
        else if (IsGesture(cameraTransform.up))
        {
            if (speed > 5f)
                CastAbility(SpikesAoE);
            else
                CastAbility(SpikesCircle);
        }
        else
        {
            return;
        }

        if (isRightHand)
            rightLastTriggerTime = Time.time;
        else
            leftLastTriggerTime = Time.time;
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

        const float stretch = 0.2f;
        
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
        if (AnimatorIsPlaying()) return;
        spikeAnimator.transform.parent = titan.transform;
        spikeAnimator.transform.localPosition = Vector3.zero;
        spikeAnimator.transform.localRotation = Quaternion.identity;
        spikeAnimator.transform.parent = null;

        foreach (Transform child in spikeAnimator.transform)
        {
            if (Physics.Raycast(child.position, Vector3.down, out RaycastHit hit, 40f))
            {
                child.position = hit.point;
            }
        }

        if (hash == SpikesAoE)
        {
            ColossalTitan.ApplyExplosionForce(10f, spikeAnimator.transform.position, 70f);
        }
        else if (hash == SpikesForward)
        {
            ColossalTitan.ApplyExplosionForce(5f, spikeAnimator.transform.position, 40f);
        }
        else if (hash == SpikesCircle)
        {
            ColossalTitan.ApplyExplosionForce(3f, spikeAnimator.transform.position, 30f);
        }


    spikeAnimator.Play(hash);
        PlaySound("ElectricWarhammerAudio", spellCaster.transform.position);
        PlaySound("SpikeCreationWarhammer", spellCaster.transform.position);
        electricEffect.Play();
    }
    
    bool AnimatorIsPlaying()
    {
        var state = spikeAnimator.GetCurrentAnimatorStateInfo(0);

        return (state.shortNameHash == SpikesAoE ||
                state.shortNameHash == SpikesForward ||
                state.shortNameHash == SpikesCircle)
               && state.normalizedTime < 1f;
    }
    
}