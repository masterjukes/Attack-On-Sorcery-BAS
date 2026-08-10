using System;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.Abstract;

public class TitanVelocityRemover : MonoBehaviour
{
    Rigidbody[] rigidbodies;
    private void Awake()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
    }
    private void FixedUpdate()
    {
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.velocity = Vector3.zero;
        }
    }
}