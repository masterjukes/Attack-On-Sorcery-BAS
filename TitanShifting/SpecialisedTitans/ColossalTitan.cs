using System.Collections;
using System.Collections.Generic;
using BladeAndTitan.DestructionPhysics;
using BladeAndTitan.Titans.Generic;
using BladeAndTitan.TitanShifting.Abstract;
using ThunderRoad;
using UnityEngine;
using UnityEngine.TerrainTools;


namespace BladeAndTitan.TitanShifting.SpecialisedTitans;

public class ColossalTitan : PlayerTitanBase
{
    public override string titanAddress => "Bert_ColossalTitanRig";
    public override float footDistance => 8f;
    public override float stepSpeed => 0.8f;
    
    public override float maxHealth => 1000f;
    public override float jumpForce => 0.2f;
    public override float speedMultiplier => 4f;
    public override float handWeight => 35f;
    
    public override float HeadTargetForwardOffset => -0.1f;
    
    protected override string VRIKLeftFootName => "LeftFoot";
    protected override string VRIKRightFootName => "RightFoot";
    
    public override string stepSoundId => "CollTitanStepAudio";

    static ParticleSystem smokeEffect;
    private static bool isCastingL;
    private static bool isCastingR;

    private static float lastCastL = float.MaxValue;
    private static float lastCastR = float.MaxValue;

    private static float lastSmokeTime = Time.time;

    public override bool useXYThumbRotation => true;
    
    Vector3 _thumbRotationRight = new Vector3(0, 90, 70);
    Vector3 _thumbRotationLeft = new Vector3(0, -90, -70);
    
    public override Vector3 thumbRotationLeft => _thumbRotationLeft;
    public override Vector3 thumbRotationRight => _thumbRotationRight;
    
    /* R = 0, 90, 70 */
    /* L = 0, -90, -70 */


    protected void CTExplosion()
    {
        PlaySound("CollTitanShiftExplosionAudio", Player.currentCreature.transform.position);
        titan.transform.FindChildRecursive("TitanTransformSpecialFX").gameObject.GetComponent<ParticleSystem>().Play();
        ApplyExplosionForce(200f, titan.transform.FindChildRecursiveTR("CreatureLocation").position, 500f);
        
        var hits = Physics.RaycastAll(titan.transform.position, Vector3.down,100f, 1, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider is TerrainCollider terrainCollider)
            {
                Debug.Log("Applying terrain paint!");

                var terrain = terrainCollider.gameObject.GetComponent<Terrain>();
                PaintTerrain(terrain, hit.point, 5, 200f, 100f);
                PaintTerrain(terrain, hit.point, 6, 100f, 100f);
                break;
            }
        }

        //GameManager.local.StartCoroutine(ControlLight());
        
        foreach (var creature in Creature.InRadius(titan.transform.FindChildRecursiveTR("CreatureLocation").position, 60f))
        {
            creature.Inflict("Burning", "ckig", 320, 100f);
        }
    }

    public static void PaintTerrain(
        Terrain terrain,
        Vector3 worldPosition,
        int layerIndex,
        float radius,
        float strength = 1f)
    {
        TerrainData data = terrain.terrainData;

        Vector3 terrainPos = terrain.transform.position;

        // Convert world position to normalized terrain coordinates
        float normalizedX =
            (worldPosition.x - terrainPos.x) / data.size.x;

        float normalizedZ =
            (worldPosition.z - terrainPos.z) / data.size.z;

        int mapWidth = data.alphamapWidth;
        int mapHeight = data.alphamapHeight;

        int centerX = Mathf.RoundToInt(normalizedX * mapWidth);
        int centerZ = Mathf.RoundToInt(normalizedZ * mapHeight);

        int radiusPixels =
            Mathf.RoundToInt(radius / data.size.x * mapWidth);

        int startX = Mathf.Max(0, centerX - radiusPixels);
        int startZ = Mathf.Max(0, centerZ - radiusPixels);
        int endX = Mathf.Min(mapWidth - 1, centerX + radiusPixels);
        int endZ = Mathf.Min(mapHeight - 1, centerZ + radiusPixels);

        int width = endX - startX + 1;
        int height = endZ - startZ + 1;

        float[,,] alphamaps =
            data.GetAlphamaps(startX, startZ, width, height);

        int layerCount = data.alphamapLayers;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x + startX - centerX;
                float dz = z + startZ - centerZ;

                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance > radiusPixels)
                    continue;

                float falloff =
                    1f - Mathf.Clamp01(distance / radiusPixels);

                float amount = falloff * strength;

                // Increase target layer
                alphamaps[z, x, layerIndex] += amount;

                // Renormalize all layers so their total remains 1
                float total = 0f;

                for (int layer = 0; layer < layerCount; layer++)
                    total += alphamaps[z, x, layer];

                for (int layer = 0; layer < layerCount; layer++)
                    alphamaps[z, x, layer] /= total;
            }
        }

        data.SetAlphamaps(startX, startZ, alphamaps);
    }

    IEnumerator ControlLight()
    {
        float intensityTarget = 2f;
        Light light = titan.transform.FindChildRecursive("TitanNukeRedAura").GetComponent<Light>();
        while (light.intensity < intensityTarget)
        {
            light.intensity = Mathf.Lerp(light.intensity, intensityTarget, Time.deltaTime * 2f);
            yield return Yielders.EndOfFrame;
        }
        
        yield return new WaitForSeconds(15f);
        while (light.intensity > 0)
        {
            light.intensity = Mathf.Lerp(light.intensity, 0, Time.deltaTime / 4f);
            yield return null;
        }
        light.intensity = 0;
        
    }
    
    public void ApplyExplosionForce(float radius, Vector3 explosionPosition, float force)
    {
        Collider[] colliders = Physics.OverlapSphere(explosionPosition, radius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                if(rb.GetComponentInParent<Player>() != null)
                    continue;
                
                
                if (rb.GetComponentInParent<Creature>() != null)
                {
                    rb.GetComponentInParent<Creature>().ragdoll.SetState(Ragdoll.State.Destabilized);
                    rb.GetComponentInParent<Creature>().AddExplosionForce(force, explosionPosition, radius, 3, ForceMode.Impulse);

                }
                else
                {
                    rb.AddExplosionForce(force, explosionPosition, radius, 3, ForceMode.Impulse);
                }
            }

            if (collider.gameObject.name.Contains("House"))
            {
                collider.gameObject.GetOrAddComponent<HouseDestroyer>().Init(radius, explosionPosition, force);
            }
            
            if (collider.gameObject.GetComponent<SimplePhysicsObject>())
            {
                collider.gameObject.GetComponent<SimplePhysicsObject>().AddExplosionForce(force, explosionPosition, radius, 3, ForceMode.Impulse);
            }
            
            if (collider.GetComponentInParent<TitanGeneric>())
            {
                var titan = collider.GetComponentInParent<TitanGeneric>();
                var titanRb = titan.GetComponent<Rigidbody>();
            
                titanRb.isKinematic = false;
                titanRb.useGravity = true;
                titanRb.AddExplosionForce(force, explosionPosition, radius, 3, ForceMode.Impulse);;
                titan.Kill();
            
            
            }
            
        }
    }

    protected override Quaternion TitanHandLeftRotation => Quaternion.Euler(0, -90, 90);
    protected override Quaternion TitanHandRightRotation => Quaternion.Euler(0, 90, -90);
    protected override Quaternion TitanHeadRotation => Quaternion.Euler(0, -90, -90);

    protected override void OnShift()
    {
        base.OnShift();
        ApplyExplosionForce(4, Player.currentCreature.transform.position, 25f);
    }

    protected override void OnTitanPossess()
    {
        base.OnTitanPossess();
        Debug.Log("Titan possess");
        smokeEffect = titan.transform.FindChildRecursive("TitanSmokeAbility").gameObject.GetComponent<ParticleSystem>();
        if(smokeEffect == null)
            Debug.LogError("Smoke effect not found");
        else
        {
            Debug.Log("Smoke effect found");
        }

        leftFoot.GetOrAddComponent<TitanFootCollider>();
        rightFoot.GetOrAddComponent<TitanFootCollider>();
        
    }

    protected override void OnLeftFootstep()
    {
        base.OnLeftFootstep();
        foreach (var creature in leftFoot.GetComponent<TitanFootCollider>().creatures)
        {
            creature?.Kill();
        }
        //CheckAndDestroyHouses(leftFoot.GetComponent<TitanFootCollider>().houses);
        ApplyExplosionForce(15f, leftFoot.transform.position, 10f);
        
        var hits = Physics.RaycastAll(leftFoot.position, Vector3.down, 100f, 1, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider is TerrainCollider terrainCollider)
            {
                Debug.Log("Stamping foot!");
                
                var terrain = terrainCollider.gameObject.GetComponent<Terrain>();
                Debug.Log($"Terrain height: {terrain.SampleHeight(hit.point)}");
                FootprintStamp.Stamp(terrain, hit.point, 0);
                terrain.terrainData.SyncHeightmap();
                Debug.Log($"Terrain height: {terrain.SampleHeight(hit.point)}");

                break;
            }
        }

    }
    
    protected override void OnRightFootstep()
    {
        base.OnRightFootstep();
        foreach (var creature in rightFoot.GetComponent<TitanFootCollider>().creatures)
        {
            creature.Kill();
        }
        //CheckAndDestroyHouses(rightFoot.GetComponent<TitanFootCollider>().houses);
        ApplyExplosionForce(15f, rightFoot.transform.position, 10f);
        
        var hits = Physics.RaycastAll(rightFoot.position, Vector3.down, 100f, 1, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider is TerrainCollider terrainCollider)
            {
                Debug.Log("Stamping foot!");

                var terrain = terrainCollider.gameObject.GetComponent<Terrain>();
                Debug.Log($"Terrain height: {terrain.SampleHeight(hit.point)}");
                FootprintStamp.Stamp(terrain, hit.point, 0);
                terrain.terrainData.SyncHeightmap();
                Debug.Log($"Terrain height: {terrain.SampleHeight(hit.point)}");
                break;
            }
        }

    }

    void CheckAndDestroyHouses(List<GameObject> houses)
    {
        foreach (var house in houses)
        {
            if (house == null)
                continue;
            
            house.GetOrAddComponent<HouseDestroyer>();
        }
        houses.Clear();
    }

    public override void Fire(bool active)
    {
        base.Fire(active);

        if (!isTransformingIn)
        {
            return;
        }

        Debug.Log("Fire is running.");
        
        if (active)
        {
            if (spellCaster.ragdollHand.side == Side.Left)
                lastCastL = Time.time;
            else
                lastCastR = Time.time;
        }
        else
        {
            if (spellCaster.ragdollHand.side == Side.Left)
                lastCastL = float.MaxValue;
            else
                lastCastR = float.MaxValue;
        }

    }

    public override void UpdateCaster()
    {
        base.UpdateCaster();
        
        
        if(!isTitan || titan == null)
            return;
        
        
        if (Time.time - lastCastL > 3f && Time.time - lastCastR > 3f)
        {
            CTExplosion();
            lastCastL = float.MaxValue;
            lastCastR = float.MaxValue;
        }
        
        
        if (spellCaster.isFiring)
        {
            if(spellCaster.ragdollHand.side == Side.Left)
                isCastingL = true;
            else
                isCastingR = true;
            
        }
        else
        {
           if(spellCaster.ragdollHand.side == Side.Left)
               isCastingL = false;
           else
               isCastingR = false;
        }

        if (isCastingL && isCastingR)
        {

            if (!smokeEffect.isPlaying)
                smokeEffect.Play();

            if (Time.time - lastSmokeTime > 0.5f)
            {
                lastSmokeTime = Time.time;
                foreach (var creature in Creature.InRadius(Player.currentCreature.transform.position, 50f))
                    if (creature != Player.currentCreature)
                        creature.Inflict("Burning", "evilSmokeTitan", 10, 10f);
            }
        }
        else
        {
            if (smokeEffect.isPlaying)
                smokeEffect.Stop();
        }

    }


    protected override void SetHands(GameObject o)
    {
        var handR = o.transform.FindChildRecursiveTR("hand.R").gameObject.AddComponent<TitanHand>();
        handR.side = Side.Right;
        handR.thumbParentName = "thumb.R";
        handR.indexParentName = "index.R";
        handR.middleParentName = "middle.R";
        handR.ringParentName = "ring.R";
        handR.pinkyParentName = "pinky.R";
        handR.Init();


        var handL = o.transform.FindChildRecursiveTR("hand.L").gameObject.AddComponent<TitanHand>();
        handL.side = Side.Left;
        handL.thumbParentName = "thumb.L";
        handL.indexParentName = "index.L";
        handL.middleParentName = "middle.L";
        handL.ringParentName = "ring.L";
        handL.pinkyParentName = "pinky.L";
        handL.Init();


        handL.maxAllowed = 5;
        handR.maxAllowed = 5;

    }
    
    
}