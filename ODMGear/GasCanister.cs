using BladeAndTitan.DebugHelpers;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.ODMGear;

public class GasCanisterModule : ItemModule
{
    public override void OnItemLoaded(Item item)
    {
        base.OnItemLoaded(item);
        var detectorR = item.transform.Find("WirePointR").gameObject.AddComponent<WireAttaching.WireDetector>();
        detectorR.expectedWireNames = new[] {"GasWireR", "GasWireL"};
        var detectorL = item.transform.Find("WirePointL").gameObject.AddComponent<WireAttaching.WireDetector>();
        detectorL.expectedWireNames = new[] {"GasWireR", "GasWireL"};

        item.GetOrAddComponent<GasCanisterTracker>();
    }
}

public class GasCanisterTracker : MonoBehaviour
{
    float gasAmount;
    float gasMax = 1000;

    void Start()
    {
        gasAmount = gasMax;
    }
    
    public bool UseGas(float amount)
    {
        gasAmount -= amount;
        gasAmount = Mathf.Clamp(gasAmount, 0, gasMax);
        return gasAmount >= 0;
    }
}