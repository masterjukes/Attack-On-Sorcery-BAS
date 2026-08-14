using System.Collections;
using ThunderRoad;

namespace BladeAndTitan.Map;

public class ViewDistanceIncrease : LevelModule
{
    public override IEnumerator OnPlayerSpawnCoroutine()
    { 
        Player.local.head.cam.farClipPlane = 2700f;
        return base.OnPlayerSpawnCoroutine();
    }
}