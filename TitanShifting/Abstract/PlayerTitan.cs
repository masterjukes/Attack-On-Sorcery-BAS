#define DEBUG // enable Titan Transofrm on fist event .

using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.Abstract;


public abstract class PlayerTitanBase : SpellCastCharge
{
    [ModOption]
    [ModOptionFloatValues(0.1f, 1f, 0.05f)]
    public static float ScaleUniversal = 1.0f;
    static bool isTitan;
    static float lastHeight;
    
    protected static GameObject titan;

    public abstract string titanAddress { get; }
    public abstract float footDistance { get; }
    
    public abstract float stepSpeed { get; }
    
    float lastHeadRotation;

    public sealed override void Load(SpellCaster spellCaster)
    {
        base.Load(spellCaster);
        Player.selfCollision = true;
        Player.currentCreature.OnDamageEvent += CurrentCreatureOnOnDamageEvent;
        
        Debug.Log("Loaded " + this.GetType().Name);
        
        #if DEBUG
        spellCaster.ragdollHand.playerHand.OnFistEvent += PlayerHandOnOnFistEvent;
        #endif
    }

    private void PlayerHandOnOnFistEvent(PlayerHand hand, bool gripping)
    {
        if (gripping && !isTitan)
        {
            Debug.Log("Titan Shifting due to debug fist event");
            OnShift(false);
        }

    }

    private void CurrentCreatureOnOnDamageEvent(CollisionInstance collisionInstance, EventTime eventTime)
    {
        if (isTitan)
            return;


        bool doneBySelf =
            (collisionInstance.sourceCollider.GetComponentInParent<Item>()?.handlers
                .Contains(Player.currentCreature.handLeft) ?? false) ||
            (collisionInstance.sourceCollider.GetComponentInParent<Item>()?.handlers
                .Contains(Player.currentCreature.handRight) ?? false);

        if (!doneBySelf)
            return;


        if (collisionInstance.damageStruct.damageType == DamageType.Pierce)
            if (collisionInstance.damageStruct.penetrationDepth > 0.2f)
                OnShift(true);


        if (collisionInstance.damageStruct.damageType == DamageType.Slash)
            OnShift(false);
    }

    protected virtual void OnShift(bool abilityShift)
    {
        if (isTitan)
            return;
        Debug.Log("Titan Shifting");
        isTitan = true;
        Player.currentCreature.handLeft.caster.DisableSpellWheel(this);
        Player.currentCreature.handRight.caster.DisableSpellWheel(this);

        spellCaster.ragdollHand.otherHand.caster.LoadSpell(Catalog.GetData<SpellCastData>(id));
        SummonTitan();
        if (abilityShift)
            OnSpecialShift();
    }

    protected virtual void OnSpecialShift()
    {
        
    }
    


    protected void SummonTitan()
    {
        Catalog.InstantiateAsync(titanAddress, Player.local.transform.position,
            Player.local.transform.rotation, null,
            o =>
            {
                titan = o;
                titan.transform.localScale = Vector3.one * ScaleUniversal;
                var height = o.transform.FindChildRecursiveTR("Scale").position.y -
                             o.transform.FindChildRecursiveTR("CreatureLocation").position.y;

                var q = o.AddComponent<VRIK>();
                q.AutoDetectReferences();

                var th = new GameObject("j");
                th.transform.position = Player.local.head.transform.position;
                th.transform.parent = Player.local.head.transform;
                th.transform.localPosition = new Vector3(0, 0, -0.1f);
                th.transform.localRotation = Quaternion.Euler(0, -90, -90);

                var thr = new GameObject("j2");
                thr.transform.position = Player.local.handRight.transform.position;
                thr.transform.parent = Player.local.handRight.transform;
                thr.transform.localPosition = new Vector3(0, 0, 0);
                thr.transform.localRotation = Quaternion.Euler(0, 90, 90);

                var thl = new GameObject("j3");
                thl.transform.position = Player.local.handLeft.transform.position;
                thl.transform.parent = Player.local.handLeft.transform;
                thl.transform.localPosition = new Vector3(0, 0, 0);
                thl.transform.localRotation = Quaternion.Euler(0, -90, 90);

                q.solver.spine.headTarget = th.transform;
                q.solver.leftArm.target = thl.transform;
                q.solver.rightArm.target = thr.transform;
                q.solver.locomotion.footDistance = footDistance;
                q.solver.locomotion.stepHeight =
                    new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, height / 3), new Keyframe(1, 0));
                q.solver.locomotion.stepSpeed = stepSpeed;
                q.solver.locomotion.maxVelocity = 30f;
                q.solver.locomotion.stepThreshold = height / 3;
                q.solver.plantFeet = false;

                Scale(height);

                Player.currentCreature.renderers.ForEach(r => r.renderer.enabled = false);
                o.transform.SetParent(Player.local.transform, true);

                SetHands(o);


                isTitan = true;
                Player.currentCreature.HideItemsInHolders(false);
            }, "gd");
    }

    protected abstract void SetHands(GameObject o);


    protected void Scale(float scale)
    {
        lastHeight = Player.local.creature.morphology.height;
        Player.local.creature.morphology.height = scale * ScaleUniversal;
        Player.local.transform.localScale = (Vector3.one * ScaleUniversal) *
                                            (Player.local.creature.morphology.height /
                                             Player.characterData.calibration.height);
        if (Player.local?.footLeft != null)
        {
            Player.local.footLeft.playerMinHeight = 0.09f;
        }

        if (Player.local?.footRight != null)
        {
            Player.local.footRight.playerMinHeight = 0.09f;
        }

        Player.local?.creature?.RefreshMorphology();
        Player.local?.creature?.currentLocomotion?.SetCapsuleCollider(Player.local.creature.morphology.legsLength);
        Player.local?.creature?.RefreshJointForceMultipliers();
        Player.local?.creature?.currentLocomotion?.RefreshPhysicModifiers();
        Player.local?.creature?.currentLocomotion?.RefreshSpeedModifiers();
        Player.local?.handLeft?.link?.RefreshJointConfig();
        Player.local?.handLeft?.link?.RefreshJointModifiers();
        Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshJointDrive();
        Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshJointModifiers();
        Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshAllJointDrives();
        Player.local?.handRight?.link?.RefreshJointConfig();
        Player.local?.handRight?.link?.RefreshJointModifiers();
        Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshJointDrive();
        Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshJointModifiers();
        Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshAllJointDrives();
        Player.local.locomotion.colliderRadius = 0.3f * Player.local.transform.localScale.x;
        Player.local.locomotion.groundDetectionDistance = 0.05f * Player.local.transform.localScale.x;
        Player.currentCreature.ragdoll.SetColliders(false);
        Player.local.airHelper.minHeight = 1 * Player.local.transform.localScale.x;
    }

    void UnScale()
    {
        Player.local.creature.morphology.height = lastHeight;
        Player.local.transform.localScale = Vector3.one;
        
        Player.local?.creature?.RefreshMorphology();
        Player.local?.creature?.currentLocomotion?.SetCapsuleCollider(Player.local.creature.morphology.legsLength);
        Player.local?.creature?.RefreshJointForceMultipliers();
        Player.local?.creature?.currentLocomotion?.RefreshPhysicModifiers();
        Player.local?.creature?.currentLocomotion?.RefreshSpeedModifiers();
        Player.local?.handLeft?.link?.RefreshJointConfig();
        Player.local?.handLeft?.link?.RefreshJointModifiers();
        Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshJointDrive();
        Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshJointModifiers();
        Player.local?.handLeft?.ragdollHand?.grabbedHandle?.RefreshAllJointDrives();
        Player.local?.handRight?.link?.RefreshJointConfig();
        Player.local?.handRight?.link?.RefreshJointModifiers();
        Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshJointDrive();
        Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshJointModifiers();
        Player.local?.handRight?.ragdollHand?.grabbedHandle?.RefreshAllJointDrives();
        Player.local.locomotion.colliderRadius = 0.3f * Player.local.transform.localScale.x;
        Player.local.locomotion.groundDetectionDistance = 0.05f * Player.local.transform.localScale.x;
        Player.currentCreature.ragdoll.SetColliders(true);
        Player.local.airHelper.minHeight = 1 * Player.local.transform.localScale.x;
    }

    public override void UpdateCaster()
    {
        base.UpdateCaster();
        
        if(!isTitan)
            return;
        
        float current = Player.local.head.transform.localRotation.eulerAngles.x;
        float delta = Mathf.DeltaAngle(lastHeadRotation, current);

        if (delta > 1f || delta < -1f)
        Debug.Log($"Current: {current}  Last: {lastHeadRotation}  Delta: {delta}");
        
        if (Player.local.handRight.controlHand.alternateUsePressed &&
            Player.local.handLeft.controlHand.alternateUsePressed)
        {

            if (delta > 3f)
            {
                Debug.Log("Unshift");
                OnUnshift();
            }

        }
        
        
        lastHeadRotation = Player.local.head.transform.localRotation.eulerAngles.x;
    }


    protected virtual void OnUnshift()
    {
        isTitan = false;
        Player.currentCreature.handLeft.caster.AllowSpellWheel(this);
        Player.currentCreature.handRight.caster.AllowSpellWheel(this);
        titan?.transform.SetParent(null);
        Object.Destroy(titan?.GetComponent<VRIK>());
        Object.Destroy(titan);
        titan = null;
        UnScale();
    }


    public sealed override void Unload()
    {
        base.Unload();
        isTitan = false;
        OnUnshift();
        Player.selfCollision = false;
        Player.currentCreature.OnDamageEvent -= CurrentCreatureOnOnDamageEvent;
        
        #if DEBUG
            spellCaster.ragdollHand.playerHand.OnFistEvent -= PlayerHandOnOnFistEvent;
        #endif

    }
}