using System.Collections;
using System.Collections.Generic;
using BladeAndTitan.DebugHelpers;
using BladeAndTitan.Titans.LookAnimator;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;

namespace BladeAndTitan.Titans.Generic;

public class GenericTitanAI : AIBase
{
    private static readonly int Kneel = Animator.StringToHash("Kneel");
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Walk = Animator.StringToHash("Walk");
    private static readonly int PickUp = Animator.StringToHash("PickUp");
    private static readonly int Jump = Animator.StringToHash("Jump");

    private static List<Creature> grabbedCreatures = new List<Creature>();

    public enum AIBehaviourMode
    {
        Roaming,
        Chasing
    }

    
    public AIBehaviourMode behaviourMode;

    public Vector3 target;
    public Vector3 roamAreaCenter;

    public int maxDestinationAttempts = 10;
    int titansChasingPlayer = 0;

    private const string ForestCenter = "ForestCenter";
    private const string CastleCenter = "CastleCenter";
    private const string CountryCenter = "CountryCenter";
    private const string CityCenter = "CityCenter";
    
    private bool kneeling;
    private bool eyesDestroyed;
    private bool pickingUp;
    public bool grabbing;

    private bool isAbberant;
    
    public static AnimationClip[] walkAnimationClips;
    public static AnimationClip[] runAnimationClips;
    private static readonly int Eat = Animator.StringToHash("Eat");


    float turnSpeed = 50f;
    float walkSpeed = 5.5f;
    float runSpeed = 15f;
    
    FLookAnimator fLookAnimator;

    private Transform leftGrabPos;
    private Transform rightGrabPos;
    
    
    bool isAnimationTriggerRunning => kneeling || eyesDestroyed || pickingUp;

    protected override void Start()
    {
        base.Start();
        var un = GameObject.Find(ForestCenter);
        var de = GameObject.Find(CastleCenter);
        var tr = GameObject.Find(CountryCenter);
        var qa = GameObject.Find(CityCenter);
        
        isAbberant = Random.Range(0, 6) == 4;
        
        

        
        var choice = Random.Range(0, 4);
        switch (choice)
        {
            case 0: roamAreaCenter = un.transform.position; break;
            case 1: roamAreaCenter = de.transform.position; break;
            case 2: roamAreaCenter = tr.transform.position; break;
            case 3: roamAreaCenter = qa.transform.position; break;
        }
        
        float num3 = Random.Range(1.5f, 3f);
        float num4 = Mathf.Lerp(1.2f, 0.7f, num3 / 3f);
        
        anim.SetFloat("Multiplier", num4);
        
        
        AnimatorOverrideController overrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);

        var walkAnimationClip = Random.Range(0, walkAnimationClips.Length);

        if (isAbberant)
        {
            overrideController["Run_Normal"] = runAnimationClips[Random.Range(0, runAnimationClips.Length)];
            turnSpeed = 100f;
            runSpeed *= num4;
        }
        else
        {
            overrideController["Run_Normal"] = walkAnimationClips[walkAnimationClip];
            turnSpeed = 50f;
            runSpeed = walkSpeed * num4;
        }
        
        turnSpeed *= num4;
        agent.angularSpeed = turnSpeed;
        walkSpeed *= num4;
        agent.speed = walkSpeed;
        
        overrideController["Walk"] = walkAnimationClips[walkAnimationClip];
        
        anim.runtimeAnimatorController = overrideController;
        
        
        
        
        anim.SetBool(Walk, true);


        fLookAnimator = titan.GetOrAddComponent<LookAnimator.FLookAnimator>();
        fLookAnimator.FindHeadBone();
        fLookAnimator.SetLookTarget(Player.local.head.transform);
        fLookAnimator.enabled = false;
        
        leftGrabPos = transform.FindChildRecursive("LeftGrabPos");
        rightGrabPos = transform.FindChildRecursive("RightGrabPos");

        leftGrabPos.GetOrAddComponent<TitanGrabTrigger>();
        rightGrabPos.GetOrAddComponent<TitanGrabTrigger>();
        
        
        titan.OnLimbDestroy += TitanOnOnLimbDestroy;
        
        ChangeDestination();
    }

    private void TitanOnOnLimbDestroy(TitanLimb limb)
    {
        Debug.Log($"Limb Destroyed of type {limb.type}");
        if (limb.type == TitanLimb.LimbType.RightLeg || limb.type == TitanLimb.LimbType.LeftLeg)
        {
            StartCoroutine(LegDestroyRoutine());
        }
        else if(limb.type == TitanLimb.LimbType.Eye)
        {
            StartCoroutine(EyesDestroyRoutine());
        }
        
    }


    IEnumerator LegDestroyRoutine()
    {
        if (!isAnimationTriggerRunning)
        {
            kneeling = true;
            anim.ResetTrigger(Kneel);
            anim.SetTrigger(Kneel);

            var previousSpeed = agent.speed;
            var turnSpeed = agent.angularSpeed;

            agent.speed = 0f;
            agent.angularSpeed = 0f;
            yield return new WaitForSeconds(6f);
            agent.speed = previousSpeed;
            agent.angularSpeed = turnSpeed;
            yield return new WaitForSeconds(1f);
            kneeling = false;
        }

    }

    IEnumerator EyesDestroyRoutine()
    {
        if (!isAnimationTriggerRunning)
        {
            eyesDestroyed = true;
            anim.ResetTrigger("EyesHit");
            anim.SetTrigger("EyesHit");
            var previousSpeed = agent.speed;
            var turnSpeed = agent.angularSpeed;

            agent.speed = 0f;
            agent.angularSpeed = 0f;
            yield return new WaitForSeconds(6f);
            agent.speed = previousSpeed;
            agent.angularSpeed = turnSpeed;

        }
    }

    public IEnumerator PickUpRoutine()
    {
        if(!isAnimationTriggerRunning)
        {
            pickingUp = true;
            anim.ResetTrigger(PickUp);
            anim.SetTrigger(PickUp);
            var previousSpeed = agent.speed;
            agent.speed = 1f;
            yield return new WaitForSeconds(3f);
            agent.speed = previousSpeed;
            pickingUp = false;
        }
    }
    
    public IEnumerator JumpRoutine()
    {
        if(!isAnimationTriggerRunning)
        {
            pickingUp = true;
            anim.ResetTrigger(Jump);
            anim.SetTrigger(Jump);
            var previousSpeed = agent.speed;
            agent.speed = 0f;
            yield return new WaitForSeconds(3f);
            foreach(Creature creature in Creature.InRadius(titan.transform.position, 2f))
                TitanEatTrigger.AttemptKill(creature);
            agent.speed = previousSpeed;
            pickingUp = false;
        }
    }

    public IEnumerator GrabRoutine(Creature creature, Side side)
    {
        if (grabbedCreatures.Contains(creature) || grabbing)
        {
            yield break;
        }
        grabbing = true;
        
        grabbedCreatures.Add(creature);
        
        anim.SetTrigger(Eat);
        
        var previousSpeed = agent.speed;
        var prevTurnSpeed = agent.angularSpeed;
        
        agent.speed = 0f;
        agent.angularSpeed = 0f;
        
        var grabPos = side == Side.Left ? leftGrabPos : rightGrabPos;


        if(!creature.isPlayer)
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);

        creature.ragdoll.SetColliders(false);

        
        while (creature.transform.position != grabPos.position)
        {
            var expectedPosition = Vector3.MoveTowards(creature.transform.position, grabPos.position, 35f * Time.deltaTime);

            if (creature.isPlayer)
                Player.local.Teleport(expectedPosition, Player.local.transform.rotation, false, false);
            else
                creature.Teleport(expectedPosition,creature.transform.rotation);
            
            if(Vector3.Distance(creature.transform.position, grabPos.position) < 0.5f)
                break;
            
            
            yield return Yielders.FixedUpdate;
        }
        
        
        
        
        while (grabbedCreatures.Contains(creature) && !creature.isKilled)
        {
            var titanPart = titan.GetPart(TitanLimb.LimbType.RightArm);
            if(side == Side.Left)
                titanPart = titan.GetPart(TitanLimb.LimbType.LeftArm);

            if (titanPart.isDisabled || Random.Range(0, 7000) == 3333)
                break;

            
            if(creature.isPlayer)
                Player.local.Teleport(grabPos.position, Player.local.transform.rotation, false, false);
            else
            {
                creature.RootPhysicBody.velocity = Vector3.zero;
                creature.transform.position = grabPos.position;

            }

            
            yield return Yielders.FixedUpdate;

        }
        
        Debug.Log("Escaped Grab");
        
        agent.speed = previousSpeed;
        agent.angularSpeed = prevTurnSpeed;

        creature.ragdoll.SetColliders(true);
        grabbedCreatures.Remove(creature);

        yield return Yielders.ForSeconds(5f);

        grabPos.GetComponent<TitanGrabTrigger>().enabled = true;
        grabbing = false;

        
    }
    
    
    
    

    public override void Update()
    {
        base.Update();
    }

    protected override void AITick()
    {
        RoamingUpdate();
        ChasingUpdate();
    }
    
    protected override void LimitedTick()
    {
        SimulateMovement();
    }

    protected override void FakeTick()
    {
        SimulateMovement();
    }

    public override void AIStateChanged(AIState prevState, AIState newState)
    {
        base.AIStateChanged(prevState, newState);
        if(newState == AIState.FullAI) 
            agent.Warp(transform.position);
    }

    void RoamingUpdate()
    {
        var randomChange = Random.Range(0, 1000) == 777;
        var agentBools = (!agent.hasPath ||
                          (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance));
        if (agentBools || randomChange)
        {
            ChangeDestination();
        }
    }
    
    
    
    
    
    void SimulateMovement()
    {
        
        if (target == Vector3.zero)
            ChangeDestination();
        Vector3 toTarget = target - transform.position;
        float distance = toTarget.magnitude;

        if (distance < 0.5f)
        {
            ChangeDestination();
            return;
        }

        float moveDistance = agent.speed * timeSinceLastTick;

        if (moveDistance >= distance)
        {
            transform.position = target;
        }
        else
        {
            transform.position += toTarget.normalized * moveDistance;
        }
    }


    public void ChangeDestination()
    {
        if (!agent.isOnNavMesh && state == AIState.FullAI) return;


        target = GetNavMeshTarget(roamAreaCenter, 400f, maxDestinationAttempts);

        if (state == AIState.FullAI)
            agent.SetDestination(target);


    }


    public Vector3 GetNavMeshTarget(Vector3 center, float radius, int attempts = 10)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 raw = center + new Vector3(offset.x, 0, offset.y);

            if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                continue;

            return hit.position;
        }

        return Vector3.zero;
    }
    

    void ChasingUpdate()
    {
        if (behaviourMode != AIBehaviourMode.Chasing) return;

        var random = Random.Range(0, 7000);
        if (random == 23)
            PlayAudio("TitanGroan", GetComponent<AudioSource>());

        if (!titan.movementDisabled)
        {
            if (Vector3.Distance(transform.position, Player.currentCreature.transform.position) > 400f)
            {
                ChangeDestination();
                SwitchBehaviourMode(AIBehaviourMode.Roaming);
                titansChasingPlayer--;
            }

            Creature nearestCreature = null;
            
            foreach (var creature in Creature.allActive)
            {
                if(creature.isKilled)
                    continue;
                
                if(nearestCreature == null)
                    nearestCreature = creature;
                
                if(Vector3.Distance(titan.transform.position, creature.transform.position) > Vector3.Distance(titan.transform.position, nearestCreature.transform.position))
                    nearestCreature = creature;
                
            }

            fLookAnimator.SetLookTarget(nearestCreature.transform);
            agent.SetDestination(nearestCreature.transform.position);

        }


    }
    
    

    public void SwitchBehaviourMode(AIBehaviourMode mode)
    {
        if (mode == AIBehaviourMode.Chasing)
        {
            agent.speed = runSpeed;
            anim.SetBool(Idle, value: false);
            anim.SetBool(Walk, value: false);
            
            fLookAnimator.enabled = true;
            titansChasingPlayer++;
        }

        if (behaviourMode == AIBehaviourMode.Chasing && mode == AIBehaviourMode.Roaming)
        {
            agent.speed = walkSpeed;
            anim.SetBool(Idle, value: true);
            anim.SetBool(Walk, value: true);
            fLookAnimator.enabled = false;
            titansChasingPlayer--;
        }

        behaviourMode = mode;

    }


    
}