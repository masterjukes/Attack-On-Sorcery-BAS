using UnityEngine;

namespace BladeAndTitan.DestructionPhysics;


public class SimplePhysicsObject : MonoBehaviour
{
    public Vector3 velocity;
    private float mass = 0.3f;

    public void AddExplosionForce(
        float force,
        Vector3 explosionPosition,
        float radius,
        float upwardsModifier = 0f, ForceMode mode = ForceMode.Impulse)
    {

        upwardsModifier = 20f;
        
        Vector3 direction = transform.position - explosionPosition;
        float distance = direction.magnitude;

        if (distance > radius)
            return;

        if (distance > 0.001f)
            direction /= distance;
        else
            direction = Vector3.up;

        float falloff = 1f - (distance / radius);

        Vector3 forceVector = direction * ((force / mass) * falloff);
        Vector3 random = Random.insideUnitSphere * 5f;
        forceVector += random * (force / mass);
        forceVector.y = 0f;
        
        
        
        forceVector.y += upwardsModifier * falloff;
        
        
        velocity += forceVector;
    }
    
    
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        Vector3 gravity = Physics.gravity;
        velocity += gravity * dt;

        float dragCoefficient = 1.05f;
        float airDensity = 1.225f;
        float area = 0.09f;

        Vector3 dragForce = -0.5f * dragCoefficient * airDensity * area
                            * velocity.magnitude * velocity;

        Vector3 dragAcceleration = dragForce / mass;
        velocity += dragAcceleration * dt;

        transform.position += velocity * dt;
    }
    
}