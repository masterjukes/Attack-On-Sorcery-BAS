using System;
using BladeAndTitan.Titans.Generic;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.SpecialisedTitans;

public class WarhammerSpikesTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Creature>())
        {
            var creature = other.GetComponentInParent<Creature>();
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
            creature.Kill();
            creature.AddForce(Vector3.up * 100, ForceMode.Impulse);
        }
        else if (other.GetComponentInParent<TitanGeneric>())
        {
            var titan = other.GetComponentInParent<TitanGeneric>();
            var titanRb = titan.GetComponent<Rigidbody>();
            
            titanRb.isKinematic = false;
            titanRb.useGravity = true;
            titanRb.AddForce(Vector3.up * 100, ForceMode.Impulse);
            titan.Kill();
            
        }
    }
}