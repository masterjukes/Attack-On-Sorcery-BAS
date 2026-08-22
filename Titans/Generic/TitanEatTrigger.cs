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
        if (other.GetComponentInParent<Player>() != null && !titan.jawDisabled && !titan.eyesDisabled)
        {
            if(!TitanShifting.Abstract.PlayerTitanBase.isTitan && Player.invincibility == false)
                Player.currentCreature.Kill();
        }
    }
}