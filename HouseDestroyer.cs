using System.Collections.Generic;
using BladeAndTitan.DestructionPhysics;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan;

public class HouseDestroyer : MonoBehaviour
{
    
    struct DestructionRequest
    {
        public HouseDestroyer house;
        public float radius;
        public Vector3 position;
        public float force;
    }

    static List<DestructionRequest> pendingDestructions = new();
    static bool isLoading;
    static bool isLoaded;
    
    static Material houseMaterial;
    static GameObject destructionVfx;
    
    public static string houseMaterialName = "DestructionPhysicsHouseSliceMat";
    static string destructionVfxName = "DestructionPhysicsHouseVfx";
    public void Init(float radius, Vector3 explosionPosition, float force)
    {
        if (isLoaded)
        {
            ApplyDestruction(radius, explosionPosition, force);
            return;
        }

        pendingDestructions.Add(new DestructionRequest
        {
            house = this,
            radius = radius,
            position = explosionPosition,
            force = force
        });

        if (isLoading)
            return;

        isLoading = true;

        Catalog.LoadAssetAsync<Material>(houseMaterialName, material =>
        {
            houseMaterial = material;

            Catalog.LoadAssetAsync<GameObject>(destructionVfxName, vfx =>
            {
                destructionVfx = vfx;
                isLoaded = true;
                isLoading = false;

                foreach (var request in pendingDestructions)
                {
                    request.house.ApplyDestruction(
                        request.radius,
                        request.position,
                        request.force
                    );
                }

                pendingDestructions.Clear();

            }, "DestructionVfx");

        }, "HouseMaterial");
    }
    

    void ApplyDestruction(float radius, Vector3 explosionPosition, float force)
    {
        var collapser = gameObject.AddComponent<CollapserProcedural>();
        collapser.meshNode = gameObject;
        collapser.collapseVfxPrefab = destructionVfx;
        CollapserProcedural.sliceMaterial = houseMaterial;
        
        var distance = Vector3.Distance(explosionPosition, transform.position);
        
        collapser.minShards = 3;
        collapser.maxShards = 6;
        collapser.Collapse(force, explosionPosition, radius);
    }
    
    
}