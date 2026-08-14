using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.TitanShifting.Abstract
{
    public class TitanPosess : ThunderScript
    {
        public override void ScriptEnable()
        {
            base.ScriptEnable();
            EventManager.onPossess += EventManagerOnonPossess;
        }

        private void EventManagerOnonPossess(Creature creature, EventTime eventTime)
        {
            PlayerTitanBase.isTitan = false;
            PlayerTitanBase.isTransforming = false;
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
            if (PlayerTitanBase.isTitan)
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
    
}