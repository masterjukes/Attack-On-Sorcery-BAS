using System.Collections;
using System.Collections.Generic;
using BladeAndTitan.Titans.Generic;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.Abstract
{
    class GrabData
    {
        public Rigidbody target;
        public Transform driver;

        public Vector3 posOffset;
        public Quaternion rotOffset;
        public ConfigurableJoint joint;
    }

    public class TitanHand : MonoBehaviour
    {
        public int maxAllowed = 5;
        private List<Rigidbody> collidersInHandTrigger = new();
        bool lastGrabbed;
        public Side side;


        List<GrabData> grabs = new();


        public string thumbParentName;
        public string indexParentName;
        public string middleParentName;
        public string ringParentName;
        public string pinkyParentName;

        Transform thumb;
        Transform index;
        Transform middle;
        Transform ring;
        Transform pinky;


        public float controllerMass = 2.5f;
        public float positionSpring = 1100f;
        public float positionDamper = 125f;
        public float rotationSpring = 180f;
        public float rotationDamper = 18f;
        public float maxForce = 4500f;
        public float maxTorque = 700f;
        public float maxLagDistance = 1.25f;

        public Transform IkTarget => simulatedController != null
            ? simulatedController.transform
            : controllerTarget;

        private Transform controllerTarget;
        private Rigidbody simulatedController;

        private Vector3 previousTargetPosition;
        private Quaternion previousTargetRotation;
        private bool hasPreviousTargetPose;
        
        
        public float playerVelocityCompensation = 1f;

        public float maxPlayerVelocityCorrection = 25f;
        public float catchUpDistance = 0.35f;
        public float catchUpSpringMultiplier = 2.5f;
        public float catchUpDamperMultiplier = 1.8f;
        
        private Vector3 currentControllerVelocity;
        private Vector3 currentControllerAngularVelocity;

        public float requiredFlingVelocity = 20f;

        private Vector3 handVelocity;
        private Vector3 previousPosition;
        
        


        private void OnTriggerEnter(Collider other)
        {
            if (!other.isTrigger && collidersInHandTrigger.Count < maxAllowed &&
                other.GetComponentInParent<Player>() == null)
            {
                if (other.GetComponentInParent<Creature>() != null)
                {
                    if (ObjectAlreadyInHand(other, other.GetComponentInParent<Creature>()))
                        return;
                   
                    var creature = other.GetComponentInParent<Creature>();
                    
                    var torsoRb = other.GetComponentInParent<Creature>().ragdoll.GetPart(RagdollPart.Type.Torso)
                        .physicBody.rigidBody;
                    
                    if (handVelocity.magnitude > requiredFlingVelocity)
                    {
                        creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                        creature.AddForce(handVelocity * controllerMass, ForceMode.Impulse);
                    }
                    
                    if (!collidersInHandTrigger.Contains(torsoRb))
                        collidersInHandTrigger.Add(torsoRb);
                    return;
                }

                if (other.GetComponentInParent<Item>())
                {
                    if (ObjectAlreadyInHand(other, other.GetComponentInParent<Item>()))
                        return;
                    
                    var itemRb = other.GetComponentInParent<Item>().physicBody.rigidBody;
                    if (!collidersInHandTrigger.Contains(itemRb))
                        collidersInHandTrigger.Add(itemRb);
                    return;
                }
                
                if (other.GetComponentInParent<TitanGeneric>())
                {
                    if (ObjectAlreadyInHand(other, other.GetComponentInParent<TitanGeneric>()))
                        return;
                    
                    var titan = other.GetComponentInParent<TitanGeneric>();
                    var titanRb = titan.GetComponent<Rigidbody>();
                    if (handVelocity.magnitude > requiredFlingVelocity)
                    {
                        titanRb.isKinematic = false;
                        titanRb.useGravity = true;
                        titanRb.AddForce(handVelocity * controllerMass, ForceMode.Impulse);
                        titan.Kill();
                    }
                    if(!collidersInHandTrigger.Contains(titanRb))
                        collidersInHandTrigger.Add(titanRb);
                }
            }
        }

        bool ObjectAlreadyInHand<T>(Collider collider, T type) where T : Component
        {
            foreach (var rb in collidersInHandTrigger)
            {
                if(rb == null)
                    continue;
                
                if(rb.GetComponentInParent<T>() == type)
                    return true;
            }
            return false;
        }
        
        private void OnTriggerExit(Collider other)
        {
            
            
            if (!other.isTrigger &&
                other.GetComponentInParent<Player>() == null)
            {
                if (other.GetComponentInParent<Creature>() != null)
                {
                    var torsoRb = other.GetComponentInParent<Creature>().ragdoll.GetPart(RagdollPart.Type.Torso)
                        .physicBody.rigidBody;
                    collidersInHandTrigger.Remove(torsoRb);
                    return;
                }

                if (other.GetComponentInParent<Item>())
                {
                    var itemRb = other.GetComponentInParent<Item>().physicBody.rigidBody;
                    collidersInHandTrigger.Remove(itemRb);
                    return;
                }

                if (other.attachedRigidbody != null)
                {
                    collidersInHandTrigger.Remove(other.attachedRigidbody);
                }
                
                if (other.GetComponentInParent<TitanGeneric>())
                {
                    var titan = other.GetComponentInParent<TitanGeneric>();
                    var titanRb = titan.GetComponent<Rigidbody>();
                    collidersInHandTrigger.Remove(titanRb);
                }
            }
        }


        public void Init()
        {
            thumb = transform.FindChildRecursive(thumbParentName);
            if(thumb == null)
                Debug.LogError($"TitanHand {name} was given a null thumb.");
            index = transform.FindChildRecursive(indexParentName);
            if(index == null)
                Debug.LogError($"TitanHand {name} was given a null index.");
            middle = transform.FindChildRecursive(middleParentName);
            if(middle == null)
                Debug.LogError($"TitanHand {name} was given a null middle.");
            ring = transform.FindChildRecursive(ringParentName);
            if(ring == null)
                Debug.LogError($"TitanHand {name} was given a null ring.");
            pinky = transform.FindChildRecursive(pinkyParentName);
            if(pinky == null)
                Debug.LogError($"TitanHand {name} was given a null pinky.");
        }


        void Update()
        {
            Vector3 velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
            previousPosition = transform.position;
            handVelocity = velocity;
            
            CheckGrip();
            UpdateFingers();
        }


        void UpdateFingers()
        {
            Transform offset = side == Side.Left
                ? Player.local.transform.Find("Offset/LeftHand")
                : Player.local.transform.Find("Offset/RightHand");

            var indexMain = offset.FindChildRecursive("IndexProximal");
            var middleMain = offset.FindChildRecursive("MiddleProximal");
            var ringMain = offset.FindChildRecursive("RingProximal");
            var pinkyMain = offset.FindChildRecursive("LittleProximal");
            var thumbMain = offset.FindChildRecursive("ThumbProximal");

            // Helper function to swap X and Y
            Quaternion SwapXY(Quaternion q)
            {
                Vector3 e = q.eulerAngles;
                return Quaternion.Euler(-e.y, e.x, e.z);
            }

            // ===== PROXIMAL =====
            thumb.localRotation = SwapXY(thumbMain.localRotation);
            index.localRotation = SwapXY(indexMain.localRotation);
            middle.localRotation = SwapXY(middleMain.localRotation);
            ring.localRotation = SwapXY(ringMain.localRotation);
            pinky.localRotation = SwapXY(pinkyMain.localRotation);

            // ===== INTERMEDIATE =====
            thumb.GetChild(0).localRotation = SwapXY(thumbMain.GetChild(0).localRotation);
            index.GetChild(0).localRotation = SwapXY(indexMain.GetChild(0).localRotation);
            middle.GetChild(0).localRotation = SwapXY(middleMain.GetChild(0).localRotation);
            ring.GetChild(0).localRotation = SwapXY(ringMain.GetChild(0).localRotation);
            pinky.GetChild(0).localRotation = SwapXY(pinkyMain.GetChild(0).localRotation);

            // ===== DISTAL =====
            thumb.GetChild(0).GetChild(0).localRotation = SwapXY(thumbMain.GetChild(0).GetChild(0).localRotation);
            index.GetChild(0).GetChild(0).localRotation = SwapXY(indexMain.GetChild(0).GetChild(0).localRotation);
            middle.GetChild(0).GetChild(0).localRotation = SwapXY(middleMain.GetChild(0).GetChild(0).localRotation);
            ring.GetChild(0).GetChild(0).localRotation = SwapXY(ringMain.GetChild(0).GetChild(0).localRotation);
            pinky.GetChild(0).GetChild(0).localRotation = SwapXY(pinkyMain.GetChild(0).GetChild(0).localRotation);
        }


        void Grab()
        {
            foreach (var rb in collidersInHandTrigger)
            {

                if (rb == null)
                {
                    continue;
                }

                if (rb.GetComponentInParent<Creature>())
                {
                    var creature = rb.GetComponentInParent<Creature>();
                    creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    IgnoreCollisions(creature.gameObject, gameObject);
                }

                if (rb.GetComponentInParent < TitanGeneric>())
                {
                    continue;
                    
                    var titan = rb.GetComponentInParent<TitanGeneric>();
                    titan.Kill();
                    titan.GetComponent<Rigidbody>().isKinematic = false;
                    titan.GetComponent<Rigidbody>().useGravity = true;
                    
                    IgnoreCollisions(titan.gameObject, gameObject);
                }


                GameObject driver = new GameObject("GrabDriver");
                driver.transform.position = rb.position;
                driver.transform.rotation = rb.rotation;

                Rigidbody driverRb = driver.AddComponent<Rigidbody>();
                driverRb.isKinematic = true;
                driverRb.useGravity = false;

                ConfigurableJoint joint = driver.AddComponent<ConfigurableJoint>();
                joint.connectedBody = rb;

                GrabData data = new GrabData
                {
                    target = rb,
                    driver = driver.transform,
                    joint = joint,
                    posOffset = transform.InverseTransformPoint(driver.transform.position),
                    rotOffset = Quaternion.Inverse(transform.rotation) * driver.transform.rotation
                };

                grabs.Add(data);

                joint.autoConfigureConnectedAnchor = false;

                joint.anchor = Vector3.zero;
                joint.connectedAnchor = rb.transform.InverseTransformPoint(driver.transform.position);

                joint.xMotion = ConfigurableJointMotion.Limited;
                joint.yMotion = ConfigurableJointMotion.Limited;
                joint.zMotion = ConfigurableJointMotion.Limited;

                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;

                SoftJointLimitSpring spring = new SoftJointLimitSpring
                {
                    spring = 15000f,
                    damper = 1500f
                };

                joint.linearLimitSpring = spring;

                joint.massScale = 10f;
                joint.connectedMassScale = 20f;

                joint.enableCollision = false;
                
                

            }
        }

        void UnGrab()
        {
            foreach (var g in grabs)
            {
                if(g == null)
                    continue;
                    
                Rigidbody rb = g.target;
                
                
                Destroy(g.joint);
                Destroy(g.driver.gameObject);

                if (g.target.GetComponentInParent<Creature>())
                {
                    UnignoreCollisions(g.target.GetComponentInParent<Creature>().gameObject, gameObject);
                }
            }
            
            
            grabs.Clear();
        }

        public void CheckGrip()
        {
            bool gripping = Player.currentCreature
                .GetHand(side)
                .playerHand.controlHand.gripPressed;

            if (gripping && !lastGrabbed)
                Grab();

            if (!gripping && lastGrabbed)
                UnGrab();

            lastGrabbed = gripping;
        }




        // Add these methods inside TitanHand.

        public void ConfigureControllerMass(Transform target)
        {
            controllerTarget = target;

            if (controllerTarget == null)
            {
                Debug.LogError($"TitanHand {name} was given a null controller target.");
                return;
            }

            if (simulatedController != null)
            {
                Destroy(simulatedController.gameObject);
            }

            var proxy = new GameObject($"TitanHandMassProxy_{side}");

            // Deliberately do not parent this to the VR hand or titan.
            // It must remain a world-space Rigidbody for the simulated mass to work.
            proxy.transform.position = controllerTarget.position;
            proxy.transform.rotation = controllerTarget.rotation;

            simulatedController = proxy.AddComponent<Rigidbody>();
            simulatedController.mass = controllerMass;
            simulatedController.useGravity = false;
            simulatedController.drag = 0f;
            simulatedController.angularDrag = 0f;
            simulatedController.interpolation = RigidbodyInterpolation.Interpolate;
            simulatedController.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            previousTargetPosition = controllerTarget.position;
            previousTargetRotation = controllerTarget.rotation;
            hasPreviousTargetPose = true;
        }

        private void FixedUpdate()
        {
            UpdateSimulatedController();
            UpdateGrabDrivers();
        }

        private void UpdateSimulatedController()
        {
            if (controllerTarget == null || simulatedController == null)
                return;

            float titanScale = Mathf.Max(1f, Player.local.transform.lossyScale.x);

            float scaledCatchUpDistance = catchUpDistance * titanScale;
            float scaledMaxLagDistance = maxLagDistance * titanScale;
            float scaledMaxForce = maxForce * titanScale;
            float scaledMaxPlayerVelocityCorrection =
                maxPlayerVelocityCorrection * titanScale;
            
            float dt = Time.fixedDeltaTime;

            // Hand/controller movement since the previous physics frame.
            Vector3 controllerVelocity = Vector3.zero;
            Vector3 controllerAngularVelocity = Vector3.zero;

            if (hasPreviousTargetPose)
            {
                controllerVelocity =
                    (controllerTarget.position - previousTargetPosition) / dt;

                Quaternion rotationDelta =
                    controllerTarget.rotation * Quaternion.Inverse(previousTargetRotation);

                rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

                if (angle > 180f)
                    angle -= 360f;

                if (Mathf.Abs(angle) > 0.001f && axis.sqrMagnitude > 0.001f)
                {
                    controllerAngularVelocity =
                        axis.normalized * (angle * Mathf.Deg2Rad / dt);
                }
            }

            previousTargetPosition = controllerTarget.position;
            previousTargetRotation = controllerTarget.rotation;
            hasPreviousTargetPose = true;

            // This mirrors PlayerLink's locomotion velocity correction:
            // when the player runs, jumps, or falls, the titan hand inherits that motion
            // instead of having to be pulled after the player by the spring.
            Vector3 playerVelocity = Vector3.zero;

            if (Player.local?.locomotion?.physicBody != null)
            {
                playerVelocity = Player.local.locomotion.physicBody.velocity;
            }

            playerVelocity = Vector3.ClampMagnitude(
                playerVelocity,
                scaledMaxPlayerVelocityCorrection);

            Vector3 desiredVelocity =
                controllerVelocity +
                playerVelocity * playerVelocityCompensation;

            Vector3 positionError =
                controllerTarget.position - simulatedController.position;

            float errorDistance = positionError.magnitude;

            // Stronger, more damped correction once the hand is noticeably behind.
            float spring = positionSpring;
            float damper = positionDamper;

            if (errorDistance > scaledCatchUpDistance)
            {
                spring *= catchUpSpringMultiplier;
                damper *= catchUpDamperMultiplier;
            }

            Vector3 velocityError =
                desiredVelocity - simulatedController.velocity;

            Vector3 force =
                positionError * spring +
                velocityError * damper;

            simulatedController.AddForce(
                Vector3.ClampMagnitude(force, scaledMaxForce),
                ForceMode.Force);

            Quaternion desiredRotation =
                controllerTarget.rotation * Quaternion.Inverse(simulatedController.rotation);

            desiredRotation.ToAngleAxis(out float rotationAngle, out Vector3 rotationAxis);

            if (rotationAngle > 180f)
                rotationAngle -= 360f;

            if (Mathf.Abs(rotationAngle) > 0.001f && rotationAxis.sqrMagnitude > 0.001f)
            {
                Vector3 torque =
                    rotationAxis.normalized *
                    (rotationAngle * Mathf.Deg2Rad * rotationSpring) +
                    (controllerAngularVelocity - simulatedController.angularVelocity) *
                    rotationDamper;

                simulatedController.AddTorque(
                    Vector3.ClampMagnitude(torque, maxTorque),
                    ForceMode.Force);
            }

            currentControllerVelocity = controllerVelocity;
            currentControllerAngularVelocity = controllerAngularVelocity;
            
            // Teleports, respawns, and extreme desyncs should not leave hands behind.
            if (positionError.sqrMagnitude >
                scaledMaxLagDistance * scaledMaxLagDistance)
            {
                simulatedController.position = controllerTarget.position;
                simulatedController.rotation = controllerTarget.rotation;
                simulatedController.velocity = playerVelocity;
                simulatedController.angularVelocity = Vector3.zero;
            }
        }

        private void UpdateGrabDrivers()
        {
            foreach (var g in grabs)
            {
                if (g.driver == null)
                    continue;

                g.driver.position = transform.TransformPoint(g.posOffset);
                g.driver.rotation = transform.rotation * g.rotOffset;
            }
        }
        
        public static void IgnoreCollisions(GameObject a, GameObject b)
        {
            Collider[] aColliders = a.GetComponentsInChildren<Collider>(true);
            Collider[] bColliders = b.GetComponentsInChildren<Collider>(true);

            foreach (Collider aCol in aColliders)
            {
                foreach (Collider bCol in bColliders)
                {
                    Physics.IgnoreCollision(aCol, bCol, true);
                }
            }
        }
        
        public static void UnignoreCollisions(GameObject a, GameObject b)
        {
            Collider[] aColliders = a.GetComponentsInChildren<Collider>(true);
            Collider[] bColliders = b.GetComponentsInChildren<Collider>(true);

            foreach (Collider aCol in aColliders)
            {
                foreach (Collider bCol in bColliders)
                {
                    Physics.IgnoreCollision(aCol, bCol, false);
                }
            }
        }  

        private void OnDestroy()
        {
            UnGrab();
            if (simulatedController != null)
            {
                Destroy(simulatedController.gameObject);
            }
        }
    }



    
    public static class TransformExtensions
    {
        public static Transform FindChildRecursive(this Transform parent, string name)
        {
            
            
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                var result = child.FindChildRecursive(name);
                if (result != null)
                    return result;
            }
            
            return null;
        }
    }

}