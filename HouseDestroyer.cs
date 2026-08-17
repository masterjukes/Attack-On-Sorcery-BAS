using BladeAndTitan.DestructionPhysics;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan;

public class HouseDestroyer : MonoBehaviour
{
    static Material houseMaterial;
    static GameObject destructionVfx;

    private bool isLoaded;
    
    static string houseMaterialName = "DestructionPhysicsHouseSliceMat";
    static string destructionVfxName = "DestructionPhysicsHouseVfx";
    void Start()
    {
        if (!isLoaded)
        {
            Catalog.LoadAssetAsync<Material>(houseMaterialName, LoadAssets, "HouseMaterial");
        }
        else
        {
            ApplyDestruction();
        }
    }

    private void LoadAssets(Material obj)
    {
        houseMaterial = obj;
        Catalog.LoadAssetAsync<GameObject>(destructionVfxName, o => {destructionVfx = o;
            isLoaded = true;
            ApplyDestruction();
        }, "DestructionVfx" );
    }

    void ApplyDestruction()
    {
        var collapser = gameObject.AddComponent<CollapserProcedural>();
        collapser.meshNode = gameObject;
        collapser.collapseVfxPrefab = destructionVfx;
        collapser.sliceMaterial = houseMaterial;
        collapser.minShards = 4;
        collapser.maxShards = 8;
        collapser.Collapse();
    }
    
    
}