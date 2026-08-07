using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.Abstract
{
    /*
    public class TitanPosess : ThunderScript
    {
        private bool isTitan;
        
        public override void ScriptEnable()
        {
            base.ScriptEnable();
            EventManager.onPossess += EventManagerOnonPossess;
            EventManager.onLevelUnload += EventManagerOnonLevelUnload;

        }

        private void EventManagerOnonLevelUnload(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
        {
            isTitan = false;
        }

        private void EventManagerOnonPossess(Creature creature, EventTime eventTime)
        {
            if(eventTime == EventTime.OnStart)
                return;
            
            Player.local.handRight.OnFistEvent += HandRightOnOnFistEvent;
        }

        private void HandRightOnOnFistEvent(PlayerHand hand, bool gripping)
        {
            if (gripping && !isTitan)
            {
                isTitan = true;
                
                Catalog.InstantiateAsync("Bert_ColossalTitanRig", Player.local.transform.position, Player.local.transform.rotation, null,
                    o =>
                    {
                        
                        var height = o.transform.FindChildRecursiveTR("Scale").position.y - o.transform.FindChildRecursiveTR("CreatureLocation").position.y;
                        
                        var q = o.AddComponent<VRIK>();
                        q.AutoDetectReferences();
                        
                        var th = new GameObject("j");
                        th.transform.position = Player.local.head.transform.position;
                        th.transform.parent = Player.local.head.transform;
                        th.transform.localPosition = new Vector3(0, 0, -0.1f);
                        th.transform.localRotation = Quaternion.Euler(0, -90, -90);
                        
                        var thr = new GameObject("j2");
                        thr.transform.position = Player.local.handRight.transform.position;
                        thr.transform.parent = Player.local.handRight.transform;
                        thr.transform.localPosition = new Vector3(0, 0, 0);
                        thr.transform.localRotation = Quaternion.Euler(0, 90, 90);
                        
                        var thl = new GameObject("j3");
                        thl.transform.position = Player.local.handLeft.transform.position;
                        thl.transform.parent = Player.local.handLeft.transform;
                        thl.transform.localPosition = new Vector3(0, 0, 0);
                        thl.transform.localRotation = Quaternion.Euler(0, -90, 90);

                        q.solver.spine.headTarget = th.transform;
                        q.solver.leftArm.target = thl.transform;
                        q.solver.rightArm.target = thr.transform;
                        q.solver.locomotion.footDistance = 8f;
                        q.solver.locomotion.stepHeight = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 20), new Keyframe(1, 0));
                        q.solver.locomotion.stepSpeed = 0.8f;
                        q.solver.locomotion.maxVelocity = 30f;
                        q.solver.locomotion.stepThreshold = 17f;
                        q.solver.plantFeet = false;
                        

                        Player.currentCreature.renderers.ForEach(r => r.renderer.enabled = false);
                        o.transform.SetParent(Player.local.transform, true);

                        
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

                        
                        
                        


                    }, "gd");
                
                
                
            }
            
            
        }



        
        public override void ScriptUpdate()
        {
            base.ScriptUpdate();
            if (!(Player.local?.creature != null))
            {
                return;
            }
            InventoryChestHolder inventoryChestHolder = Player.currentCreature?.GetComponentInChildren<InventoryChestHolder>();;
            if ((object)inventoryChestHolder == null)
            {
                return;
            }
            GameObject gameObject = inventoryChestHolder.gameObject;
            if ((object)gameObject == null)
            {
                return;
            }
            int active;
            if (isTitan)
            {
                active = 0;
            }
            else
            {
                active = 1;
            }
            gameObject.SetActive((byte)active != 0);
        }
        


        

        
    }
    */
}