using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.SpecialisedTitans;

public class TitanFootCollider : MonoBehaviour
{
    public List<Creature> creatures = new();
    public List<GameObject> houses = new();
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Creature>() && other.GetComponentInParent<Player>() == null)
        {
            creatures.Add(other.GetComponentInParent<Creature>());
        }
        else if (other.gameObject.name.Contains("House"))
        {
            houses.Add(other.gameObject);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<Creature>())
        {
            if(creatures.Contains(other.GetComponentInParent<Creature>()))
                creatures.Remove(other.GetComponentInParent<Creature>());
        }
        else if (other.gameObject.name.Contains("House"))
        {
            if(houses.Contains(other.gameObject))
                houses.Remove(other.gameObject);
        }
    }
}