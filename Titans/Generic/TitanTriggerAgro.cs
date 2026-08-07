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
        
        var playerColliders = Player.local.GetComponentsInChildren<Collider>();
        if (!playerColliders.Contains(other))
            return;

        attachedTitanAI.SwitchBehaviourMode(GenericTitanAI.AIBehaviourMode.Chasing);

    }
}