using System;
using System.Collections;
using System.Linq;
using System.Net.Mime;
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
            item.GetOrAddComponent<OdmGear>();
            item.GetOrAddComponent<BladeEjectionBehaviour>();
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
            Player.currentCreature.locomotion.SetAllSpeedModifiers("hamagane", 20f);


        }

        private void ItemOnOnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                Vector3 direction = Quaternion.AngleAxis(20f, ragdollHand.transform.forward) * ragdollHand.transform.right * -1;
                Vector3 position = ragdollHand.transform.position;


                if ( (!isHooked) && Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
                {
                    isHooked = true;
                    hookPoint.transform.parent = hit.collider.gameObject.transform;
                    hookPoint.transform.position = hit.point;
                    grapple.Grapple(ragdollHand.transform, hookPoint.transform);
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

            var button = ((InputXR_Oculus)PlayerControl.input).GetController(item.mainHandler.side).thumbstickClick;
            
            if (button.GetDown())
            {
                gasButtonPressed = true;
                if(audioSource.isPlaying == false)
                    audioSource.Play();
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

            if (item.mainHandler == null)
            {
                if (grapple.Grappling)
                    grapple.UnGrapple();
                
                Player.local.locomotion.physicBody.rigidBody.useGravity = true;
                return;
            }
            
            
            
            if (grapple.Grappling)
            {
                Vector3 position = Player.local.transform.position;
                Vector3 force = (hookPoint.transform.position - position).normalized * (Time.deltaTime * pullForce);
                Rigidbody rb = Player.local.locomotion.physicBody.rigidBody;
                rb.AddForce(force, ForceMode.VelocityChange);
                
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
                Player.local.locomotion.isGrounded = false;
                Player.local.locomotion.physicBody.rigidBody.AddForce(direction, ForceMode.VelocityChange);
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