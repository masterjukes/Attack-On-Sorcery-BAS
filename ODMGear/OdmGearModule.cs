using System;
using System.Collections;
using System.Linq;
using System.Net.Mime;
using BladeAndTitan.DebugHelpers;
using BladeAndTitan.ODMGear.WireAttaching;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace BladeAndTitan.ODMGear
{

    public  enum OdmSpeed
    {
        ReallySlow,
        Slow,
        Normal,
        Fast,
        ReallyFast,
    }
    
    public class OdmGearModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            var gear = item.GetOrAddComponent<OdmGear>();
            item.GetOrAddComponent<BladeEjectionBehaviour>();
            var detector = item.transform.FindChildRecursive("WirePoint").gameObject.AddComponent<WireDetector>();
            detector.expectedWireNames = new[] {"SwordWireR", "SwordWireL"};
            gear.wireDetector = detector;
        }
    }
    
    public class OdmGear : ThunderBehaviour
    {
        private Item item;
        bool isHooked;
        float maxDistance = 100f;
        const int layerMask = 1 << 0;
        private GameObject hookPoint;
        public GameObject highlighter;
        private GrapplingRope grapple;
        public bool gasButtonPressed;
        
        public WireDetector wireDetector;
        bool isOdmAttached => wireDetector.isAttached;
        GasBooster gasBooster => Player.currentCreature.holders.FirstOrDefault(h => h.name == "LowerBackholderODM")?.items.FirstOrDefault()?.GetComponent<GasBooster>();
        

        AudioSource audioSource;
        private void Start()
        {
            item = GetComponent<Item>();
            grapple = item.GetOrAddComponent<GrapplingRope>();
            grapple.Init();
            audioSource = GetComponent<AudioSource>();
            
            item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
            
            highlighter = new GameObject("Highlighter");
            hookPoint = new GameObject("HookPoint");
            hookPoint.name = "HookPoint";
            hookPoint.GetOrAddComponent<SphereCollider>().radius = 0.1f;
            hookPoint.GetComponent<SphereCollider>().enabled = false;
            hookPoint.GetOrAddComponent<Rigidbody>().isKinematic = true;
            hookPoint.GetOrAddComponent<HookColliderChecker>();
            hookPoint.GetComponent<Rigidbody>().drag = 0.1f;
            hookPoint.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Player.currentCreature.locomotion.SetAllSpeedModifiers("hamagane", 20f);


        }

        private void ItemOnOnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                Vector3 direction = Quaternion.AngleAxis(20f, ragdollHand.transform.forward) * ragdollHand.transform.right * -1;
                Vector3 position = ragdollHand.transform.position;

                if(!isOdmAttached)
                    return;

                if ( (!isHooked))
                {
                    isHooked = true;
                    hookPoint.transform.position = position;
                    hookPoint.transform.position += direction.normalized * 0.4f;
                    
                    hookPoint.GetComponent<Collider>().enabled = true;
                    Rigidbody rb = hookPoint.GetComponent<Rigidbody>();
                    float targetDistance = maxDistance;
                    float drag = rb.drag;
                    float requiredVelocity = targetDistance * drag;

                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.AddForce(direction.normalized * requiredVelocity * 10, ForceMode.VelocityChange);
                    
                    Transform grappleOrigin = Player.currentCreature.holders.FirstOrDefault(h => h.name == $"Hips{ragdollHand.side}")!.transform;
                    
                    grapple.Grapple(grappleOrigin, hookPoint.transform);
                    PlaySound("HookAttachODM");
                    
                }
                
            }

            if (action == Interactable.Action.UseStop)
            {
                if (isHooked)
                { 
                    isHooked = false;
                    hookPoint.transform.parent = null;
                    
                    grapple.UnGrapple();
                    PlaySound("HookRetractODM");
                }
            }
            
            
        }

        private void Update()
        {
            if (PlayerControl.loader != PlayerControl.Loader.Oculus) return;
            if(item?.mainHandler == null) return;
            
            var button = ((InputXR_Oculus)PlayerControl.input).GetController(item.mainHandler.side).thumbstickClick;
            
            if (button.GetDown())
            {
                gasButtonPressed = true;
            }
            if (button.GetUp())
            {
                gasButtonPressed = false;
                if(audioSource.isPlaying)
                    audioSource.Stop();
            }

            
        }
        

        private void FixedUpdate()
        {
            const float pullForce = 20f;
            const float gasDirectionSpeed = 0.1f;

            if (item?.mainHandler == null || !isOdmAttached)
            {
                if (grapple.Grappling)
                    grapple.UnGrapple();
                
                Player.local.locomotion.physicBody.rigidBody.useGravity = true;
                return;
            }
            
            
            
            if (grapple.Grappling && hookPoint.GetComponent<HookColliderChecker>().isHooked)
            {
                Vector3 position = Player.local.transform.position;
                Vector3 force = (hookPoint.transform.position - position).normalized * (Time.deltaTime * pullForce);
                
                gasBooster.ReelIn(item.mainHandler.side, force);
                
                if (Player.local.locomotion.physicBody.rigidBody.useGravity && !gasButtonPressed)
                {
                    Player.local.locomotion.physicBody.rigidBody.useGravity = false;
                }

            }

            else if (!Player.local.locomotion.physicBody.rigidBody.useGravity)
            {
                Player.local.locomotion.physicBody.rigidBody.useGravity = true;
            }
            
            if (gasButtonPressed)
            {
                Vector3 direction = Quaternion.AngleAxis(20f, item.mainHandler.transform.forward) * item.mainHandler.transform.right * -1;
                direction *= gasDirectionSpeed;
                if(!gasBooster.UseGas(item.mainHandler.side, direction))
                    return;
                
                if(audioSource.isPlaying == false)
                    audioSource.Play();

                
            }
            
            UpdateCrosshair();
            
            
        }
        
        private void PlaySound(string soundId)
        {
            Catalog.LoadAssetAsync<AudioContainer>(soundId,
                sound => sound.PlayClipAtPoint(item.transform.position, 1f, AudioMixerName.Effect), soundId);
        }
        
        void UpdateCrosshair()
        {
            Vector3 direction = Quaternion.AngleAxis(20f, item.mainHandler.transform.forward) * item.mainHandler.transform.right * -1;
            Vector3 position = item.mainHandler.transform.position;
            if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                highlighter.transform.position = hit.point;
                highlighter.transform.rotation = Quaternion.LookRotation(hit.normal);
                Highlighter.GetSide(item.mainHandler.side).Show(highlighter.transform, null, null, Highlighter.Style.TK, 3f);
                Highlighter.GetSide(item.mainHandler.side).SetOutlineColor(Color.white);
            }
            else
            {
                highlighter.transform.position = position + (direction * maxDistance);
                Highlighter.GetSide(item.mainHandler.side).Show(highlighter.transform, null, null, Highlighter.Style.TK, 3f);
                Highlighter.GetSide(item.mainHandler.side).SetOutlineColor(Color.white);
            }

        }
        
    }

    
    
    
}