using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.Titans.Generic;

public class TitanTriggerAgro : MonoBehaviour
{
    private GenericTitanAI attachedTitanAI;
    private void Start()
    {
        attachedTitanAI = GetComponentInParent<GenericTitanAI>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if(attachedTitanAI.behaviourMode != GenericTitanAI.AIBehaviourMode.Roaming)
            return;
        
        if(other.GetComponentInParent<Creature>())
            attachedTitanAI.SwitchBehaviourMode(GenericTitanAI.AIBehaviourMode.Chasing);

    }
}