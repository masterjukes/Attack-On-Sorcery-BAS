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