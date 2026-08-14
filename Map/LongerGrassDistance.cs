using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.Map;

public class LongerGrassDistance : LevelModule
{
    private const float distance = 1000f;
    
    public override IEnumerator OnPlayerSpawnCoroutine()
    {
        GameObject.Find("Terrains").GetComponentInChildren<Terrain>().detailObjectDistance = distance;
        return base.OnPlayerSpawnCoroutine();
    }
}