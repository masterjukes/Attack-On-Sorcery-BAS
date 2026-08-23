using System;
using BladeAndTitan.TitanShifting.Abstract;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.Titans.Generic;

public class TitanGrabTrigger : MonoBehaviour
{
    GenericTitanAI attachedTitanAI;
    Side side;
    private void Start()
    { 
        attachedTitanAI = GetComponentInParent<GenericTitanAI>();
        side = Side.Right;
        if(transform.name.Contains("Left"))
            side = Side.Left;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Creature>())
        {
            var creature = other.GetComponentInParent<Creature>();
            
            if (creature.isPlayer && PlayerTitanBase.isTitan)
                return;
            
            if(attachedTitanAI.grabbing)
                return;
            
            attachedTitanAI.StartCoroutine(attachedTitanAI.GrabRoutine(creature, side));
            enabled = false;
        }
    }
}