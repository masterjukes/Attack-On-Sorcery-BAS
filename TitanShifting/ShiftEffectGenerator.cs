using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting
{

    public class ShiftEffectGenerator : MonoBehaviour
    {
        List<GameObject> createdObjects = new List<GameObject>();

        
        
        public float duration = 5f;
        public int lightingCount = 10;
        public float maxLightIntensity = 3f;

        private const string AudioName = "TitanShiftAudioGeneric";
        private const string MaterialName = "TitanShiftLightningMaterial";
       
        
        public IEnumerator Activate()
        {
            
            
            
            for (int i = 0; i < lightingCount; i++)
            {
                SpawnRandomLighting();
            }
            
            var light = new GameObject("TitanLightingLight");
            light.transform.parent = transform;
            light.transform.position = transform.position;
            var audioSource = light.AddComponent<AudioSource>();
            var actualLight = light.AddComponent<Light>();
            actualLight.color = Color.yellow;
            actualLight.intensity = 0;
            actualLight.range = 10;
            actualLight.type = LightType.Point;
            actualLight.shadows = LightShadows.None;
            actualLight.enabled = true;
            light.transform.position += Vector3.up;
            
            TitanSpawner.PlayAudio(AudioName, audioSource);


            float time = 0;
            while (time < duration)
            {
                actualLight.intensity = Mathf.Lerp(0, maxLightIntensity, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            
            Destroy(light);
            foreach (var obj in createdObjects)
                Destroy(obj);
        }


        void SpawnRandomLighting()
        {

            if (createdObjects.Count >= lightingCount)
                return;

            var lighting = new GameObject($"TitanLighting{createdObjects.Count}");
            
            lighting.transform.position = transform.position;
            lighting.transform.rotation = transform.rotation;
            lighting.transform.parent = transform;
            
            var startObject = new GameObject("StartObject");
            var endObject = new GameObject("EndObject");
            
            createdObjects.Add(lighting);
            createdObjects.Add(startObject);
            createdObjects.Add(endObject);
            
            var component = lighting.AddComponent<LightningBoltScript>();
            startObject.transform.position = transform.position;
            endObject.transform.position = transform.position;
            
            Catalog.LoadAssetAsync<Material>(MaterialName, material =>
            { 
                lighting.GetComponent<Renderer>().material = material;
            }, MaterialName);
            
            endObject.transform.localPosition = Vector3.up * Random.Range(10f, 30f);
            
            startObject.transform.parent = lighting.transform;
            endObject.transform.parent = lighting.transform;

            component.StartObject = startObject;
            component.EndObject = endObject;
            
            component.enabled = true;
            component.Generations = 8;
            component.Duration = 0.05f;
            component.ChaosFactor = Random.Range(2f, 3.5f);
        }
    }
}