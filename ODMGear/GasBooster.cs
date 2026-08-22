using System;
using BladeAndTitan.DebugHelpers;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.ODMGear;


public class GasBoosterModule : ItemModule
{
    public override void OnItemLoaded(Item item)
    {
        base.OnItemLoaded(item);
        var booster = item.GetOrAddComponent<GasBooster>();
        
        Catalog.GetData<ItemData>("ODMWireObject").SpawnAsync(swr =>
        {


            
            booster.SwordWireR = swr;
            swr.name = "SwordWireR";
            swr.transform.SetParent(item.transform);
            swr.transform.localPosition = Vector3.zero;
            swr.transform.localRotation = Quaternion.identity;
            swr.transform.localScale = Vector3.one;
            
            var rope = swr.gameObject.AddComponent<WireAttaching.Rope>();
            rope.ropeLength = 1.4f;
            rope.startPoint = item.transform.FindChildRecursive("SwordWirePointR");

            var joint = rope.startPoint.gameObject.AddComponent<SpringJoint>();
            rope.startPoint.GetComponent<Rigidbody>().isKinematic = true;
            joint.connectedBody = swr.GetComponent<Rigidbody>();
            joint.maxDistance = 1.5f;
            
            rope.endPoint = swr.transform;
            
        });
        Catalog.GetData<ItemData>("ODMWireObject").SpawnAsync(swr =>
        {
            booster.SwordWireL = swr;
            swr.name = "SwordWireL";
            swr.transform.SetParent(item.transform);
            swr.transform.localPosition = Vector3.zero;
            swr.transform.localRotation = Quaternion.identity;
            swr.transform.localScale = Vector3.one;
            
            var rope = swr.gameObject.AddComponent<WireAttaching.Rope>();
            rope.ropeLength = 1.4f;
            rope.startPoint = item.transform.FindChildRecursive("SwordWirePointL");
            rope.endPoint = swr.transform;
            
            var joint = rope.startPoint.gameObject.AddComponent<SpringJoint>();
            rope.startPoint.GetComponent<Rigidbody>().isKinematic = true;
            joint.connectedBody = swr.GetComponent<Rigidbody>();
            joint.maxDistance = 1.5f;
            
        });
        Catalog.GetData<ItemData>("ODMWireObject").SpawnAsync(swr =>
        {
            booster.GasWireR = swr;
            swr.name = "GasWireR";
            swr.transform.SetParent(item.transform);
            swr.transform.localPosition = Vector3.zero;
            swr.transform.localRotation = Quaternion.identity;
            swr.transform.localScale = Vector3.one;
            
            var rope = swr.gameObject.AddComponent<WireAttaching.Rope>();
            rope.ropeLength = 1.4f;
            rope.startPoint = item.transform.FindChildRecursive("WirePointR");
            rope.endPoint = swr.transform;
            
            var joint = rope.startPoint.gameObject.AddComponent<SpringJoint>();
            rope.startPoint.GetComponent<Rigidbody>().isKinematic = true;
            joint.connectedBody = swr.GetComponent<Rigidbody>();
            joint.maxDistance = 1.5f;
            
        });
        Catalog.GetData<ItemData>("ODMWireObject").SpawnAsync(swr =>
        {
            booster.GasWireL = swr;
            swr.name = "GasWireL";
            swr.transform.SetParent(item.transform);
            swr.transform.localPosition = Vector3.zero;
            swr.transform.localRotation = Quaternion.identity;
            swr.transform.localScale = Vector3.one;
            
            var rope = swr.gameObject.AddComponent<WireAttaching.Rope>();
            rope.ropeLength = 1.4f;
            rope.startPoint = item.transform.FindChildRecursive("WirePointL");
            rope.endPoint = swr.transform;
            
            var joint = rope.startPoint.gameObject.AddComponent<SpringJoint>();
            rope.startPoint.GetComponent<Rigidbody>().isKinematic = true;
            joint.connectedBody = swr.GetComponent<Rigidbody>();
            joint.maxDistance = 1.5f;
            
        });
        
        
        

    }
}
public class GasBooster : MonoBehaviour
{
    public Item SwordWireR;
    public Item SwordWireL;
    public Item GasWireR;
    public Item GasWireL;

    private Item self;

    private void Start()
    {
        self = GetComponent<Item>();
        self.OnDespawnEvent += SelfOnOnDespawnEvent;
        
    }

    GasCanisterTracker GetCanister(Side side)
    {
        return side == Side.Left ? GasWireL.GetComponentInParent<GasCanisterTracker>() : GasWireR.GetComponentInParent<GasCanisterTracker>();
    }


    public void ReelIn(Side handSide, Vector3 force)
    {
        var canister = GetCanister(handSide);
        if(canister == null) return;
        
        if(!canister.UseGas(0.7f * Time.fixedDeltaTime)) return;

        
        Rigidbody rb = Player.local.locomotion.physicBody.rigidBody;
        rb.AddForce(force, ForceMode.VelocityChange);
        canister.UseGas(0.7f * Time.fixedDeltaTime);
    }

    public bool UseGas(Side handSide, Vector3 force)
    {
        var canister = GetCanister(handSide);
        if(canister == null) return false;
        
        if(!canister.UseGas(1.3f * Time.fixedDeltaTime)) return false;
        
        Rigidbody rb = Player.local.locomotion.physicBody.rigidBody;
        rb.AddForce(force, ForceMode.VelocityChange);
        
        return true;
    }
    
    
    
    private void SelfOnOnDespawnEvent(EventTime eventTime)
    {
        if(eventTime == EventTime.OnEnd)
            return;

        SwordWireR.DisallowDespawn = false;
        SwordWireL.DisallowDespawn = false;
        GasWireR.DisallowDespawn = false;
        GasWireL.DisallowDespawn = false;
        
        SwordWireR.Despawn();
        SwordWireL.Despawn();
        GasWireR.Despawn();
        GasWireL.Despawn();
        self.OnDespawnEvent -= SelfOnOnDespawnEvent;
    }
}