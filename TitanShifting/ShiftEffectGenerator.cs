using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BladeAndTitan.TitanShifting
{

    public class ShiftEffectGenerator : MonoBehaviour
    {
        List<GameObject> createdObjects = new List<GameObject>();

        public Material lightingMaterial;
        
        public float duration = 5f;
        public int lightingCount = 10;
        public float maxLightIntensity = 3f;

        private const string AudioName = "TitanShiftAudioGeneric";

        void Start()
        {
            //StartCoroutine(Activate());
        }


        IEnumerator Activate()
        {
            
            
            
            for (int i = 0; i < lightingCount; i++)
            {
                SpawnRandomLighting();
            }
            
            var light = new GameObject("TitanLightingLight");
            light.transform.position = transform.position;
            var audioSource = light.AddComponent<AudioSource>();
            var actualLight = light.AddComponent<Light>();
            actualLight.color = Color.yellow;
            actualLight.intensity = 0;
            actualLight.range = 10;
            actualLight.type = LightType.Point;
            actualLight.shadows = LightShadows.None;
            actualLight.enabled = true;
            
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
            
            createdObjects.Add(lighting);
            var component = lighting.AddComponent<LightningBoltScript>();
            component.StartPosition = transform.position;
            component.EndPosition = transform.position;
            lighting.GetComponent<Renderer>().material = lightingMaterial;
            
            component.EndPosition += Vector3.up * Random.Range(10f, 30f);

            component.enabled = true;
            component.Generations = 8;
            component.Duration = 0.05f;
            component.ChaosFactor = Random.Range(2f, 3.5f);
        }
    }
}