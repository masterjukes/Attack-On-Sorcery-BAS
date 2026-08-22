using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.AI.Get;
using UnityEngine;
using UnityEngine.AI;

namespace BladeAndTitan.Titans.Generic;

public class TitanGeneric : MonoBehaviour
{
    public List<TitanLimb> limbs = new List<TitanLimb>();

    public delegate void OnLimbDestroyDelegate(TitanLimb limb);
    public event OnLimbDestroyDelegate OnLimbDestroy;
    
    public bool movementDisabled
    {
        get
        {
            var leftLeg = GetPart(TitanLimb.LimbType.LeftLeg);
            var rightLeg = GetPart(TitanLimb.LimbType.RightLeg);
            if(leftLeg.isDisabled || rightLeg.isDisabled || eyesDisabled)
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
        armLeft.GetOrAddComponent<TitanLimb>().SetupDefaultValues(100, new []{"mixamorig:LeftArm"}, 5f, 10f, TitanLimb.LimbType.LeftArm);
        var armRight = new GameObject("TitanRightArm");
        armRight.transform.SetParent(limnbParent.transform);
        armRight.GetOrAddComponent<TitanLimb>().SetupDefaultValues(100, new []{"mixamorig:RightArm"}, 5f, 10f, TitanLimb.LimbType.RightArm);
        
        
        var legleft = new GameObject("TitanLegLeft");
        legleft.transform.SetParent(limnbParent.transform);
        legleft.GetOrAddComponent<TitanLimb>().SetupDefaultValues(100, new []{"mixamorig:LeftLeg"}, 5, 10f, TitanLimb.LimbType.LeftLeg);
        var legRight = new GameObject("TitanLegRight");
        legRight.transform.SetParent(limnbParent.transform);
        legRight.GetOrAddComponent<TitanLimb>().SetupDefaultValues(100, new []{"mixamorig:RightLeg"}, 5f, 10f, TitanLimb.LimbType.RightLeg);

        
        var eyes = new GameObject("TitanEyes");
        eyes.transform.SetParent(limnbParent.transform);
        eyes.GetOrAddComponent<TitanLimb>().SetupDefaultValues(50, new []{"EyeTrigger"}, 3f, 6f, TitanLimb.LimbType.Eye);
        
        
        var jaw = new GameObject("TitanJaw");
        jaw.transform.SetParent(limnbParent.transform);
        jaw.GetOrAddComponent<TitanLimb>().SetupDefaultValues(50, new []{"DisableJawColliders"}, 3f, 6f, TitanLimb.LimbType.Jaw);
        
        
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
        return null;
    }

    public TitanLimb GetPart(TitanLimb.LimbType partType)
    {
        foreach (var limb in limbs)
        {
            if(limb.type == partType)
                return limb;
        }
        return null;
    }


    public virtual void SetupDefaultComponents()
    {
        CreateLimbs();
        gameObject.AddComponent<GenericTitanAI>();
    }

    public virtual void Kill()
    {
        Destroy(gameObject.GetComponent<GenericTitanAI>());
        gameObject.transform.FindChildRecursiveTR("NapeWound").gameObject.SetActive(true);
        gameObject.transform.FindChildRecursiveTR("StrikeNape").GetComponent<ParticleSystem>().Play();
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
        GetComponent<Animator>().SetBool("Death", true);
        GetComponent<NavMeshAgent>().enabled = false;
        Object.Destroy(gameObject, 10f);
    }


    public virtual void Damage(CollisionInstance collisionInstance)
    {
        const float damageMultiplier = 10f;
        var damage  = damageMultiplier * collisionInstance.impactVelocity.magnitude;
        
        Debug.Log(damage);
        
        if (collisionInstance.targetCollider.name == "NapeCollider")
        {
            if(damage > 50f)
                Kill();
        }
        else
        {
            var limb = FindLimbByCollider(collisionInstance.targetCollider.name);
            if (limb != null)
            {
                limb.Damage(damage);
                if (limb.isDisabled)
                {
                    OnLimbDestroy?.Invoke(limb);
                }
            }
        }
    }
    
}
