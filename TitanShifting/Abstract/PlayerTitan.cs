#define DEBUG // enable Titan Transofrm on fist event .

using System.Collections;
using IngameDebugConsole;
using RootMotion.Demos;
using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.Abstract;


public abstract class PlayerTitanBase : SpellCastCharge
{
    [ModOption]
    [ModOptionFloatValues(0.1f, 1f, 0.05f)]
    public static float ScaleUniversal = 1.0f;
    public static bool isTitan;
    private static bool isTransforming;
    static float lastHeight;
    TitanHand leftTitanHand;
    TitanHand rightTitanHand;
    
    protected static GameObject titan;

    public abstract string titanAddress { get; }
    public abstract float footDistance { get; }
    
    public abstract float stepSpeed { get; }
    public abstract float maxHealth { get; }
    public abstract float jumpForce { get; }
    public abstract float speedMultiplier { get; }
    
    public abstract float handWeight { get; }
    
    float lastHeadRotation;
    
    

    public sealed override void Load(SpellCaster spellCaster)
    {
        base.Load(spellCaster);
        Player.selfCollision = true;
        Player.currentCreature.OnDamageEvent += CurrentCreatureOnOnDamageEvent;
        Player.currentCreature.OnKillEvent += CurrentCreatureOnOnKillEvent;
        Debug.Log("Loaded " + this.GetType().Name);
        
        #if DEBUG
        spellCaster.ragdollHand.playerHand.OnFistEvent += PlayerHandOnOnFistEvent;
        #endif
    }

    private void CurrentCreatureOnOnKillEvent(CollisionInstance collisionInstance, EventTime eventTime)
    {
        titan = null;
        isTitan = false;
        isTransforming = false;
    }

    private void PlayerHandOnOnFistEvent(PlayerHand hand, bool gripping)
    {
        if (gripping && !isTitan)
        {
            Debug.Log("Titan Shifting due to debug fist event");
            OnShift(false);
        }
        else if (isTitan)
        {
            Debug.Log("Titan Unshift due to debug fist event");
            OnUnshift();
        }
        
        

    }

    private void CurrentCreatureOnOnDamageEvent(CollisionInstance collisionInstance, EventTime eventTime)
    {
        if (isTitan || isTransforming)
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

    [ConsoleMethod("TitanShift", "Shifts the player into a Titan form")]
    protected virtual void OnShift(bool abilityShift)
    {
        if (isTitan || isTransforming)
            return;
        Debug.Log("Titan Shifting");
        isTitan = true;
        isTransforming = true;
        Player.currentCreature.handLeft.caster.DisableSpellWheel(this);
        Player.currentCreature.handRight.caster.DisableSpellWheel(this);

        spellCaster.ragdollHand.otherHand.caster.LoadSpell(Catalog.GetData<SpellCastData>(id));
        Player.currentCreature.StartCoroutine(SummonTitan());
        if (abilityShift)
            OnSpecialShift();
    }

    protected virtual void OnSpecialShift()
    {
        
    }
    


    protected IEnumerator SummonTitan()
    {
        var shiftEffects = new GameObject("ShiftEffects");
        shiftEffects.transform.position = Player.local.transform.position;
        shiftEffects.transform.rotation = Player.local.transform.rotation;
        shiftEffects.transform.localScale = Vector3.one;
        shiftEffects.transform.SetParent(Player.local.transform, true);
        var effectGenerator = shiftEffects.AddComponent<ShiftEffectGenerator>();
        
        Player.currentCreature.StartCoroutine(effectGenerator.Activate());
        yield return new WaitForSeconds(effectGenerator.duration);
        Catalog.InstantiateAsync(titanAddress, Player.local.transform.position,
            Player.local.transform.rotation, null,
            o =>
            {
                titan = o;
                DisableRagdoll();
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
                thr.transform.localRotation = Quaternion.Euler(0, 90, -90);

                var thl = new GameObject("j3");
                thl.transform.position = Player.local.handLeft.transform.position;
                thl.transform.parent = Player.local.handLeft.transform;
                thl.transform.localPosition = new Vector3(0, 0, 0);
                thl.transform.localRotation = Quaternion.Euler(0, -90, 90);

                
                q.solver.spine.headTarget = th.transform;

                SetHands(o);

                leftTitanHand = null;
                rightTitanHand = null;

                foreach (var titanHand in o.GetComponentsInChildren<TitanHand>(true))
                {
                    if (titanHand.side == Side.Left)
                        leftTitanHand = titanHand;
                    else if (titanHand.side == Side.Right)
                        rightTitanHand = titanHand;
                }

                if (leftTitanHand == null || rightTitanHand == null)
                {
                    Debug.LogError("Titan hand setup failed: left or right TitanHand was not found.");
                }
                else
                {
                    
                    leftTitanHand.controllerMass = handWeight;
                    rightTitanHand.controllerMass = handWeight;

                    leftTitanHand.ConfigureControllerMass(thl.transform);
                    rightTitanHand.ConfigureControllerMass(thr.transform);
                    


                    q.solver.leftArm.target = leftTitanHand.IkTarget;
                    q.solver.rightArm.target = rightTitanHand.IkTarget;
                }
                
                
                
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
                

                isTransforming = false;
                isTitan = true;
                Player.currentCreature.HideItemsInHolders(true);
            }, "gd");
    }

    protected abstract void SetHands(GameObject o);


    protected void Scale(float scale)
    {
        if(lastHeight == 0)
            lastHeight = Player.local.creature.morphology.height;
        Player.local.creature.morphology.height = scale * ScaleUniversal;
        Player.local.transform.localScale = Vector3.one * (ScaleUniversal * (Player.local.creature.morphology.height /
                                                                             Player.characterData.calibration.height));
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

        Player.local.creature.currentLocomotion.SetAllSpeedModifiers("creeg", speedMultiplier); 
        Player.local.creature.currentLocomotion.jumpGroundForce = jumpForce;
        Player.local.creature.healthModifier.Add("Trog", maxHealth);
            
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
        
        Player.local.creature.currentLocomotion.jumpGroundForce = 0.3f;
        Player.local.creature.healthModifier.Remove("Trog");
        Player.local.creature.currentLocomotion.RemoveSpeedModifier("creeg");
        Player.local.creature.currentLocomotion.ClearSpeedModifiers();
        
        
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
            OnUnshift();
            isTransforming = true;
            if (delta > 3f)
            {
                Debug.Log("Unshift");
                
            }

        }
        
        
        lastHeadRotation = Player.local.head.transform.localRotation.eulerAngles.x;
    }

    
    [ConsoleMethod("TitanUnshift", "Unshifts the player back to the original form")]
    protected virtual void OnUnshift()
    {
        if (!isTitan || isTransforming)
            return;
       
        
        
        Object.Destroy(Player.local.head.transform.Find("j")?.gameObject);
        Object.Destroy(Player.local.handRight.transform.Find("j2")?.gameObject);
        Object.Destroy(Player.local.handLeft.transform.Find("j3")?.gameObject);

        Object.Destroy(leftTitanHand);
        Object.Destroy(rightTitanHand);
        
        
        var oldLossy = titan.transform.lossyScale;
        
        UnScale();
        
        if(Player.currentCreature == null)
            return;
        
        Player.currentCreature.handLeft.caster.AllowSpellWheel(this);
        Player.currentCreature.handRight.caster.AllowSpellWheel(this);
        titan?.transform.SetParent(null, true);

        titan.transform.localScale = oldLossy;
        Player.currentCreature.renderers.ForEach(r => r.renderer.enabled = true);
        Player.currentCreature.HideItemsInHolders(false);
        Object.Destroy(titan?.GetComponent<VRIK>());
        
        foreach (Rigidbody r in titan.GetComponentsInChildren<Rigidbody>(true))
        {
            r.isKinematic = true;
            r.velocity = Vector3.zero;
            r.angularVelocity = Vector3.zero; 
            r.constraints = RigidbodyConstraints.FreezeAll;
        } 
        
        

        
        var unspawnLocation = titan.transform.FindChildRecursive("PlayerSpawnUnshift");
        var handlockL = titan.transform.FindChildRecursive("PlayerHandLockL"); 
        var handLockR = titan.transform.FindChildRecursive("PlayerHandLockR");

        //Player.local.handLeft.ragdollHand.transform.position = handlockL.position;
        //Player.local.handRight.ragdollHand.transform.position = handLockR.position;
        
        //Player.local.Teleport(unspawnLocation, false, true);


        Player.local.locomotion.allowMove = false;
        Player.local.locomotion.physicBody.useGravity = false;
        Player.local.locomotion.physicBody.velocity = Vector3.zero;
        
        //titan.transform.FindChildRecursive("head").transform.localRotation = Quaternion.Euler(0, 0, 100);
        //var lockL = Player.local.handLeft.ragdollHand.GetOrAddComponent<OneWayHandLock>();
        //var lockR = Player.local.handRight.ragdollHand.GetOrAddComponent<OneWayHandLock>();
        //lockL.target = handlockL;
        //lockR.target = handLockR;

        Player.local.StartCoroutine(WaitForJointExit(/*lockL, lockR*/));
    }


    IEnumerator WaitForJointExit(/*OneWayHandLock lockL, OneWayHandLock lockR*/)
    {

        //yield return new WaitUntil(() => { return (lockL.IsBroken && lockR.IsBroken); });
        if (Player.currentCreature != null)
        {
            Player.local.locomotion.physicBody.useGravity = true;
            Player.local.locomotion.physicBody.velocity = Vector3.zero;
            Player.local.locomotion.allowMove = true;

            yield return new WaitForFixedUpdate();
            
            RagdollTitan();
            Player.currentCreature.ragdoll.SetColliders(true);
            var smoke = titan.transform.FindChildRecursive("TitanSmoke");
            var flames = titan.transform.FindChildRecursive("TitanFlames");
            smoke.gameObject.SetActive(true);
            flames.gameObject.SetActive(true);
            yield return new WaitForSeconds(20f);
            smoke.SetParent(null, true);
            flames.SetParent(null, true);

            var smokePs = smoke.GetComponent<ParticleSystem>();
            smokePs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Object.Destroy(smoke.gameObject, smokePs.main.startLifetime.constantMax);

            var flamesPs = flames.GetComponent<ParticleSystem>();
            flamesPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Object.Destroy(flames.gameObject, flamesPs.main.startLifetime.constantMax);
            Object.Destroy(titan);
        }

        isTitan = false;
        isTransforming = false;
        titan = null;
        
    }

    void DisableRagdoll()
    {
        foreach (Rigidbody r in titan.GetComponentsInChildren<Rigidbody>(true))
        {
            r.isKinematic = true;
            r.useGravity = false;
        }
    }

    void RagdollTitan()
    {
        Debug.Log("RagdollTitan BEFORE");

        foreach (Rigidbody rb in titan.GetComponentsInChildren<Rigidbody>(true))
        {
            Debug.Log($"{rb.name} BEFORE: {rb.velocity}");
        }

        // DON'T enable physics
        foreach (Rigidbody rb in titan.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("RagdollTitan AFTER");

        foreach (Rigidbody rb in titan.GetComponentsInChildren<Rigidbody>(true))
        {
            Debug.Log($"{rb.name} AFTER: {rb.velocity}");
        }
    }


    public sealed override void Unload()
    {
        base.Unload();
        OnUnshift();
        Player.selfCollision = false;
        if (Player.currentCreature != null)
        {
            Player.currentCreature.OnDamageEvent -= CurrentCreatureOnOnDamageEvent;
            Player.currentCreature.OnKillEvent -= CurrentCreatureOnOnKillEvent;
        }
        #if DEBUG
        if(spellCaster != null)
            spellCaster.ragdollHand.playerHand.OnFistEvent -= PlayerHandOnOnFistEvent;
        #endif

    }
}