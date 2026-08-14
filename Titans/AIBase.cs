using BladeAndTitan.Titans.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AI;

namespace BladeAndTitan.Titans;

public abstract class AIBase : MonoBehaviour
{

    public enum AIState
    {
        FullAI,
        FakedMovement,
        SlowFakedMovement
    }
    
    

    public TitanGeneric titan;
    public AIState state;

    private int framesPerTick;
    int frameCount;
    
    public NavMeshAgent agent;
    public Animator anim;
    
    protected float timeSinceLastTick;

    
    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        titan = GetComponent<TitanGeneric>();

        agent.enabled = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
        agent.avoidancePriority = 30;
    }
    public virtual void Update()
    {
        timeSinceLastTick += Time.deltaTime;
        if (frameCount == framesPerTick)
        {
            var previousState = state;
            state = AIState.FullAI;
            framesPerTick = 10;
            if (Vector3.Distance(transform.position, Player.local.transform.position) > 200)
            {
                framesPerTick = 30;
                state = AIState.FakedMovement;
            }

            if (Vector3.Distance(transform.position, Player.local.transform.position) > 500)
            {
                framesPerTick = 100;
                state = AIState.SlowFakedMovement;
            }
            
            if (previousState != state)
                AIStateChanged(previousState, state);
            
            frameCount = 0;
            switch (state)
            {
                case AIState.FullAI:
                    agent.enabled = true;
                    AITick();
                    break;
                case AIState.FakedMovement:
                    agent.enabled = false;
                    LimitedTick();
                    break;
                case AIState.SlowFakedMovement:
                    agent.enabled = false;
                    FakeTick();
                    break;
            }
            timeSinceLastTick = 0;
        }
        

        frameCount++;
    }


    public virtual void AIStateChanged(AIState prevState, AIState newState)
    {
        
    }

    protected virtual void AITick()
    {

    }
    

    protected virtual void LimitedTick()
    {
        
    }

    protected virtual void FakeTick()
    {
        
    }
    
    
    
    public void PlayAudio(string audioName, AudioSource source)
    {
        Catalog.LoadAssetAsync<AudioContainer>(audioName, ac =>
        {
            if (source != null) source.PlayOneShot(ac.PickAudioClip());
        }, audioName);
    }


}



