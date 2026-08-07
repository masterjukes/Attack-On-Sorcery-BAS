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
        [ModOption( "Speed", "The speed of the ODM Gear.")]
        public static OdmSpeed speed = OdmSpeed.Normal;
        
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

        private bool isHooking;
        private bool isUnhooking;

        private LineRenderer lineRenderer;
        
        const int layerMask = 1 << 0;

        private float lastBoostTime;
        
        private GameObject hookPoint;
        private Vector3 hookPointPosition;
        
        public GameObject highlighter;

        public float reelDistance;

        public const float maxPlayerVelocity = Mathf.Infinity;
        
        public bool isBoosting;
        
        float oldAngle;


        public float odmSpeed
        {
            get
            {
                return OdmGearModule.speed switch
                {
                    OdmSpeed.ReallySlow => 0.25f,
                    OdmSpeed.Slow => 0.5f,
                    OdmSpeed.Normal => 1f,
                    OdmSpeed.Fast => 2f,
                    OdmSpeed.ReallyFast => 4f,
                    _ => 1f,
                };
            }
        }

        private void Start()
        {
            item = GetComponent<Item>();
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


                if ((!isHooked || isUnhooking) && Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
                {
                    hookPoint.transform.position = hit.point;
                    hookPointPosition = hit.point;
                    hookPoint.transform.SetParent(hit.collider.gameObject.transform);
                    
                    item.StartCoroutine(SpawnHook(position, hookPointPosition));
                }
                
            }

            if (action == Interactable.Action.UseStop)
            {
                if (isHooked || isHooking)
                {
                    item.StartCoroutine(RemoveHook(item.mainHandler.transform.position));
                }
            }
            
        }


        IEnumerator SpawnHook(Vector3 position, Vector3 hitPoint)
        {
            isHooking = true;
            
            if(isUnhooking)
                yield return new WaitUntil(() => !isUnhooking);
            
            Debug.Log("Checkpoint");
            
            lineRenderer = item.gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            if (lineRenderer.material == null)
                lineRenderer.material = new Material(Shader.Find("Standard"));

            lineRenderer.startColor = Color.grey;
            lineRenderer.endColor = Color.grey;
            lineRenderer.SetPosition(0, position);

            Debug.Log("Checkpoint2");
            
            Catalog.LoadAssetAsync<AudioContainer>("HookAttachODM", q =>
            {
                q.PlayClipAtPoint(item.transform.position, 1.0f, AudioMixerName.Effect);
            }, "HookAttachODM");
            
            var nextPosition = position;
            while (Vector3.Distance(nextPosition, hitPoint) > 0.01f)
            {
                nextPosition = Vector3.MoveTowards(nextPosition, hitPoint, 200f * Time.deltaTime);
                lineRenderer.SetPosition(1, nextPosition);
                yield return null;
            }
            Debug.Log("Checkpoint3");
            

            Rigidbody rb = item.physicBody.rigidBody;

            float dist = Vector3.Distance(rb.position, hookPointPosition);
            
            Debug.Log("Checkpoint4");

            isHooked = true;
            isHooking = false;
            
            Player.local.locomotion.physicBody.useGravity = false;
            Player.local.locomotion.physicBody.useGravity = true;

            Player.currentCreature.AddForce((hookPointPosition - Player.currentCreature.transform.position) * (dist / 6), ForceMode.Acceleration);
            
            Debug.Log("Checkpoint5");
            
        }
        
        IEnumerator RemoveHook(Vector3 position)
        {
            isUnhooking = true;

            if (isHooking)
                yield return new WaitUntil(() => !isHooking);
            
            if (lineRenderer == null) yield break;

            var nextPosition = hookPointPosition;
            
            Catalog.LoadAssetAsync<AudioContainer>("HookRetractODM", q =>
            {
                q.PlayClipAtPoint(item.transform.position, 1.0f, AudioMixerName.Effect);
            }, "HookRetractODM");
            
            while (Vector3.Distance(nextPosition, position) > 0.01f)
            {
                nextPosition = Vector3.MoveTowards(nextPosition, position, 200f * Time.deltaTime);
                lineRenderer.SetPosition(1, nextPosition);
                yield return null;
            }


            Destroy(lineRenderer);
            isHooked = false;
            isUnhooking = false;
        }

        private void Update()
        {
            
            if(item.mainHandler == null)
                return;
            
            hookPointPosition = hookPoint.transform.position;
            
            Vector3 dir = (hookPointPosition - Player.currentCreature.transform.position).normalized;
            float dist = Vector3.Distance(Player.currentCreature.transform.position, hookPointPosition);

            Vector3 currentVel = Player.local.locomotion.velocity;
            float dot = Vector3.Dot(currentVel, dir);
            Vector3 opposingVel = dot < 0 ? dir * dot : Vector3.zero;

            float pullStrength =  4f;
            Vector3 pullForce = dir * pullStrength * odmSpeed;

            Vector3 counterForce = -opposingVel;
            
            if (PlayerControl.loader == PlayerControl.Loader.Oculus)
            {
                var stick = ((InputXR_Oculus)PlayerControl.input).GetController(item.mainHandler.side)
                    .thumbstickClick;
                if (stick.GetDown())
                {
                    isBoosting = true;
                }
                if (stick.GetUp())
                {
                    isBoosting = false;
                }
                
            }

            

            RefreshLineRenderer();

            if (isBoosting)
            {
                if (!isHooked && !Player.local.locomotion.isGrounded)
                    Player.currentCreature.AddForce(Player.local.head.cam.transform.forward * 40f, ForceMode.Acceleration);
                if (isHooked)
                {
                    pullForce *= 2;

                }
            }



            if (isHooked)
            {
                if (!Mathf.Approximately(Player.local.locomotion.groundAngle, -359f))
                {
                    oldAngle = Player.local.locomotion.groundAngle;
                }
                
                Player.local.locomotion.groundAngle = -359f;
                Player.local.locomotion.physicBody.useGravity = false;
                Player.currentCreature.AddForce((pullForce + counterForce) * 2, ForceMode.Acceleration);;
                
            }
            else
            {
                Player.local.locomotion.groundAngle = oldAngle;
                Player.local.locomotion.physicBody.useGravity = true;

            }
            
            
            Player.currentCreature.locomotion.velocity = Vector3.ClampMagnitude(Player.currentCreature.locomotion.velocity, maxPlayerVelocity); 
        }


        void RefreshLineRenderer()
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


            if (lineRenderer == null) return;
            lineRenderer.SetPosition(0, position);
            lineRenderer.SetPosition(1, hookPointPosition);
        }
        
    }

    
    
    
}