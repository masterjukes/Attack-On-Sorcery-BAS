using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.SpecialisedTitans;

public class TitanFootCollider : MonoBehaviour
{
    public List<Creature> creatures = new();
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Creature>() && other.GetComponentInParent<Player>() == null)
        {
            creatures.Add(other.GetComponentInParent<Creature>());
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<Creature>())
        {
            if(creatures.Contains(other.GetComponentInParent<Creature>()))
                creatures.Remove(other.GetComponentInParent<Creature>());
        }
    }
}