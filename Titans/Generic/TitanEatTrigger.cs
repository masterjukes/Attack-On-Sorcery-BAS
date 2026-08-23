using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.Titans.Generic;

public class TitanEatTrigger : MonoBehaviour
{
    public TitanGeneric titan;


    void Start()
    {
        titan = GetComponentInParent<TitanGeneric>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Creature>() != null && !titan.jawDisabled && !titan.eyesDisabled)
        {
            AttemptKill(other.GetComponentInParent<Creature>());
        }
    }


    public static void AttemptKill(Creature creature)
    {

        if (!creature.isPlayer)
        {
            creature.Kill();
            return;
        }

        if(!TitanShifting.Abstract.PlayerTitanBase.isTitan && Player.invincibility == false)
            Player.currentCreature.Kill();
    }
}