using System.Collections;
using System.Collections.Generic;
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
        
        


        private void OnTriggerEnter(Collider other)
        {
            if (!other.isTrigger && collidersInHandTrigger.Count < maxAllowed &&
                other.GetComponentInParent<Player>() == null)
            {
                if (other.GetComponentInParent<Creature>() != null)
                {
                    var torsoRb = other.GetComponentInParent<Creature>().ragdoll.GetPart(RagdollPart.Type.Torso)
                        .physicBody.rigidBody;
                    if (!collidersInHandTrigger.Contains(torsoRb))
                        collidersInHandTrigger.Add(torsoRb);
                    return;
                }

                if (other.GetComponentInParent<Item>())
                {
                    var itemRb = other.GetComponentInParent<Item>().physicBody.rigidBody;
                    if (!collidersInHandTrigger.Contains(itemRb))
                        collidersInHandTrigger.Add(itemRb);
                    return;
                }

                if (other.attachedRigidbody != null)
                {
                    if (!collidersInHandTrigger.Contains(other.attachedRigidbody))
                        collidersInHandTrigger.Add(other.attachedRigidbody);
                }
            }
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
            }
        }
        
        
        public void Init()
        {
            thumb = transform.FindChildRecursive(thumbParentName);
            index = transform.FindChildRecursive(indexParentName);
            middle = transform.FindChildRecursive(middleParentName);
            ring = transform.FindChildRecursive(ringParentName);
            pinky = transform.FindChildRecursive(pinkyParentName);
            
        }



        void Update()
        {
            CheckGrip();
            UpdateFingers();
        }
        
        void FixedUpdate()
        {
            foreach (var g in grabs)
            {
                if (g.driver == null) continue;

                g.driver.position = transform.TransformPoint(g.posOffset);
                g.driver.rotation = transform.rotation * g.rotOffset;
            }
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
                return Quaternion.Euler(-e.y,e.x, e.z);
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
                if (rb.GetComponentInParent<Creature>())
                {
                    var creature = rb.GetComponentInParent<Creature>();
                    creature.ragdoll.SetState(Ragdoll.State.Destabilized);
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
                
                Player.local.locomotion.SetAllSpeedModifiers("ajfa", 10);

            }
        }

        void UnGrab()
        {
            StartCoroutine(EnableCollision(grabs.ToArray()));
            foreach (var g in grabs)
            {
                Destroy(g.joint);
                Destroy(g.driver.gameObject);
                
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
        
        IEnumerator EnableCollision(GrabData[] clogs)
        {
            yield return new WaitForSeconds(0.2f);
            
            foreach (var col in gameObject.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
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