using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace BladeAndTitan.Titans.Generic;

public class TitanLimb : MonoBehaviour
{
    public enum LimbType
    {
        RightLeg,
        LeftLeg,
        RightArm,
        LeftArm,
        Jaw,
        Eye,
        other
    }
    
    TitanGeneric titan;
    public bool isDisabled;
    float health;
    float maxHealth;
    float healRate;
    private float healFromDestroyDelay;
    private float currentHealTime = 0;
    
    public LimbType type;
    
    public string[] connectedCollider;

    
    public virtual void SetupDefaultValues(float maxHealth, string[] connectedCollider, float healRate, float healFromDestroyDelay, LimbType type)
    {
        this.maxHealth = maxHealth;
        this.connectedCollider = connectedCollider;
        this.healRate = healRate;
        this.healFromDestroyDelay = healFromDestroyDelay;
        this.type = type;

        health = maxHealth;
        titan = GetComponentInParent<TitanGeneric>();
        titan.limbs.Add(this);
    }
    

    
    public virtual void Damage(float amount)
    {
        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        if (health <= 0)
        {
            DestroyPart();
        }
    }

    public virtual void DestroyPart()
    {
        isDisabled = true;
        currentHealTime = 0;

    }


    public void Update()
    {
        if (isDisabled)
        {
            if (currentHealTime < healFromDestroyDelay)
            {
                currentHealTime += Time.deltaTime;
                return;
            }
        }
        isDisabled = false;
        health += Time.deltaTime * healRate;
        health = Mathf.Clamp(health, 0, maxHealth);
    }
    
}


