using BladeAndTitan.TitanShifting.Abstract;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.SpecialisedTitans;

public class ColossalTitan : PlayerTitanBase
{
    public override string titanAddress => "Bert_ColossalTitanRig";
    public override float footDistance => 8f;
    public override float stepSpeed => 0.8f;
    
    public override float maxHealth => 1000f;
    public override float jumpForce => 2f;


    protected override void OnSpecialShift()
    {
        base.OnSpecialShift();
        foreach (var creature in Creature.InRadius(titan.transform.FindChildRecursiveTR("CreatureLocation").position, 30f))
        {
            creature.Inflict("Burning", "ckig", 320, 100f);
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
        
    }
    
    
}