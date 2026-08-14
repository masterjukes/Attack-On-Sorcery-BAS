
using System;
using System.Collections;
using System.Threading.Tasks;
using IngameDebugConsole;
using RootMotion.Demos;
using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.Yielders;
using Object = UnityEngine.Object;

namespace BladeAndTitan.TitanShifting.Abstract;


class Handles
{
    public Handle leftHandle;
    public Handle rightHandle;
}

public abstract class PlayerTitanBase : SpellCastCharge
{
    public static bool isTitan;
    public static bool isTransforming;
    static float lastHeight;
    static TitanHand leftTitanHand;
    static TitanHand rightTitanHand;
    static Transform unspawnLocation;

    protected static Transform leftFoot;
    protected static Transform rightFoot;
    
    protected static GameObject titan;

    protected static bool isTransformingIn;

    public abstract string titanAddress { get; }
    public abstract float footDistance { get; }
    
    public abstract float stepSpeed { get; }
    
    public virtual string stepSoundId { get; }
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
            OnShift();
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
            OnShift();

        if (collisionInstance.damageStruct.damageType == DamageType.Slash)
            OnShift();
    }

    [ConsoleMethod("TitanShift", "Shifts the player into a Titan form")]
    protected virtual void OnShift()
    {
        if (isTitan || isTransforming)
            return;
        Debug.Log("Titan Shifting");
        isTitan = true;
        isTransformingIn = true;
        isTransforming = true;
        Player.currentCreature.handLeft.caster.DisableSpellWheel(this);
        Player.currentCreature.handRight.caster.DisableSpellWheel(this);

        if( spellCaster.ragdollHand.otherHand.caster.spellInstance?.id != spellCaster.spellInstance.id )
            spellCaster.ragdollHand.otherHand.caster.LoadSpell(Catalog.GetData<SpellCastData>(id));
        Player.currentCreature.StartCoroutine(SummonTitan());
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
                titan.transform.localScale = Vector3.one;
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

                q.solver.locomotion.onLeftFootstep.AddListener(OnLeftFootstep);
                q.solver.locomotion.onRightFootstep.AddListener(OnRightFootstep);
                
                q.solver.spine.headTarget = th.transform;

                leftFoot = titan.transform.FindChildRecursive("LeftFoot");
                rightFoot = titan.transform.FindChildRecursive("RightFoot");
                
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
                
                isTransformingIn = false;
                isTransforming = false;
                isTitan = true;
                Player.currentCreature.HideItemsInHolders(true);
                
                OnTitanPossess();
                
            }, "gd");
    }

    protected virtual void OnLeftFootstep()
    {
        PlaySound(stepSoundId, leftFoot.position);
    }

    protected virtual void OnRightFootstep()
    {
        PlaySound(stepSoundId, rightFoot.position);
    }

    protected abstract void SetHands(GameObject o);

    protected virtual void OnTitanPossess()
    {
        
    }
    
    protected void Scale(float scale)
    {
        if(lastHeight == 0)
            lastHeight = Player.local.creature.morphology.height;
        Player.local.creature.morphology.height = scale;
        Player.local.transform.localScale = Vector3.one * ((Player.local.creature.morphology.height /
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

        
        if (Player.local.handRight.controlHand.alternateUsePressed &&
            Player.local.handLeft.controlHand.alternateUsePressed)
        {

            if (delta > 3f)
            {
                OnUnshift();
                isTransforming = true;
            }

        }
        
        
        lastHeadRotation = Player.local.head.transform.localRotation.eulerAngles.x;
    }

    

    protected virtual void OnUnshift()
    {
        if (!isTitan || isTransforming)
            return;

        isTransforming = true;

        var vrik = titan.GetComponent<VRIK>();
        if (vrik != null)
            vrik.enabled = false;

        Vector3 titanPosition = titan.transform.position;
        Quaternion titanRotation = titan.transform.rotation;
        Vector3 titanScale = titan.transform.lossyScale;

        titan.transform.SetParent(null, true);

        titan.transform.position = titanPosition;
        titan.transform.rotation = titanRotation;
        titan.transform.localScale = titanScale;

        UnScale();
        
        
        unspawnLocation = titan.transform.FindChildRecursive("PlayerSpawnUnshift");
        var handlockL = titan.transform.FindChildRecursive("PlayerHandLockL"); 
        var handLockR = titan.transform.FindChildRecursive("PlayerHandLockR");


        var handles = new Handles();

        Transform neck = titan.transform.FindChildRecursive("neck");
        
        /*
        
        Catalog.GetData<ItemData>("FoodBirdEgg").SpawnAsync(item =>
        {
            var joint = item.gameObject.AddComponent<ConfigurableJoint>();
            var lockRb = handlockL.GetOrAddComponent<Rigidbody>();
            lockRb.useGravity = false;
            lockRb.isKinematic = true;
            joint.connectedBody = lockRb;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

// Allow the hand to move, but resist it.
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit limit = joint.linearLimit;
            limit.limit = 0.05f; // 5cm of wiggle room
            joint.linearLimit = limit;
            SoftJointLimitSpring spring = joint.linearLimitSpring;

            spring.spring = 5000f; // how strongly it pulls back
            spring.damper = 100f;  // how much it dampens movement

            joint.linearLimitSpring = spring;
            handles.leftHandle = item.GetMainHandle(Side.Left);
            foreach (var itemRenderer in item.renderers)
            {
               itemRenderer.enabled = false; 
            }
            

            var lr = item.GetOrAddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, item.transform.position);
            lr.SetPosition(1, neck.position);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            if (lr.material == null)
                lr.material = new Material(Shader.Find("Standard"));
            lr.startWidth = 0.01f;
            lr.endWidth = 0.01f;
            lr.startColor = Color.red;
            lr.endColor = Color.red;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            item.GetOrAddComponent<LineRendererController>().target = neck;

            Player.local.handLeft.ragdollHand.Grab(item.GetMainHandle(Side.Left), true);
        });
        
        Catalog.GetData<ItemData>("FoodBirdEgg").SpawnAsync(item =>
        {
            var joint = item.gameObject.AddComponent<ConfigurableJoint>();
            var lockRb = handLockR.GetOrAddComponent<Rigidbody>();
            lockRb.useGravity = false;
            lockRb.isKinematic = true;
            joint.connectedBody = lockRb;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

// Allow the hand to move, but resist it.
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit limit = joint.linearLimit;
            limit.limit = 0.05f; // 5cm of wiggle room
            joint.linearLimit = limit;
            SoftJointLimitSpring spring = joint.linearLimitSpring;

            spring.spring = 5000f; // how strongly it pulls back
            spring.damper = 100f;  // how much it dampens movement

            joint.linearLimitSpring = spring;


            handles.rightHandle = item.GetMainHandle(Side.Right);
            foreach (var itemRenderer in item.renderers)
            {
                itemRenderer.enabled = false; 
            }
            
            var lr = item.GetOrAddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, item.transform.position);
            lr.SetPosition(1, neck.position);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            if (lr.material == null)
                lr.material = new Material(Shader.Find("Standard"));
            lr.startWidth = 0.01f;
            lr.endWidth = 0.01f;
            lr.startColor = Color.red;
            lr.endColor = Color.red;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            item.GetOrAddComponent<LineRendererController>().target = neck;
            Player.local.handRight.ragdollHand.Grab(item.GetMainHandle(Side.Right), true);
            Player.currentCreature.HideItemsInHolders(false); 
        }); 
        
        Player.local.Teleport(unspawnLocation);
       */
        
        Object.Destroy(Player.local.head.transform.Find("j")?.gameObject);
        Object.Destroy(Player.local.handRight.transform.Find("j2")?.gameObject);
        Object.Destroy(Player.local.handLeft.transform.Find("j3")?.gameObject);

        Object.Destroy(leftTitanHand);
        Object.Destroy(rightTitanHand);

        Player.currentCreature.handLeft.caster.AllowSpellWheel(this);
        Player.currentCreature.handRight.caster.AllowSpellWheel(this);

        Player.local.locomotion.allowMove = false;
        Player.local.locomotion.physicBody.useGravity = false;
        Player.local.locomotion.physicBody.velocity = Vector3.zero;
        
        

        Player.currentCreature.renderers.ForEach(r => r.renderer.enabled = true);
        Player.local.StartCoroutine(WaitForJointExit(handles));
    }

    

    


    IEnumerator WaitForJointExit(Handles handles)
    {
        /*
        yield return new WaitUntil(() => (handles.leftHandle != null && handles.rightHandle != null));
        
        var leftJoint = handles.leftHandle.item.gameObject.GetComponent<ConfigurableJoint>();
        var rightJoint = handles.rightHandle.item.gameObject.GetComponent<ConfigurableJoint>();
        

     
        
        
        while (leftJoint != null || rightJoint != null)
        {
            //Player.local.Teleport(unspawnLocation, false, false);

            if (Player.local.handLeft.ragdollHand.grabbedHandle != handles.leftHandle)
            {
                Player.local.handLeft.ragdollHand.UnGrab(false);
                Player.local.handLeft.ragdollHand.Grab(handles.leftHandle, false);
            }

            if (Player.local.handRight.ragdollHand.grabbedHandle != handles.rightHandle)
            {
                Player.local.handRight.ragdollHand.UnGrab(false);
                Player.local.handRight.ragdollHand.Grab(handles.rightHandle, false);
            }
            
            if (Player.local.handRight.controlHand.GetHandVelocity().magnitude > 3f)
            {
                Object.Destroy(leftJoint);
                leftJoint = null;
            }

            if (Player.local.handLeft.controlHand.GetHandVelocity().magnitude > 3f)
            {
                Object.Destroy(rightJoint);
                rightJoint = null;
            }
            

            
            yield return Yielders.EndOfFrame;
        }
        
        
        handles.leftHandle.item.Despawn();
        handles.rightHandle.item.Despawn();
        */
        
        ResetVelocity();
        
        if (Player.currentCreature != null)
        {
            Player.local.locomotion.physicBody.useGravity = true;
            Player.local.locomotion.physicBody.velocity = Vector3.zero;
            Player.local.locomotion.allowMove = true;

            ResetRagdollHandLinks();
            
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

    void ResetVelocity()
    {
        foreach (Rigidbody r in titan.GetComponentsInChildren<Rigidbody>(true))
        {
            r.velocity = Vector3.zero;
            r.angularVelocity = Vector3.zero;
        }
    }

    void RagdollTitan()
    {
        Rigidbody[] bodies = titan.GetComponentsInChildren<Rigidbody>(true);

        Physics.SyncTransforms();
        AlignRagdollJoints();

        foreach (Rigidbody rb in bodies)
        {
            rb.position = rb.transform.position;
            rb.rotation = rb.transform.rotation;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.constraints = RigidbodyConstraints.None;
        }

        Physics.SyncTransforms();

        foreach (Collider c in titan.GetComponentsInChildren<Collider>(true))
        {
            c.enabled = true;
        }


        
        foreach (Rigidbody rb in bodies)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        foreach (Rigidbody rb in bodies)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    void AlignRagdollJoints()
    {
        foreach (CharacterJoint joint in titan.GetComponentsInChildren<CharacterJoint>(true))
        {
            if (joint.connectedBody == null)
                continue;

            Vector3 worldAnchor =
                joint.transform.TransformPoint(joint.anchor);

            joint.autoConfigureConnectedAnchor = false;

            joint.connectedAnchor =
                joint.connectedBody.transform.InverseTransformPoint(worldAnchor);
        }

        Physics.SyncTransforms();
    }

    private void ResetRagdollHandLinks()
    {
        Catalog.GetData<ItemData>("FoodBirdEgg").SpawnAsync(async void (item) =>
        {
            
            if(Player.currentCreature.handRight.grabbedHandle)
                Player.currentCreature.handRight.UnGrab(false);
            Player.currentCreature.handRight.Grab(item.mainHandleRight);
            await Task.Delay((int)(Time.deltaTime * 1000));
            Player.currentCreature.handRight.UnGrab(false);
            item.Despawn();
            
        });
        
        
        Catalog.GetData<ItemData>("FoodBirdEgg").SpawnAsync(async void (item) =>
        {
            
            if(Player.currentCreature.handLeft.grabbedHandle)
                Player.currentCreature.handLeft.UnGrab(false);
            Player.currentCreature.handLeft.Grab(item.mainHandleLeft);
            await Task.Delay((int)(Time.deltaTime * 1000));
            Player.currentCreature.handLeft.UnGrab(false);
            item.Despawn();
            
        }); 
        
    }

    public void PlaySound(string soundId, Vector3 position)
    {
        Catalog.LoadAssetAsync<AudioContainer>(soundId,
            sound => sound.GetRandomAudioClip().PlayClipAtPoint(position, 1f, AudioMixerName.Effect), soundId);
    }
    public sealed override void Unload()
    {
        base.Unload();
        OnUnshift();
        isTransformingIn = false;
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