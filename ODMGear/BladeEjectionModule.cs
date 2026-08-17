using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BladeAndTitan.Titans.Generic;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BladeAndTitan.ODMGear;

public class BladeEjectionBehaviour : MonoBehaviour
{
    private int remainingBlades = 6;
    Item item;
    Transform bladeCollider;
    Transform bladeParent;
    public bool wasPreviouslyHolstered = false;
    
    public Stack<DamageType> lastDamageTypes = new Stack<DamageType> ();
    
    Dictionary<int, string> bladeMap = new Dictionary<int, string>() {{6, "First"}, {5, "Second"}, {4, "Third"}, {3, "Fourth"}, {2, "Fith"}, {1, "Sixth"}};
    Dictionary<int, float> sizeMap = new Dictionary<int, float>() {{6, 0.1035f}, {5, 0.1035f}, {4, 0.1035f}, {3, 0.1035f}, {2, 0.1035f}, {1, 0.1135f}};

    private void Start()
    {
        item = GetComponent<Item>();
        item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
        bladeParent = item.GetCustomReference("Joints");
        bladeCollider = item.GetCustomReference("BladeCollider");
        bladeCollider.GetComponent<BoxCollider>();
        item.mainCollisionHandler.OnCollisionStartEvent += MainCollisionHandlerOnOnCollisionStartEvent;

    }
    

    private void MainCollisionHandlerOnOnCollisionStartEvent(CollisionInstance collisionInstance)
    {
        lastDamageTypes.Push(collisionInstance.damageStruct.damageType);

        if (collisionInstance.impactVelocity.magnitude > 5f)
        {
            ReleaseBlade();
        }
        
        if (collisionInstance.targetCollider.GetComponentInParent<TitanGeneric>() != null)
        {
            var titan = collisionInstance.targetCollider.GetComponentInParent<TitanGeneric>();
            titan.Damage(collisionInstance);
        }
    }


    

    

    private void Update()
    {

        if (item.holder != null && remainingBlades != 6)
        {
            remainingBlades = 6;
            Debug.Log("Holstered");
            var holder = item.holder;
            Catalog.GetData<ItemData>(item.data.id).SpawnAsync(i =>
            {
                holder.UnSnapAll();
                holder.SnapItemSilent(i);
                item.Despawn();
                Catalog.LoadAssetAsync<AudioContainer>("BladeAttachODM", q =>
                {
                    q.PlayClipAtPoint(holder.transform.position, 1.0f, AudioMixerName.Effect);
                }, "BladeAttachODM");
                
            });
            
        }
            
    }
    
    private void ItemOnOnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
    {
        if (action == Interactable.Action.AlternateUseStart)
        {
            for (int i = 0; i < 8; i++)
                ReleaseBlade();
        }
    }

    public void ReleaseBlade()
    {
        if (remainingBlades > 0)
        {
            

            
            var name = bladeMap[remainingBlades];
            var blade = bladeParent.FindChildRecursiveTR(name);
            
            Catalog.LoadAssetAsync<AudioContainer>("BladeDetachODM", q =>
            {
                q.PlayClipAtPoint(blade.position, 1.0f, AudioMixerName.Effect);
            }, "BladeDetachODM");
            
            blade.SetParent(null);
            blade.GetOrAddComponent<Rigidbody>().AddForce(blade.up * 2f, ForceMode.Impulse);
            blade.GetComponent<MeshCollider>().enabled = true;
                
            var col = bladeCollider.GetComponent<BoxCollider>();

            float amountToRemove = sizeMap[remainingBlades];

            float oldSize = col.size.y;
            float newSize = oldSize - amountToRemove;

            float delta = oldSize - newSize;

            col.size = new Vector3(col.size.x, newSize, col.size.z);
            col.center -= new Vector3(0, delta / 2f, 0);
                
            remainingBlades--;
            
        }
    }
    
    void DrawBoxCollider(BoxCollider col, LineRenderer lr)
    {
        lr.positionCount = 16; // 12 edges, but we reuse points for clean loops

        Transform t = col.transform;

        // Get world center
        Vector3 center = t.TransformPoint(col.center);
        Vector3 size = Vector3.Scale(col.size, t.lossyScale) * 0.5f;

        // Get local axes
        Vector3 right = t.right * size.x;
        Vector3 up = t.up * size.y;
        Vector3 forward = t.forward * size.z;

        // 8 corners
        Vector3[] corners = new Vector3[8];

        corners[0] = center + right + up + forward;
        corners[1] = center + right + up - forward;
        corners[2] = center + right - up + forward;
        corners[3] = center + right - up - forward;

        corners[4] = center - right + up + forward;
        corners[5] = center - right + up - forward;
        corners[6] = center - right - up + forward;
        corners[7] = center - right - up - forward;

        // Draw edges (line strip style)
        Vector3[] lines = new Vector3[]
        {
            // Top square
            corners[0], corners[1], corners[5], corners[4], corners[0],

            // Bottom square
            corners[2], corners[3], corners[7], corners[6], corners[2],

            // Vertical lines
            corners[0], corners[2],
            corners[1], corners[3],
            corners[5], corners[7],
            corners[4], corners[6]
        };

        lr.positionCount = lines.Length;
        lr.SetPositions(lines);
    }
}

