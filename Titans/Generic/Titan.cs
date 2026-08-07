using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.AI.Get;
using UnityEngine;

namespace BladeAndTitan.Titans.Generic;

public class TitanGeneric : MonoBehaviour
{
    public List<TitanLimb> limbs = new List<TitanLimb>();

    
    public bool movementDisabled
    {
        get
        {
            var leftLeg = GetPart(TitanLimb.LimbType.LeftLeg);
            var rightLeg = GetPart(TitanLimb.LimbType.RightLeg);
            if(leftLeg.isDisabled && rightLeg.isDisabled)
                return true;
            
            if(eyesDisabled)
                return true;
            
            return false;
        }
    }

    public bool armsDisabled
    {
        get
        {
            var leftArm = GetPart(TitanLimb.LimbType.LeftArm);
            var rightArm = GetPart(TitanLimb.LimbType.RightArm);
            if(leftArm.isDisabled && rightArm.isDisabled)
                return true;
            return false;
        }
    }

    public bool jawDisabled
    {
        get
        {
            var jaw = GetPart(TitanLimb.LimbType.Jaw);
            return jaw.isDisabled;
        }
    }
    
    public bool eyesDisabled
    {
        get
        {
            var eyes = GetPart(TitanLimb.LimbType.Eye);
            return eyes.isDisabled;
        }
    }
    

    protected virtual void Start()
    {
        SetupDefaultComponents();
    }

    protected virtual void CreateLimbs()
    {
        var limnbParent = new GameObject("TitanLimbs");
        limnbParent.transform.SetParent(transform);
        
        var armLeft = new GameObject("TitanLeftArm");
        armLeft.transform.SetParent(limnbParent.transform);
        armLeft.GetOrAddComponent<TitanLimb>().SetupDefaultValues(3000, new []{"mixamorig:LeftArm"}, 100f, 10f, TitanLimb.LimbType.LeftArm);
        var armRight = new GameObject("TitanRightArm");
        armRight.transform.SetParent(limnbParent.transform);
        armRight.GetOrAddComponent<TitanLimb>().SetupDefaultValues(3000, new []{"mixamorig:RightArm"}, 100f, 10f, TitanLimb.LimbType.RightArm);
        
        
        var legleft = new GameObject("TitanLegLeft");
        legleft.transform.SetParent(limnbParent.transform);
        legleft.GetOrAddComponent<TitanLimb>().SetupDefaultValues(3000, new []{"mixamorig:LeftLeg"}, 100f, 10f, TitanLimb.LimbType.LeftLeg);
        var legRight = new GameObject("TitanLegRight");
        legRight.transform.SetParent(limnbParent.transform);
        legRight.GetOrAddComponent<TitanLimb>().SetupDefaultValues(3000, new []{"mixamorig:RightLeg"}, 100f, 10f, TitanLimb.LimbType.RightLeg);

        
        var eyes = new GameObject("TitanEyes");
        eyes.transform.SetParent(limnbParent.transform);
        eyes.GetOrAddComponent<TitanLimb>().SetupDefaultValues(500, new []{"EyeTrigger"}, 30f, 6f, TitanLimb.LimbType.Eye);
        
        
        var jaw = new GameObject("TitanJaw");
        jaw.transform.SetParent(limnbParent.transform);
        jaw.GetOrAddComponent<TitanLimb>().SetupDefaultValues(500, new []{"DisableJawColliders"}, 30f, 6f, TitanLimb.LimbType.Jaw);
        
    }
    
    
    protected TitanLimb FindLimbByCollider(string name)
    {
        foreach (var limb in limbs)
        {
            foreach (var collider in limb.connectedCollider)
            {
                if (collider == name) return limb;
            }
        }
        Debug.LogError("Limb not found");
        return null;
    }

    public TitanLimb GetPart(TitanLimb.LimbType partType)
    {
        foreach (var limb in limbs)
        {
            if(limb.type == partType)
                return limb;
        }
        Debug.LogError("Limb not found");
        return null;
    }


    public virtual void SetupDefaultComponents()
    {
        CreateLimbs();
        gameObject.AddComponent<GenericTitanAI>();
    }


    public virtual void Damage(CollisionInstance collisionInstance)
    {
        
    }
    
}
