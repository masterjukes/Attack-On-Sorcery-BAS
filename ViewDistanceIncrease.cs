using System.Collections;
using ThunderRoad;

namespace BladeAndTitan;

public class ViewDistanceIncrease : LevelModule
{
    public override IEnumerator OnPlayerSpawnCoroutine()
    { 
        yield return Yielders.ForSeconds(0.5f);
        Player.local.head.cam.farClipPlane = 2700f;
        yield return base.OnPlayerSpawnCoroutine();
    }
}