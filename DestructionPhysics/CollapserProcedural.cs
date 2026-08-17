using BladeAndTitan.DestructionPhysics.EzSlice;
using UnityEngine;
using Plane = BladeAndTitan.DestructionPhysics.EzSlice.Plane;

namespace BladeAndTitan.DestructionPhysics
{
    public class CollapserProcedural : Collapser
    {
        [SerializeField] public GameObject meshNode;
        [SerializeField] public GameObject collapseVfxPrefab;
        [SerializeField] public Material sliceMaterial;
        [SerializeField] public int minShards;
        [SerializeField] public int maxShards;
        [SerializeField] Vector3 slicerNormalBias;

	bool hasCollapsed;

        void Start()
        {
            currentHp = startingHp;
        }

        public override void Collapse()
        {
            base.Collapse();
            if (hasCollapsed) return;
            
	        hasCollapsed = true;
            Fragment(meshNode, Random.Range(minShards, maxShards));
            gameObject.SetActive(false);
            Instantiate(collapseVfxPrefab, transform.position, Quaternion.identity);
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
                            var body = candidate.AddComponent<Rigidbody>();
                            var col = candidate.AddComponent<MeshCollider>();

                            body.mass = 20;
                            col.sharedMesh = candidate.GetComponent<MeshFilter>().sharedMesh;
                            col.convex = true;
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