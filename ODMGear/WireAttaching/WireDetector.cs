using System;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.ODMGear.WireAttaching;

public class WireDetector : MonoBehaviour
{
    public string[] expectedWireNames;
    public bool isAttached = false;
    
    
    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponentInParent<Rope>())
        {
            var rope = other.GetComponentInParent<Rope>();
            bool isCorrectRope = false;
            foreach (var expectedWireName in expectedWireNames)
            {
                if (rope.transform.name == expectedWireName)
                {
                    isCorrectRope = true;
                    break;
                }

            }
            
            if (!isCorrectRope) return;
            
            
            var isHolding = rope.GetComponent<Item>().handlers.Count > 0;
            if (!isHolding)
            {
                if(rope.isAttached || isAttached)
                    return;
                
                other.isTrigger = true;
                Debug.Log($"Attached {rope.transform.name} to {transform.name}");
                rope.transform.parent = transform;
                rope.GetComponent<Rigidbody>().isKinematic = true;
                isAttached = true;
                rope.isAttached = true;
                rope.endPoint = transform;
            }
            else
            {
                if (rope.isAttached && isAttached)
                {
                    other.isTrigger = false;
                    rope.GetComponent<Rigidbody>().isKinematic = false;
                    rope.transform.parent = null;
                    isAttached = false;
                    rope.isAttached = false;
                    rope.endPoint = rope.transform;
                }
            }
                
        }
    }
}