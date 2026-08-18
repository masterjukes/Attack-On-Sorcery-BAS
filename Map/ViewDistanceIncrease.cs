using System.Collections;
using BladeAndTitan.DestructionPhysics;
using ThunderRoad;

namespace BladeAndTitan.Map;

public class ViewDistanceIncrease : LevelModule
{
    public override IEnumerator OnPlayerSpawnCoroutine()
    { 
        yield return base.OnPlayerSpawnCoroutine();
        Player.local.head.cam.farClipPlane = 3500f;
        EventManager.onPossess += EventManagerOnonPossess;
    }

    private void EventManagerOnonPossess(Creature creature, EventTime eventTime)
    {
        if(eventTime == EventTime.OnStart)
            return;
        
        CollapserProcedural.PrebakeMeshes();
    }

    public override void OnUnload()
    {
        base.OnUnload();
        EventManager.onPossess -= EventManagerOnonPossess;
    }
}