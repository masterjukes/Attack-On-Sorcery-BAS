using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.Map;

public class LongerGrassDistance : LevelModule
{
    private const float distance = 1000f;
    
    public override IEnumerator OnPlayerSpawnCoroutine()
    {
        foreach (var componentsInChild in GameObject.Find("Terrains").GetComponentsInChildren<Terrain>())
        {
            componentsInChild.detailObjectDistance = distance;
        }

        return base.OnPlayerSpawnCoroutine();
    }
}