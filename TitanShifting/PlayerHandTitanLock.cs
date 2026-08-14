namespace BladeAndTitan.TitanShifting;

using UnityEngine;

public class OneWayHandLock : MonoBehaviour
{
    public Rigidbody handRigidbody;
    public Transform target;

    public float springStrength = 5000f;
    public float damping = 150f;

    public float maxDistance = 0.15f;
    public float breakForce = 10000000000f;

    public bool useBreakDistance = false;
    public bool useBreakForce = true;

    private bool broken;

    public bool IsBroken => broken;

    private void Awake()
    {
        if (handRigidbody == null)
            handRigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (broken || handRigidbody == null || target == null)
            return;

        Vector3 displacement = target.position - handRigidbody.position;

        float distance = displacement.magnitude;

        // Break if the hand has been pulled too far.
        if (useBreakDistance && distance >= maxDistance)
        {
            Break();
            return;
        }

        if (distance <= 0.001f)
            return;

        // Hooke's law:
        // force = displacement * spring strength
        Vector3 springForce = displacement * springStrength;

        // Damping prevents oscillation.
        Vector3 dampingForce =
            -handRigidbody.velocity * damping;

        Vector3 force = springForce + dampingForce;

        // Break if the required force becomes too large.
        if (useBreakForce && force.magnitude >= breakForce)
        {
            Debug.Log($"Hand lock broke by force. {force.magnitude}");
            Break();
            return;
        }

        // IMPORTANT:
        // This is applied ONLY to the player's hand.
        // The Titan receives absolutely no force.
        handRigidbody.AddForce(force, ForceMode.Force);
    }

    public void Break()
    {
        if (broken)
            return;

        broken = true;

        Debug.Log("Hand lock broken.");
    }

    public void ResetLock()
    {
        broken = false;
    }
}