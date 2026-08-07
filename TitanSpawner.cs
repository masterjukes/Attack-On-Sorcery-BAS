using System;
using System.Collections;
using System.Linq;
using BladeAndTitan.DebugHelpers;
using BladeAndTitan.Titans;
using BladeAndTitan.Titans.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace BladeAndTitan;

public class TitanSpawner : ThunderScript
{
    public static TitanSpawner instance;
    private Texture2D[] titanFaces;
    public int activeTitanCount;
    int allowedTitanCount = 50;
    float spawnRadius = 400f;
    float spawnTick = 20f;
    float spawnMinDistance = 20f;
    float maxSpawnTries = 70f;
    float lastSpawnTime;
    
    bool canSpawn;

    
    [ModOptionButton]
    [ModOption("Spawn Titans", "Spawns Titans.")]
    public static void SpawnTitans(bool _)
    {
        GameManager.local.StartCoroutine(instance.InitialSpawn());
    } 
    
    
    public override void ScriptLoaded(ModManager.ModData modData)
    {
        base.ScriptLoaded(modData);
        instance = this;
        EventManager.onCatalogRefresh += EventManagerOnonCatalogRefresh;
    }

    private void EventManagerOnonCatalogRefresh(EventTime eventTime)
    {
        if(eventTime == EventTime.OnStart)
            return;
        
        titanFaces = new Texture2D[5];


        for (int i = 1; i < 6; i++)
        {
            int index = i - 1;

            var name = "AOT.TitanFace" + i;

            Catalog.LoadAssetAsync<Texture2D>(name, tex =>
            {
                titanFaces[index] = tex;
            }, name);
        }
    }

    public override void ScriptUpdate()
    {
        if (canSpawn && activeTitanCount < allowedTitanCount)
        {
            var adjustedSpawnTick = spawnTick / ((allowedTitanCount - activeTitanCount));
            lastSpawnTime += Time.deltaTime;
            if (lastSpawnTime >= adjustedSpawnTick)
            {
                AttemptSpawn();
                lastSpawnTime = 0f;
            }
        }
    }

    void AttemptSpawn()
    {
        var position = Vector3.zero;
        for (int j = 0; j < maxSpawnTries; j++)
        {
            position = SamplePosition();
            if (position != Vector3.zero) break;
        }

        if (position == Vector3.zero) return;
            
        GameManager.local.StartCoroutine(SpawnTitan(position, Quaternion.identity));
    }



    IEnumerator InitialSpawn()
    {
        for (int i = 0; i < allowedTitanCount; i++)
        {

            var position = Vector3.zero;
            for (int j = 0; j < maxSpawnTries; j++)
            {
                position = SamplePosition();
                if (position != Vector3.zero) break;
            }

            if (position == Vector3.zero) continue;
            
            GameManager.local.StartCoroutine(SpawnTitan(position, Quaternion.identity));
            
            var randomSeconds = Random.Range(2f, 5f);
            yield return new WaitForSeconds(randomSeconds);
        }
        canSpawn = true;
    }

    Vector3 SamplePosition()
    {
        Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.5f, spawnRadius);
        Vector3 randomDirection = new Vector3(circle.x, 0, circle.y);

        if (randomDirection.magnitude < spawnMinDistance)
            randomDirection = randomDirection.normalized * spawnMinDistance;

        randomDirection += Player.currentCreature.transform.position;

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(randomDirection, out hit, spawnRadius, NavMesh.AllAreas))
            return Vector3.zero;

        Vector3 pos = hit.position;

        float titanRadius = 1.5f * DeviatedRandom(2.5f, 0.3f);
        float titanHeight = 5f * DeviatedRandom(2.5f, 0.3f);

        if (!CanFit(pos, titanRadius, titanHeight) || pos.y > 30)
            return Vector3.zero;

        return pos;
    }
    
    bool CanFit(Vector3 pos, float radius, float height)
    {
        Vector3 bottom = pos + Vector3.up * radius;
        Vector3 top = pos + Vector3.up * (height - radius);

        Collider[] hits = new Collider[32];
        Physics.OverlapCapsuleNonAlloc(bottom, top, radius, hits);

        return hits.Length == 0;
    }

    private IEnumerator SpawnTitan(Vector3 position, Quaternion rotation)
    {
        activeTitanCount++;
        GameObject o = null;
        Catalog.InstantiateAsync("AOT.TitanPrefab", position, rotation, null, _o => { o = _o; }, "AOT.TitanPrefab");

        yield return new WaitUntil(() => o != null);
        
        var ai = o.AddComponent<TitanGeneric>();

        o.transform.FindChildRecursive("NapeWound").gameObject.SetActive(false);
        
        o.transform.localScale = DeviatedRandom(2.5f, 0.3f) * Vector3.one;
        var hair = o.transform.FindChildRecursive("Hair");
        var hair2 = o.transform.FindChildRecursive("Hair2");

        hair.gameObject.SetActive(false);
        hair2.gameObject.SetActive(false);

        var titanMaterials = o.transform.Find("Titan").gameObject.GetComponent<Renderer>().materials;
        var faceMaterial = titanMaterials.First(material => material.name.Contains("Face"));
        var hairMaterial = titanMaterials.First(material => material.name.Contains("Hair"));

        var faceTextureNumber = Random.Range(0, 5);
        var hairColorNumber = Random.Range(0, 5);
        var hairMeshNumber = Random.Range(0, 3);

        PlayAudio("TitanSpawn", o.GetComponent<AudioSource>());

        var lightningBolt = o.AddComponent<LightningBoltScript>();
        lightningBolt.StartObject = o.transform.Find("LightningBottom").gameObject;
        lightningBolt.EndObject = o.transform.Find("LightningTop").gameObject;
        lightningBolt.enabled = true;


        o.GetComponent<LineRenderer>().enabled = true;

        foreach (var renderer in o.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.enabled = false;
        yield return new WaitForSeconds(0.5f);
        
        o.GetComponent<LightningBoltScript>().enabled = false;
        o.GetComponent<LineRenderer>().enabled = false;
        
        yield return new WaitForSeconds(1.5f);
    
        
        foreach (var renderer in o.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.enabled = true;
        
        switch (hairMeshNumber)
        {
            case 0:
                hair.gameObject.SetActive(false);
                hair2.gameObject.SetActive(false);
                break;
            case 1:
                hair2.gameObject.SetActive(false);
                hair.gameObject.SetActive(true);
                break;
            case 2:
                hair.gameObject.SetActive(false);
                hair2.gameObject.SetActive(true);
                break;
        }

        faceMaterial.mainTexture = titanFaces[faceTextureNumber];
        

        switch (hairColorNumber)
        {
            case 0:
                hairMaterial.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                break;
            case 1:
                hairMaterial.color = new Color(0.37f, 0.17f, 0f, 1f);
                break;
            case 2:
                hairMaterial.color = new Color(0.6f, 0.47f, 0.33f, 1f);
                break;
            case 3:
                hairMaterial.color = new Color(0f, 0f, 0f, 1f);
                break;
            case 4:
                hairMaterial.color = new Color(1f, 0.9f, 0.8f, 1f);
                break;
        }

        hair.GetComponent<Renderer>().material.color = hairMaterial.color;
        hair2.GetComponent<Renderer>().material.color = hairMaterial.color;
        
        

        var collider = o.transform.Find("GainAggroTrigger").gameObject;
        collider.AddComponent<TitanTriggerAgro>();
        collider.GetComponent<Collider>().enabled = true;
        
        var pickUpCollider = o.transform.Find("PickUpTrigger").gameObject;
        //pickUpCollider.AddComponent<TTitanPickUpTrigger>();
        pickUpCollider.GetComponent<Collider>().enabled = true;
        
        
        yield return new WaitForSeconds(2.5f);
        PlayAudio("TitanThud", o.GetComponent<AudioSource>());;
        
        o.transform.FindChildRecursive("EatCollider").gameObject.AddComponent<TitanEatTrigger>();
        //dustEffect.SetActive(value: true);
        

    }


    public void PlayAudio(string audioName, AudioSource source)
    {
        Catalog.LoadAssetAsync<AudioContainer>(audioName, ac =>
        {
            if (source != null) source.PlayOneShot(ac.PickAudioClip());
        }, audioName);
    }

    
    public static float DeviatedRandom(float mean, float stdDev)
    {
        float u1 = 1.0f - UnityEngine.Random.value;
        float u2 = 1.0f - UnityEngine.Random.value;

        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                              Mathf.Sin(2.0f * Mathf.PI * u2);

        return mean + stdDev * randStdNormal;
    }
    
}