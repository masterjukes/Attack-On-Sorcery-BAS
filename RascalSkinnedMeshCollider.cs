using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BladeAndTitan
{
    public class RASCALSkinnedMeshCollider : MonoBehaviour
    {
        public class PhysicsMaterialAssociation
        {
            public Material material;

            public PhysicMaterial physicsMaterial;
        }

        public delegate void RASCALTimedEvent(double time);
        
        public class Skinfo
        {
            public List<BoneInfo> bones;

            public Mesh bakedMesh;

            public SkinnedMeshRenderer skinnedMesh;

            public RASCALSkinnedMeshCollider host;

            public RASCALPhysMaterialProperties materialProperties;

            public Matrix4x4 meshRootMatrix;

            public bool noBones;

            internal Skinfo Init()
            {
                bakedMesh = new Mesh();
                materialProperties = skinnedMesh.GetComponent<RASCALPhysMaterialProperties>();
                if (skinnedMesh.bones.Length == 0)
                {
                    noBones = true;
                    var boneInfo = new BoneInfo
                    {
                        srcSkin = skinnedMesh,
                        transform = skinnedMesh.transform,
                        host = host,
                        parentSkinfo = this
                    }.Init();
                    for (var i = 0; i < skinnedMesh.sharedMesh.vertexCount; i++) boneInfo.affectedVerts.Add(i);
                    bones = new List<BoneInfo> { boneInfo };
                }
                else
                {
                    var list = skinnedMesh.bones.Select(x => new BoneInfo
                    {
                        srcSkin = skinnedMesh,
                        transform = x,
                        host = host,
                        parentSkinfo = this
                    }.Init()).ToList();
                    var boneWeights = skinnedMesh.sharedMesh.boneWeights;
                    for (var num = 0; num < boneWeights.Length; num++)
                    {
                        var boneWeight = boneWeights[num];
                        var weight = new Weight[4]
                        {
                            new Weight
                            {
                                weight = boneWeight.weight0,
                                boneIndex = boneWeight.boneIndex0
                            },
                            new Weight
                            {
                                weight = boneWeight.weight1,
                                boneIndex = boneWeight.boneIndex1
                            },
                            new Weight
                            {
                                weight = boneWeight.weight2,
                                boneIndex = boneWeight.boneIndex2
                            },
                            new Weight
                            {
                                weight = boneWeight.weight3,
                                boneIndex = boneWeight.boneIndex3
                            }
                        }.OrderBy(x => x.weight).Last();
                        var boneInfo2 = list[weight.boneIndex];
                        if ((bool)boneInfo2.materialProperties)
                        {
                            if (weight.weight > boneInfo2.materialProperties.boneWeightThreshold)
                                boneInfo2.affectedVerts.Add(num);
                        }
                        else if ((bool)materialProperties)
                        {
                            if (weight.weight > materialProperties.boneWeightThreshold)
                                boneInfo2.affectedVerts.Add(num);
                        }
                        else if (weight.weight > host.boneWeightThreshold)
                        {
                            boneInfo2.affectedVerts.Add(num);
                        }
                    }

                    bones = list.Where(b => b.affectedVerts.Count > 0 && !host.excludedBones.Contains(b.transform))
                        .ToList();
                }

                var num2 = 0;
                var subMeshCount = skinnedMesh.sharedMesh.subMeshCount;
                while (true)
                {
                    var list2 = !host.splitCollisionMeshesByMaterial
                        ? skinnedMesh.sharedMesh.triangles.ToList()
                        : skinnedMesh.sharedMesh.GetTriangles(num2).ToList();
                    if (!host.excludedMaterials.Contains(skinnedMesh.sharedMaterials[num2]))
                        foreach (var bone in bones)
                        {
                            var list3 = new List<int>();
                            var num3 = 0;
                            while (num3 < list2.Count)
                            {
                                var array = new int[3]
                                {
                                    list2[num3++],
                                    list2[num3++],
                                    list2[num3++]
                                };
                                if (bone.affectedVerts.Contains(array[0]) || bone.affectedVerts.Contains(array[1]) ||
                                    bone.affectedVerts.Contains(array[2]))
                                {
                                    if (host.onlyUniqueTriangles)
                                    {
                                        num3 -= 3;
                                        list2.RemoveRange(num3, 3);
                                    }

                                    list3.AddRange(array);
                                }
                            }

                            if (list3.Count == 0) continue;
                            var num4 = list3.Count / 3;
                            var boneMeshColl = new BoneMeshColl
                            {
                                parentBone = bone,
                                host = host,
                                skinnedMeshMaterialIndex = num2
                            }.Init();
                            bone.boneMeshes.Add(boneMeshColl);
                            if (num4 > host.maxColliderTriangles)
                            {
                                var num5 = Mathf.CeilToInt(num4 / (float)host.maxColliderTriangles);
                                boneMeshColl.SetTris(list3.Take(host.maxColliderTriangles * 3));
                                for (var num6 = 1; num6 < num5; num6++)
                                {
                                    var boneMeshColl2 = new BoneMeshColl
                                    {
                                        parentBone = bone,
                                        host = host
                                    }.Init();
                                    boneMeshColl2.SetTris(list3.Skip(num6 * host.maxColliderTriangles * 3)
                                        .Take(host.maxColliderTriangles * 3));
                                    bone.boneMeshes.Add(boneMeshColl2);
                                }
                            }
                            else
                            {
                                boneMeshColl.SetTris(list3);
                            }
                        }

                    if (!host.splitCollisionMeshesByMaterial || num2 >= subMeshCount - 1) break;
                    num2++;
                }

                bones.RemoveAll(b => b.boneMeshes.Count == 0);
                foreach (var bone2 in bones) bone2.affectedVerts = null;
                return this;
            }

            public void UpdateRootMatrix()
            {
                meshRootMatrix.SetTRS(skinnedMesh.transform.position, skinnedMesh.transform.rotation, Vector3.one);
            }

            public void Update(bool force = false)
            {
                skinnedMesh.BakeMesh(bakedMesh);
                var vertices = bakedMesh.vertices;
                UpdateRootMatrix();
                foreach (var bone in bones) bone.Update(vertices, force);
            }
        }

        private struct Weight
        {
            public int boneIndex;

            public float weight;
        }
        
        public class BoneInfo
        {
            public Transform transform;

            public Matrix4x4 cachedMatrix;

            public SkinnedMeshRenderer srcSkin;

            public HashSet<int> affectedVerts = new HashSet<int>();

            public List<BoneMeshColl> boneMeshes = new List<BoneMeshColl>();

            public RASCALSkinnedMeshCollider host;

            public RASCALPhysMaterialProperties materialProperties;

            [NonSerialized] public Skinfo parentSkinfo;

            public BoneInfo Init()
            {
                materialProperties = transform.GetComponent<RASCALPhysMaterialProperties>();
                return this;
            }

            public void Update(Vector3[] verts, bool force = false)
            {
                cachedMatrix = transform.worldToLocalMatrix;
                foreach (var boneMesh in boneMeshes) boneMesh.Update(verts, force);
            }
        }


        public class BoneMeshColl
        {
            public Mesh collMesh;

            public MeshCollider meshCol;

            public int[] tris;

            public int[] distinctVerts;

            public RASCALSkinnedMeshCollider host;

            public int skinnedMeshMaterialIndex;

            [NonSerialized] public BoneInfo parentBone;

            [SerializeField] private Vector2[] serializedUV1;

            [SerializeField] private Vector2[] serializedUV2;

            [SerializeField] private Vector2[] serializedUV3;

            [SerializeField] private Vector2[] serializedUV4;

            [SerializeField] private Vector3[] serializedVerticies;

            [SerializeField] private Vector3[] serializedNormals;

            [SerializeField] private int[] serializedTriangles;

            [SerializeField] private bool serialized;

            public int vertexCount => distinctVerts.Length;

            internal BoneMeshColl Init()
            {
                collMesh = new Mesh();
                meshCol = parentBone.transform.gameObject.AddComponent<MeshCollider>();
                meshCol.convex = host.convexMeshColliders;
                HandlePhysMatInheritence();
                return this;
            }

            private void HandlePhysMatInheritence()
            {
                var skinMat = parentBone.srcSkin.sharedMaterials[skinnedMeshMaterialIndex];
                var physicsMaterialAssociation = host.materialAssociationList.Find(m => m.material == skinMat);
                if (physicsMaterialAssociation != null)
                    meshCol.sharedMaterial = physicsMaterialAssociation.physicsMaterial;
                if ((bool)parentBone.materialProperties &&
                    (!meshCol.sharedMaterial || parentBone.materialProperties.overrideOthers))
                    meshCol.sharedMaterial = parentBone.materialProperties.physicsMaterial;
                if ((bool)parentBone.parentSkinfo.materialProperties && (!meshCol.sharedMaterial ||
                                                                         parentBone.parentSkinfo.materialProperties
                                                                             .overrideOthers))
                    meshCol.sharedMaterial = parentBone.parentSkinfo.materialProperties.physicsMaterial;
                if ((bool)parentBone.materialProperties && parentBone.materialProperties.overrideOthers)
                    meshCol.sharedMaterial = parentBone.materialProperties.physicsMaterial;
                if (!meshCol.sharedMaterial) meshCol.sharedMaterial = host.physicsMaterial;
            }

            public void SetTris(IEnumerable<int> inTriList)
            {
                tris = inTriList.ToArray();
                distinctVerts = tris.Distinct().ToArray();
                var dictionary = new Dictionary<int, int>(distinctVerts.Length);
                for (var i = 0; i < distinctVerts.Length; i++) dictionary.Add(distinctVerts[i], i);
                for (var j = 0; j < tris.Length; j++) tris[j] = dictionary[tris[j]];
                collMesh.vertices = new Vector3[vertexCount];
                collMesh.triangles = tris;
                CopyExtraMeshData();
            }

            internal void CopyExtraMeshData()
            {
                var sharedMesh = parentBone.srcSkin.sharedMesh;
                for (var i = 0; i < 4; i++)
                {
                    var list = new List<Vector2>();
                    sharedMesh.GetUVs(i, list);
                    if (list.Count != 0)
                    {
                        var list2 = new List<Vector2>();
                        for (var j = 0; j < distinctVerts.Length; j++) list2.Add(list[distinctVerts[j]]);
                        collMesh.SetUVs(i, list2);
                    }
                }

                var normals = sharedMesh.normals;
                var array = new Vector3[distinctVerts.Length];
                for (var k = 0; k < distinctVerts.Length; k++) array[k] = normals[distinctVerts[k]];
                collMesh.normals = array;
            }

            internal void SerializeForPrefab()
            {
                serializedUV1 = collMesh.uv;
                serializedUV2 = collMesh.uv2;
                serializedUV3 = collMesh.uv3;
                serializedUV4 = collMesh.uv4;
                serializedNormals = collMesh.normals;
                serializedVerticies = collMesh.vertices;
                serializedTriangles = collMesh.triangles;
                serialized = true;
            }

            internal void UnserializeForPrefab()
            {
                serializedUV1 = null;
                serializedUV2 = null;
                serializedUV3 = null;
                serializedUV4 = null;
                serializedVerticies = null;
                serializedNormals = null;
                serializedTriangles = null;
                serialized = false;
            }

            internal void RebuildFromSerialized()
            {
                if ((bool)collMesh) DestroyImmediate(collMesh);
                collMesh = new Mesh();
                collMesh.vertices = serializedVerticies;
                collMesh.triangles = serializedTriangles;
                collMesh.normals = serializedNormals;
                collMesh.uv = serializedUV1;
                collMesh.uv2 = serializedUV2;
                collMesh.uv3 = serializedUV3;
                collMesh.uv4 = serializedUV4;
                meshCol.sharedMesh = collMesh;
            }

            public bool PastThreshold(Vector3[] newVerts)
            {
                var num = 0f;
                var vertices = collMesh.vertices;
                for (var i = 0; i < distinctVerts.Length; i++) num += (newVerts[i] - vertices[i]).sqrMagnitude;
                return num >= host.updateThreshold;
            }

            internal Vector3[] TransformVertices(Vector3[] actualVerts)
            {
                var array = new Vector3[vertexCount];
                if (parentBone.parentSkinfo.noBones && !host.zeroBoneMeshAlternateTransform)
                {
                    for (var i = 0; i < distinctVerts.Length; i++) array[i] = actualVerts[distinctVerts[i]];
                }
                else
                {
                    var matrix4x = parentBone.cachedMatrix * parentBone.parentSkinfo.meshRootMatrix;
                    for (var j = 0; j < distinctVerts.Length; j++)
                        array[j] = matrix4x.MultiplyPoint3x4(actualVerts[distinctVerts[j]]);
                }

                return array;
            }

            public void Update(Vector3[] bakedVerts, bool force = false)
            {
                var array = TransformVertices(bakedVerts);
                if (force || PastThreshold(array))
                {
                    collMesh.vertices = array;
                    meshCol.sharedMesh = collMesh;
                }
            }
        }

        [Tooltip(
            "This will (re)generate all necessary data to create the bone-meshes used in collision and also generate some initial collision shapes when the game starts.")]
        public bool generateOnStart;

        [Tooltip(
            "This will enable continuous asynchronous updating of collision shapes when the game starts. You may find that the initial collision shapes work well enough for you and you dont even need to update them which would save on performance for sure, but if the mesh deforms due to its bones being moved the shapes wont match and could cause some pretty big inaccuracies. Alternatively, if you know your mesh will only need to be updated at certain points, you can call either immediate or asynchronous reconstruction of the collision shapes manually via the provided functions in the script.")]
        public bool enableUpdatingOnStart;

        [Tooltip(
            "This generates collision on startup immediately, rather than using the asynchronous method which could take second or two to fully generate. This obviously comes at the cost of taking longer to generate on start, which is why its recommended to just pre-generate everything rather than on startup.")]
        public bool immediateStartupCollision;

        [Tooltip(
            "When enabled, only unique triangles will be used between all the bone-meshes. This prevents mesh overlapping but because of how triangles are chosen without any care for which bone the triangle is more significant to, this can lead to messy bone-meshes that may impact results of certain collisions. It's bit faster and uses less memory, this option should probably be on unless you notice some otherwise bad collision meshes being generated as a result of it.")]
        public bool onlyUniqueTriangles = true;

        [Tooltip(
            "Enable the convex setting of the mesh colliders. This option obviously leads to inaccuracies in the overall mesh and you may need to lower the max polygons per mesh to avoid errors, but convex meshes should allow for non-kinematic rigid bodies to be used if that's something that you need.")]
        public bool convexMeshColliders;

        [Tooltip(
            "Splits each collision mesh up by material. For example if your skinned mesh has 2 materials it will create 1 collider for all triangles with the first material, and another for the second material. This is useful and required mostly for applying different physics materials to the colliders based on material or excluding mesh parts based on material. To do that, use the material association list and the exclusion list.")]
        public bool splitCollisionMeshesByMaterial;

        [Tooltip(
            "You almost certainly don't need this. But it's included just in case. It basically makes it so meshes with no bones and only blendshapes get transformed differently. But it should be fine by default, this should be a last resort troubleshooting step.")]
        public bool zeroBoneMeshAlternateTransform;

        [Tooltip(
            "Clears ALL mesh colliders under the component when calling the clear function, not just colliders currently associated with this component. Be careful with this.")]
        public bool clearAllMeshColliders;

        [Tooltip(
            "Material to apply to the mesh colliders. If you need more granular control for materials on what bones youll need to add a RASCAL Phys Material Properties component to the bone transform or skinned-mesh transform. The priority goes from highest to lowest: Material-Association-List -> Bone-Transform -> SkinnedMesh-Transform -> PhysicsMaterial-Variable")]
        public PhysicMaterial physicsMaterial;

        public int maxColliderTriangles = 1000;

        public float boneWeightThreshold;

        public List<PhysicsMaterialAssociation> materialAssociationList = new List<PhysicsMaterialAssociation>();

        [Header("Updating")]
        [Tooltip(
            "Amount of time in milliseconds the asynchronous generation should be allowed to run per frame while idling. It is idling when the mesh isnt changing enough to warrant rebuilding the collision shapes. (this setting doesn't affect immediate updating)")]
        public double idleCpuBudget = 0.2;

        [Tooltip(
            "Amount of time in milliseconds the asynchronous generation should be allowed to run per frame while active. It is active when any of the collision shapes are actively being rebuilt. This allows more time to rebuild collision shapes which means they will update faster at the cost of performance. (this setting doesn't affect immediate updating)")]
        public double activeCpuBudget = 1.0;

        [Tooltip(
            "An amount by which the bone mesh needs to change in order for its collision to be rebuilt. The purpose of this is to allow for slight changes in the mesh before comepletely rebuilding, which slightly improves performance at the cost of accuracy. The value for this should likely be quite small but you should play around with it to see what works best for you.")]
        public float meshUpdateThreshold = 0.02f;

        private float updateThreshold;

        public List<SkinnedMeshRenderer> excludedSkins = new List<SkinnedMeshRenderer>();

        public List<Transform> excludedBones = new List<Transform>();

        public List<Material> excludedMaterials = new List<Material>();

        [HideInInspector] public Skinfo[] skinfos;

        private double timerAcc;

        private double totalTimeAcc;

        private Stopwatch timer = new Stopwatch();

        [HideInInspector] public bool asyncUpdating;

        private Coroutine updatingCoroutine;

        [HideInInspector] [SerializeField] private bool _serialized;

        public bool noMeshData
        {
            get
            {
                if (skinfos != null) return skinfos.Length == 0;
                return true;
            }
        }

        public bool serialized => _serialized;

        public event RASCALTimedEvent OnAsyncUpdateYield;

        public event RASCALTimedEvent OnAsyncPassComplete;

        private void Start()
        {
            if (serialized) RebuildFromSerialized();
            if (!Application.isPlaying) return;
            if (generateOnStart)
            {
                ProcessMesh();
                if (immediateStartupCollision)
                {
                    ImmediateUpdateColliders(true);
                    if (enableUpdatingOnStart) StartAsyncUpdating(true);
                }
                else
                {
                    StartAsyncUpdating(enableUpdatingOnStart);
                }
            }
            else if (!noMeshData)
            {
                SetBoneParents();
                if (enableUpdatingOnStart) StartAsyncUpdating(true);
            }
        }

        public void ProcessMesh()
        {
            CleanUpMeshes();
            var hashSet = new HashSet<SkinnedMeshRenderer>();
            var componentsInChildren = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var item in componentsInChildren) hashSet.Add(item);
            SkinnedMeshRenderer component;
            if ((bool)(component = GetComponent<SkinnedMeshRenderer>())) hashSet.Add(component);
            skinfos = (from skin in hashSet
                where !excludedSkins.Contains(skin)
                select new Skinfo
                {
                    skinnedMesh = skin,
                    host = this
                }.Init()).ToArray();
            if (skinfos.Length == 0) Debug.Log("No skinned meshes were found under object: " + gameObject);
        }

        public void ImmediateUpdateColliders(bool force = false)
        {
            if (noMeshData)
            {
                Debug.Log("No skinned collision mesh data found on " + ToString() + ". Processing mesh...");
                ProcessMesh();
            }

            updateThreshold = meshUpdateThreshold * meshUpdateThreshold;
            var array = skinfos;
            for (var i = 0; i < array.Length; i++) array[i].Update(force);
        }

        private void AddTime()
        {
            timerAcc += timer.LapTime();
        }

        private bool CheckFrameYield(double budget)
        {
            AddTime();
            return timerAcc > budget;
        }

        private void ResetTimer(bool doEvent = true)
        {
            timer.Restart();
            if (OnAsyncUpdateYield != null) OnAsyncUpdateYield(timerAcc);
            totalTimeAcc += timerAcc;
            timerAcc = 0.0;
        }

        public Coroutine StartAsyncUpdating(bool continuous)
        {
            if (noMeshData)
            {
                Debug.Log("No skinned collision mesh data found on " + ToString() +
                          ". Make sure you process the mesh first!");
                return null;
            }

            StopAsyncUpdating();
            updatingCoroutine = StartCoroutine(AsynchronousUpdate(continuous));
            return updatingCoroutine;
        }

        public void StopAsyncUpdating()
        {
            if (updatingCoroutine != null) StopCoroutine(updatingCoroutine);
            asyncUpdating = false;
        }

        private IEnumerator AsynchronousUpdate(bool continuous)
        {
            asyncUpdating = true;
            do
            {
                if (enabled)
                {
                    updateThreshold = meshUpdateThreshold * meshUpdateThreshold;
                    ResetTimer();
                    var array = skinfos;
                    foreach (var skinfo in array)
                    {
                        skinfo.skinnedMesh.BakeMesh(skinfo.bakedMesh);
                        skinfo.UpdateRootMatrix();
                        foreach (var bone in skinfo.bones) bone.cachedMatrix = bone.transform.worldToLocalMatrix;
                        if (CheckFrameYield(idleCpuBudget))
                        {
                            yield return new WaitForFixedUpdate();
                            ResetTimer();
                        }

                        var bakedVerts = skinfo.bakedMesh.vertices;
                        foreach (var bone2 in skinfo.bones)
                        foreach (var boneMesh in bone2.boneMeshes)
                        {
                            var array2 = boneMesh.TransformVertices(bakedVerts);
                            if (boneMesh.PastThreshold(array2))
                            {
                                boneMesh.collMesh.vertices = array2;
                                if (CheckFrameYield(activeCpuBudget))
                                {
                                    yield return new WaitForFixedUpdate();
                                    ResetTimer();
                                }

                                boneMesh.meshCol.sharedMesh = boneMesh.collMesh;
                            }
                            else if (CheckFrameYield(idleCpuBudget))
                            {
                                yield return new WaitForFixedUpdate();
                                ResetTimer();
                            }
                        }
                    }

                    if (CheckFrameYield(idleCpuBudget))
                    {
                        yield return new WaitForFixedUpdate();
                        ResetTimer();
                    }

                    AddTime();
                    totalTimeAcc += timerAcc;
                    if (OnAsyncPassComplete != null) OnAsyncPassComplete(totalTimeAcc);
                    timerAcc = 0.0;
                    totalTimeAcc = 0.0;
                }
                else
                {
                    yield return new WaitForFixedUpdate();
                }
            } while (asyncUpdating && continuous);

            asyncUpdating = false;
        }

        private void SetBoneParents()
        {
            var array = skinfos;
            foreach (var skinfo in array)
            {
                skinfo.bakedMesh = new Mesh();
                foreach (var bone in skinfo.bones)
                {
                    bone.parentSkinfo = skinfo;
                    foreach (var boneMesh in bone.boneMeshes) boneMesh.parentBone = bone;
                }
            }
        }

        public void CleanUpAllMeshColliders()
        {
            var componentsInChildren = GetComponentsInChildren<MeshCollider>();
            for (var i = 0; i < componentsInChildren.Length; i++) DestroyImmediate(componentsInChildren[i]);
        }

        public void CleanUpMeshes()
        {
            if (noMeshData)
            {
                if (clearAllMeshColliders) CleanUpAllMeshColliders();
                return;
            }

            if (updatingCoroutine != null)
            {
                asyncUpdating = false;
                StopCoroutine(updatingCoroutine);
            }

            UnserializeForPrefab();
            var array = skinfos;
            foreach (var skinfo in array)
            {
                DestroyImmediate(skinfo.bakedMesh);
                foreach (var bone in skinfo.bones)
                {
                    foreach (var boneMesh in bone.boneMeshes)
                    {
                        DestroyImmediate(boneMesh.collMesh);
                        DestroyImmediate(boneMesh.meshCol, true);
                    }

                    bone.boneMeshes = null;
                }

                skinfo.bones = null;
            }

            skinfos = null;
            if (clearAllMeshColliders) CleanUpAllMeshColliders();
        }

        public void SerializeForPrefab()
        {
            if (serialized) return;
            var array = skinfos;
            for (var i = 0; i < array.Length; i++)
                foreach (var bone in array[i].bones)
                foreach (var boneMesh in bone.boneMeshes)
                    boneMesh.SerializeForPrefab();

            _serialized = true;
        }

        public void UnserializeForPrefab()
        {
            if (!serialized) return;
            var array = skinfos;
            for (var i = 0; i < array.Length; i++)
                foreach (var bone in array[i].bones)
                foreach (var boneMesh in bone.boneMeshes)
                    boneMesh.UnserializeForPrefab();

            _serialized = false;
        }

        private void RebuildFromSerialized()
        {
            SetBoneParents();
            var array = skinfos;
            foreach (var obj in array)
            {
                obj.bakedMesh = new Mesh();
                foreach (var bone in obj.bones)
                foreach (var boneMesh in bone.boneMeshes)
                    boneMesh.RebuildFromSerialized();
            }
        }
    }

    public static class RascalExtensions
    {
        public static void Lap(this Stopwatch sw, string msg = "")
        {
            sw.Stop();
            Debug.Log(msg + "-" + sw.ElapsedMilliseconds + ":" + sw.ElapsedTicks);
            sw.Reset();
            sw.Start();
        }

        public static double LapTime(this Stopwatch sw)
        {
            sw.Stop();
            var totalMilliseconds = sw.Elapsed.TotalMilliseconds;
            sw.Reset();
            sw.Start();
            return totalMilliseconds;
        }

        public static void Restart(this Stopwatch sw)
        {
            sw.Reset();
            sw.Start();
        }
    }
}