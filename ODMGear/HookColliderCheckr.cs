using System;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.ODMGear;

public class HookColliderChecker : MonoBehaviour
{
    public bool isHooked = false;
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.GetComponentInParent<Player>())
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), other.collider);
        }
        isHooked = true;
        GetComponent<Rigidbody>().isKinematic = true;
        transform.position = other.GetContact(0).point;
        transform.parent = other.collider.transform;
        GetComponent<Collider>().enabled = false;
        
    }
}