using System;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BladeAndTitan.Titans.Generic;

public class TitanPickUpTrigger : MonoBehaviour
{
    GenericTitanAI attachedTitanAI;
    private void Start()
    { 
        attachedTitanAI = GetComponentInParent<GenericTitanAI>();   
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponentInParent<Creature>())
        {
            var random = Random.Range(0, 30000);
            if (random == 777)
            {
                attachedTitanAI.StartCoroutine(attachedTitanAI.PickUpRoutine());
            }
            else if (random == 1234)
            {
                attachedTitanAI.StartCoroutine(attachedTitanAI.JumpRoutine());
            }
            

        }
    }
}