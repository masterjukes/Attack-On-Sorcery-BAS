using System;
using System.Collections.Generic;
using BladeAndTitan.DestructionPhysics.EzSlice;
using ThunderRoad;
using UnityEngine;
using Object = UnityEngine.Object;
using Plane = BladeAndTitan.DestructionPhysics.EzSlice.Plane;
using Random = UnityEngine.Random;

namespace BladeAndTitan.DestructionPhysics
{
    public class CollapserProcedural : Collapser
    {
        [SerializeField] public GameObject meshNode;
        [SerializeField] public GameObject collapseVfxPrefab;
        [SerializeField] public static Material sliceMaterial;
        [SerializeField] public int minShards;
        [SerializeField] public int maxShards;
        [SerializeField] Vector3 slicerNormalBias;

        [ModOption]
        [ModOptionIntValues(0, 1000, 5)]
        public static int maxDebrisObjects = 100;
        
        [ModOption( "Mesh Slices Per House", "Number of mesh slices per house, the real value will be 2^ of this modoptions value")]
        [ModOptionIntValues(0, 10, 1)]
        public static int maxSlicesPerHouse = 5;
        
        
        static List<GameObject> debrisObjects = new();
        
        struct SliceInfo
        {
            public GameObject sliceMesh;
            public Vector3 positionOffset;
            public Quaternion rotation;

        }

        private static Dictionary<string, SliceInfo[]> destructionCache = new();

        private List<GameObject> sliceParts = new();
	    bool hasCollapsed;

        void Start()
        {
            currentHp = startingHp;
        }

        public static void PrebakeMeshes()
        {
            Catalog.LoadAssetAsync<Material>(HouseDestroyer.houseMaterialName, material =>
            {
                sliceMaterial = material;
                var houseParents = GameObject.Find("New Shiganshina").transform.Find("Houses").GetComponentsInChildren<Transform>();
                foreach (var gameObject in houseParents)
                {
                    var transform = gameObject.GetChild(0);
                    var pooled = Instantiate(transform.gameObject);
                    Debug.Log(pooled.name);
                    var collapser = pooled.AddComponent < CollapserProcedural>();
                    Destroy(pooled);
                    collapser.Fragment(pooled, maxSlicesPerHouse);
                    collapser.CacheSlice(collapser.sliceParts.ToArray(), pooled);
                    foreach (var slices in collapser.sliceParts)
                    {
                        Destroy(slices);
                    }
                }
            }, "HouseMaterial");

        }
        
        static void SetShardBlack(GameObject shard)
        {
            var renderer = shard.GetComponent<Renderer>();
            if (renderer == null) return;

            var block = new MaterialPropertyBlock();

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                var material = renderer.sharedMaterials[i];
                int colorProperty = material.HasProperty("_BaseColor")
                    ? Shader.PropertyToID("_BaseColor")
                    : Shader.PropertyToID("_Color");

                if (!material.HasProperty(colorProperty)) continue;

                block.Clear();
                renderer.GetPropertyBlock(block, i);
                block.SetColor(colorProperty, Color.black);
                renderer.SetPropertyBlock(block, i);
            }
        }

        public override void Collapse(float radius, Vector3 explosionPosition, float force)
        {
            base.Collapse( radius, explosionPosition, force);
            if (hasCollapsed) return;
            
	        hasCollapsed = true;
            

            var distance = Vector3.Distance(transform.position, explosionPosition);
            
            if (distance < 30f && force > 100f)
            {
                GameObject.Destroy(gameObject);
                return;
            }
            
            
            bool scorched = distance < 75f && force > 100f;

            
            
            if (GetCachedSliceCount(meshNode) != (int) Mathf.Pow(2, maxSlicesPerHouse)  || !InstatiateCachedSlices(meshNode))
            {
                Fragment(meshNode, maxSlicesPerHouse);
                CacheSlice(sliceParts.ToArray(), meshNode);
            }
            
            MakeRoomForNewDebris();

            foreach (var slices in sliceParts)
            {
                if (scorched)
                    SetShardBlack(slices);
                
                debrisObjects.Add(slices);
                var random = Random.Range(30, 120);  
                Destroy(slices, random);
                var col = slices.AddComponent<MeshCollider>();
                col.sharedMesh = slices.GetComponent<MeshFilter>().sharedMesh;
                col.convex = true;
                Destroy(col, random-3);
                
                var rb = slices.GetOrAddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.AddExplosionForce(force, explosionPosition, radius, 0f, ForceMode.Impulse);
            }
            
            sliceParts.Clear();
            Destroy(gameObject);
            Instantiate(collapseVfxPrefab, transform.position, Quaternion.Euler(-90, 0, 0));
            
        }
        
        void MakeRoomForNewDebris()
        {
            ClearDebrisObjectsOfNull();

            int limit = Mathf.Max(0, maxDebrisObjects);
            int excess = Mathf.Max(0, debrisObjects.Count + sliceParts.Count - limit);
            int removeExisting = Mathf.Min(excess, debrisObjects.Count);

            for (int i = 0; i < removeExisting; i++)
            {
                Destroy(debrisObjects[i]);
            }

            debrisObjects.RemoveRange(0, removeExisting);
            excess -= removeExisting;

            // A single collapse can itself exceed the cap.
            for (int i = 0; i < excess; i++)
            {
                Destroy(sliceParts[i]);
            }

            if (excess > 0)
            {
                sliceParts.RemoveRange(0, excess);
            }
        }

        void ClearDebrisObjectsOfNull()
        {
            debrisObjects.RemoveAll(x => x == null);
        }

        public void CacheSlice(GameObject[] slicedMeshes, GameObject originalMesh)
        {
            List<SliceInfo> sliceInfoCache = new();
            foreach (var sliceMesh in slicedMeshes)
            {
                var cachedSlice = Instantiate(sliceMesh);
                cachedSlice.SetActive(false);

                sliceInfoCache.Add(new SliceInfo
                {
                    sliceMesh = cachedSlice,
                    positionOffset = sliceMesh.transform.position - originalMesh.transform.position,
                    rotation = Quaternion.Inverse(originalMesh.transform.rotation) *
                               sliceMesh.transform.rotation
                });
                ;
            }
            
            
            string key = originalMesh.GetComponent<MeshFilter>().sharedMesh.name;


            if (destructionCache.TryGetValue(key, out var oldSlices))
            {
                foreach (var oldSlice in oldSlices)
                {
                    if (oldSlice.sliceMesh != null)
                        Destroy(oldSlice.sliceMesh);
                }
            }

            destructionCache[key] = sliceInfoCache.ToArray();
        }

        bool InstatiateCachedSlices(GameObject replacementObject)
        {
            
            Mesh mesh = replacementObject.GetComponent<MeshFilter>().sharedMesh;
            string key = mesh.name + " Instance";


            
            if (!destructionCache.TryGetValue(key, out SliceInfo[] sliceInfo))
            {

                return false;
            }


            
            foreach (var info in sliceInfo)
            {
                if (info.sliceMesh == null)
                    return false;
                
                
                GameObject newObject = Instantiate(info.sliceMesh);

                newObject.transform.position =
                    replacementObject.transform.position +
                    replacementObject.transform.rotation * info.positionOffset;

                newObject.transform.rotation =
                    replacementObject.transform.rotation * info.rotation;

                newObject.SetActive(true);
                sliceParts.Add(newObject);
                
            }

            return true;
        }

        int GetCachedSliceCount(GameObject lookupObject)
        {
            Mesh mesh = lookupObject.GetComponent<MeshFilter>().sharedMesh;
            string key = mesh.name + " Instance";
            if (destructionCache.TryGetValue(key, out SliceInfo[] sliceInfo))
            {
                return sliceInfo.Length;
            }
            return 0;
        }
        
        

        bool Fragment(GameObject obj, int iterations)
        {
            if (iterations > 0)
            {
                GameObject[] slices = obj.SliceInstantiate(
                    GetRandomPlane(obj.GetComponent<MeshFilter>().mesh.bounds.center, transform.localScale),
                    new TextureRegion(0.0f, 0.0f, 1.0f, 1.0f),
                    sliceMaterial);

                if (slices != null)
                {
                    for (int i = 0; i < slices.Length; i++)
                    {
                        var candidate = slices[i];
                        if (Fragment(candidate, iterations - 1))
                            GameObject.DestroyImmediate(slices[i]);
                        else
                        {
                            sliceParts.Add(candidate);
                        }
                    }
                    return true;
                }
                return Fragment(obj, iterations - 1);
            }

            return false;
        }
        


        public Plane GetRandomPlane(Vector3 positionOffset, Vector3 scale)
        {
            Vector3 randomOffset = Random.insideUnitSphere;
            randomOffset.x *= scale.x;
            randomOffset.y *= scale.y;
            randomOffset.z *= scale.z;

            var position = randomOffset + positionOffset;
            Vector3 randomDirection = Random.insideUnitSphere + slicerNormalBias;

            return new Plane(position, randomDirection);
        }
    }
}