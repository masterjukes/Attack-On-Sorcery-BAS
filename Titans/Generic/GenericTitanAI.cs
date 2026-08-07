using ThunderRoad;
using UnityEngine;
using UnityEngine.AI;

namespace BladeAndTitan.Titans.Generic;

public class GenericTitanAI : AIBase
{
    public enum AIBehaviourMode
    {
        Roaming,
        Chasing
    }


    Animator anim;
    
    public AIBehaviourMode behaviourMode;

    public Vector3 target;
    public Vector3 roamAreaCenter;

    public int maxDestinationAttempts = 10;
    int titansChasingPlayer = 0;



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
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
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
            
            agent.speed = TitanSpawner.DeviatedRandom(8, 0.4f);
            agent.SetDestination(Player.currentCreature.transform.position);

        }


    }
    
    

    public void SwitchBehaviourMode(AIBehaviourMode mode)
    {
        if(mode == AIBehaviourMode.Chasing)
            titansChasingPlayer++;
        
        if(behaviourMode == AIBehaviourMode.Chasing && mode == AIBehaviourMode.Roaming)
            titansChasingPlayer--;
        
        behaviourMode = mode;

    }

}